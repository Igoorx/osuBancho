using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using osuBancho.Core;
using osuBancho.Core.Helpers;
using osuBancho.Core.Players;
using osuBancho.Core.Scores;
using osuBancho.Helpers;

namespace osuBancho.HTTP
{
    public sealed class HttpAsyncHost
    {
        private readonly int _accepts;
        private readonly HttpListener _listener;

        /// <summary>
        /// Creates an asynchronous HTTP host.
        /// </summary>
        /// <param name="accepts">
        /// Higher values mean more connections can be maintained yet at a much slower average response time; fewer connections will be rejected.
        /// Lower values mean less connections can be maintained yet at a much faster average response time; more connections will be rejected.
        /// </param>
        public HttpAsyncHost(int accepts = 4)
        {
            _listener = new HttpListener();
            // Multiply by number of cores:
            _accepts = accepts*Environment.ProcessorCount;
        }

        public List<string> Prefixes => _listener.Prefixes.ToList();

        public void Run(params string[] uriPrefixes)
        {
            _listener.IgnoreWriteExceptions = true;

            // Add the server bindings:
            foreach (var prefix in uriPrefixes)
                _listener.Prefixes.Add(prefix);

            Task.Run(() =>
            {
                try
                {
                    // Start the HTTP listener:
                    _listener.Start();
                }
                catch (HttpListenerException hlex) when (hlex.ErrorCode == 32)
                {
                    Console.Error.WriteLine("The desired port for the Bancho is in use.");
                    return;
                }
                catch (HttpListenerException hlex)
                {
                    Console.Error.WriteLine(hlex.ErrorCode+": "+hlex.Message);
                    return;
                }

                Console.WriteLine("Bancho is UP!" + Environment.NewLine);

                // Accept connections:
                // Higher values mean more connections can be maintained yet at a much slower average response time; fewer connections will be rejected.
                // Lower values mean less connections can be maintained yet at a much faster average response time; more connections will be rejected.
                var sem = new Semaphore(_accepts, _accepts);

                while (true)
                {
                    sem.WaitOne();

#pragma warning disable 4014
                    _listener.GetContextAsync().ContinueWith(async t =>
                    {
#if !DEBUG
                        sem.Release();
#endif

                        var ctx = await t;
                        await ProcessListenerContext(ctx, this);
                        /*Se der problema, fazer um queue para Processar os requests*/

#if DEBUG
                        sem.Release();
#endif
                    });
#pragma warning restore 4014
                }
            }).Wait();
        }

        internal static async Task ProcessListenerContext(HttpListenerContext context, HttpAsyncHost host)
        {
            Debug.Assert(context != null);

#if MEASUREREQ
            var sw = System.Diagnostics.Stopwatch.StartNew();
#endif

            try
            {
                if (context.Request.HttpMethod != "GET" && context.Request.HttpMethod != "POST") return;
                context.Response.StatusCode = 200;
                context.Response.SendChunked = true;
                context.Response.KeepAlive = true;

                var outStream = new MemoryStream();

                if (context.Request.RemoteEndPoint != null)
                {
                    Debug.WriteLine("{0} from {1}: {2}", context.Request.HttpMethod, context.Request.RemoteEndPoint.Address,
                        context.Request.Url.AbsolutePath);

                    switch (context.Request.Url.AbsolutePath)
                    {
                        case "/":
                            if (context.Request.HttpMethod == "GET" || context.Request.UserAgent != "osu!")
                                goto ShowMOTD;
                        
                            context.Response.AddHeader("cho-protocol", Bancho.Protocol.ToString());
                            context.Response.AddHeader("cho-token", "");

                            try
                            {
                                if (string.IsNullOrEmpty(context.Request.Headers.Get("osu-token")))
                                {
                                    //Login
                                    string[] loginContent;
                                    using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                                    {
                                        loginContent = reader.ReadToEnd().Split('\n');
                                    }

                                    if (loginContent.Length == 4)
                                    {
                                        var username = loginContent[0];
                                        Player player;
                                        if (PlayerManager.AuthenticatePlayer(username, loginContent[1], out player))
                                        {
                                            Debug.WriteLine(username + " has logged in.");
                                            context.Response.Headers["cho-token"] = player.Token;

                                            //TODO: Parse others login data like UTC, clientHash, clientVersion
                                            player.IPAddress = context.Request.RemoteEndPoint.Address.ToString();
                                            player.OnLoggedIn();
                                            player.SerializeCommands(outStream);
                                        }
                                        else
                                        {
                                            Debug.WriteLine(username + " has failed to logged in.");

                                            outStream.WriteLoginResult(LoginResult.Failed);
                                        }
                                    }
                                }
                                else
                                {
                                    MemoryStream ms = new MemoryStream(4096);
                                    await context.Request.InputStream.CopyToAsync(ms);

                                    //CopyToAsync vs ReadToEnd
                                    if (!await PlayerManager.OnPacketReceived(context.Request.Headers.Get("osu-token"),
                                        /*new MemoryStream(context.Request.InputStream.ReadToEnd())*/ ms, outStream))
                                    {
                                        context.Response.StatusCode = 403;
                                    }
                                }
                            }
                            catch (HttpListenerException) { /* Ignored */ }
                            catch (CanNotAccessBanchoException)
                            {
                                context.Response.StatusCode = 403;
                            }
                            catch (Exception ex)
                            {
                                // TODO: better exception handling
                                Trace.WriteLine(ex.ToString());

                                outStream.WriteLoginResult(LoginResult.Error);
                            }
                            break;

                        case "/web/bancho_connect.php":
                            //NOTE: Added for localhost test
                            //NOTE: It isn't recommendeded to put handles for /web/ requests on Bancho
                            //RENOTE: I do know that it isn't but it would take the need for additional webstorage and web based stuff away - rapleaqn
                            byte[] bytes = Encoding.Default.GetBytes("br");
                            outStream.Write(bytes, 0, bytes.Length);
                            break;
#if DEBUG
                        case "/web/osu-osz2-getscores.php":
                            NameValueCollection query = context.Request.QueryString;
                            int beatmapId = Convert.ToInt32(query["i"]);
                            string artist = query["f"].Split(new[] { " - " }, StringSplitOptions.None)[0];
                            string creator = query["f"].Split('(')[Counting.Count(query["f"], "(")].Split(')')[0];
                            string title = query["f"].Split(new[] { " - " }, StringSplitOptions.None)[1].Split(new[] { "(" + creator + ")" }, StringSplitOptions.None)[0];
                            title = title.Substring(0, title.Length - 1);
                            string version /*difficulty*/ = query["f"].Split('[')[Counting.Count(query["f"], "[")].Split(']')[0];
                            string fileMd5 = query["c"];
                            var deezNuts = new Scores(beatmapId, artist, creator, "", title, version, fileMd5, 0, 0, query["us"]);
                            deezNuts.getScores();
                            byte[] bytes1 = Encoding.Default.GetBytes($"2|false|648339|{beatmapId}|0\r\n0\r\n[bold:0,size:20]{artist}|{title}\r\n9.28235\r\n");
                            byte[] bytes2 = Encoding.Default.GetBytes("\n");
                            outStream.Write(bytes1, 0, bytes1.Length); //Map Related
                            outStream.Write(bytes2, 0, bytes2.Length); //My Score
                            byte[] memememememeBytes = Encoding.Default.GetBytes("\n"); //OTher Scores
                            outStream.Write(memememememeBytes, 0, memememememeBytes.Length);
                            break;

                        case "/web/osu-submit-modular.php":
                            BeatmapManager.GetAllBeatmaps();
                            string[] _loginContent;
                            using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                            {
                                _loginContent = reader.ReadToEnd().Split('\n');
                            }
                            string iv, score, decryptedScore, passwordhash;
                            byte[] decodedIV;
                            int ivindex = 0, scoreindex = 0, passwordindex = 0;
                            bool ivfound = false, scorefound = false, passwordfound = false;
                            foreach (string s in _loginContent)
                            {
                                if (s != "Content-Disposition: form-data; name=\"iv\"\r" && !ivfound) { 
                                    ivindex++;
                                }else
                                    ivfound = true;
                                if (s != "Content-Disposition: form-data; name=\"score\"\r" && !scorefound)
                                    scoreindex++;
                                else
                                    scorefound = true;
                                if (s != "Content-Disposition: form-data; name=\"pass\"\r" && !passwordfound)
                                    passwordindex++;
                                else
                                    passwordfound = true;
                            }
                            score = _loginContent[scoreindex+2].Replace("\r", "");
                            iv = _loginContent[ivindex+ 2].Replace("\r", "");
                            decryptedScore = AES._AESDecrypt(score,  iv);
                            Submit toSubmit = new Submit(score, iv);
                            toSubmit.SubmitScore();
                            break; 
#endif
                        default:
                            ShowMOTD:
                            if (Bancho.MOTD!=null)
                                outStream.Write(buffer: Bancho.MOTD, offset: 0, count: Bancho.MOTD.Length);
                            break;
                    }
                }

                if (outStream.Length != 0)
                {
                    context.Response.ContentType = "text/html; charset=UTF-8";
                    string acceptEncoding = context.Request.Headers.Get("Accept-Encoding");
                    if (!string.IsNullOrEmpty(acceptEncoding) && acceptEncoding.Contains("gzip"))
                    {
                        context.Response.AddHeader("Content-Encoding", "gzip");
                        using (var gzip = new GZipStream(context.Response.OutputStream, CompressionMode.Compress, true))
                        {
                            outStream.WriteTo(gzip);
                        }
                    }
                    else
                    {
                        outStream.WriteTo(context.Response.OutputStream);
                    }
                }

                // Close the response and send it to the client:
                context.Response.Close();
                //TODO: do a way to try resend packets that has failed to send by response.close
            }
            catch (HttpListenerException) { /* Ignored */ }
            catch (Exception ex)
            {
                // TODO: better exception handling
                Trace.WriteLine(ex.ToString());
            }

#if MEASUREREQ
            sw.Stop();
            Console.WriteLine("Time to complete request: " + sw.Elapsed.ToString());
#endif
        }
    }
}
