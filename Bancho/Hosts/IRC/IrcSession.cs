using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using osuBancho.Core.Players;
using osuBancho.Helpers;

namespace osuBancho.Hosts.IRC
{
    class IrcSession : BaseIrcSession
    {
        private Socket dataSocket;
        private string Ip;
        private string Username;
        private string Password;
        private bool IsConnected;

        /// <summary>
        /// Buffer of the connection
        /// </summary>
        private byte[] buffer;

        public IrcSession(Socket dataSocket)
        {
            this.buffer = new byte[1024];
            this.dataSocket = dataSocket;
        }

        public void Disconnect()
        {
            if (this.IsConnected)
            {
                this.IsConnected = false;
                Debug.WriteLine("IRC session disconnected");

                dataSocket.Dispose();
            }
        }

        public void SendCommandBytes(byte[] data)
        {
            dataSocket.BeginSend(data, 0, data.Length, SocketFlags.None
                        , null, dataSocket);
        }

        public void SendCommand(string command, string[] args)
        {
            byte[] data = Encoding.Default.GetBytes($":{IrcManager.ServerHost} {command} {this.Username} :{string.Join(" ", args)}");
            this.SendCommandBytes(data);
        }

        /// <summary>
        /// Start the receivement of packets by the session
        /// </summary>
        public void Start()
        {
            if (!IsConnected)
            {
                this.IsConnected = true;
                Debug.WriteLine("New IRC session created");

                try
                {
                    this.dataSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None,
                        new AsyncCallback(IncomingDataPacket), dataSocket);
                }
                catch
                {
                    this.Disconnect();
                }
            }
        }


        /// <summary>
        /// Receives a packet of data and processes it
        /// </summary>
        /// <param name="iAr">The interface of an async result</param>
        private void IncomingDataPacket(IAsyncResult iAr)
        {
            int bytesReceived;
            try
            {
                //The amount of bytes received in the packet
                bytesReceived = dataSocket.EndReceive(iAr);
            }
            catch
            {
                this.Disconnect();
                return;
            }

            if (bytesReceived == 0)
            {
                this.Disconnect();
                return;
            }

            try
            {
                byte[] packet = new byte[bytesReceived];
                Array.Copy(buffer, packet, bytesReceived);
                
                using (StringReader sr = new StringReader(Encoding.Default.GetString(packet)))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] lineSplitted = line.Split(new []{ ' ' }, 2);
                        string command = lineSplitted[0];
                        string[] args = lineSplitted[1].Split(' ');

                        Debug.WriteLine($"IRC Line received: {line}");
                        this.HandleIRCCommand(command, args);
                    }
                }
            }
            catch (Exception e)
            {
                this.Disconnect();
                Debug.WriteLine("Error in packet handling" + Environment.NewLine + e);
            }
            finally
            {
                if (IsConnected)
                    try
                    {
                        this.dataSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None,
                            new AsyncCallback(IncomingDataPacket), dataSocket);
                    }
                    catch
                    {
                        this.Disconnect();
                    }
            }
        }

        public void HandleIRCCommand(string command, string[] args)
        {
            switch (command)
            {
                case "PASS":
                    this.Password = args[0];
                    break;

                case "NICK":
                    this.Username = args[0];
                    break;

                case "USER":
                    byte[] bytesToSend =
                        Encoding.UTF8.GetBytes(":cho.ppy.sh 372 %USERNAME% :Welcome to osu!bancho.\x0D\x0A" +
                                               ":cho.ppy.sh 372 %USERNAME% :-\x0D\x0A" +
                                               ":cho.ppy.sh 372 %USERNAME% :- You are required to authenticate before accessing this service.\x0D\x0A" +
                                               ":cho.ppy.sh 372 %USERNAME% :- Please click the following link to receive your password:\x0D\x0A" +
                                               ":cho.ppy.sh 372 %USERNAME% :- https://osu.ppy.sh/p/irc\x0D\x0A" +
                                               ":cho.ppy.sh 372 %USERNAME% :-\x0D\x0A" +
                                               ":cho.ppy.sh 464 %USERNAME% :Bad authentication token.\x0D\x0A"
                                               .Replace("%USERNAME%", this.Username));
                    this.SendCommandBytes(bytesToSend);
                    Password = null;
                    this.Disconnect();
                    break;
            }
        }
    }
}
