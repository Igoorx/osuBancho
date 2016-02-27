using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using osuBancho.Core;
using osuBancho.Core.Players;
using osuBancho.Helpers;

namespace osuBancho.HTTP
{
    public sealed class HttpAsyncHost
    {
        private readonly int _accepts;
        private HttpListener _listener;

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

        public List<string> Prefixes
        {
            get { return _listener.Prefixes.ToList(); }
        }

        public void Run(params string[] uriPrefixes)
        {
            _listener.IgnoreWriteExceptions = true;

            // Add the server bindings:
            foreach (var prefix in uriPrefixes)
                _listener.Prefixes.Add(prefix);

            Task.Run(() => //NOTE: Esse task é util?
            {
                try
                {
                    // Start the HTTP listener:
                    _listener.Start();
                }
                catch (HttpListenerException hlex)
                {
                    Console.Error.WriteLine(hlex.Message);
                    return;
                }

                // Accept connections:
                // Higher values mean more connections can be maintained yet at a much slower average response time; fewer connections will be rejected.
                // Lower values mean less connections can be maintained yet at a much faster average response time; more connections will be rejected.
#if !DEBUG
                var sem = new Semaphore(_accepts, _accepts);
#else
                var sem = new Semaphore(1, 1); //To no fuck the debug x.x idk if have an better way to do this
#endif

                while (true)
                {
                    sem.WaitOne();

#pragma warning disable 4014
                    _listener.GetContextAsync().ContinueWith(async (t) =>
                    {
                        string errMessage;

                        try
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
                            return;
                        }
                        catch (Exception ex)
                        {
                            errMessage = ex.ToString();
                        }

                        await Console.Error.WriteLineAsync(errMessage);
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

                                //Esse await é util?
                                //TODO: Improve this?
                                if (!await PlayerManager.OnPacketReceived(context.Request.Headers.Get("osu-token"),
                                    /*new MemoryStream(context.Request.InputStream.ReadToEnd())*/ ms, outStream))
                                {
                                    context.Response.StatusCode = 403;
                                }
                            }
                        }
                        catch (HttpListenerException)
                        {
                            //ignored
                        }
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

                    case "/web/bancho_connect.php": //NOTE: Added for localhost test
                        byte[] bytes = Encoding.Default.GetBytes("br");
                        outStream.Write(bytes, 0, bytes.Length);
                        break;

                    default:
                ShowMOTD:
                        outStream.Write(Bancho.MOTD, 0, Bancho.MOTD.Length);
                        break;
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
            catch (HttpListenerException)
            {
                // Ignored.
                Debug.WriteLine("HTTP Error");
            }
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
