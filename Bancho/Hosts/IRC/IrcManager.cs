using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace osuBancho.Hosts.IRC
{
    public sealed class IrcManager //TODO
    {
        public const string ServerCrLf = "\r\n";
        public const char PrefixCharacter = ':';
        public const string ServerHost = "cho.ppy.sh";

        private static Socket listener;
        public const int _bufferSize = 1024;
        public const int _port = 6667;
        public bool _isRunning = true;

        static bool IsConnected(Socket s)
        {
            return !((s.Poll(1000, SelectMode.SelectRead) && (s.Available == 0)) || !s.Connected);
        }

        public void Start()
        {
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Any, _port));
            listener.Listen(100);

            listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
        }

        static void AcceptCallback(IAsyncResult ar)
        {
            if (listener == null) return;
            try
            {
                Socket handler = ((Socket)ar.AsyncState).EndAccept(ar);

                IrcSession session = new IrcSession(handler);
                session.Start();
            }
            finally
            {
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
            }
        }
    }
}
