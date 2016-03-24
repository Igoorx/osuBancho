using System.Collections.Generic;

namespace osuBancho.Hosts.IRC.Channels
{
    static class ChannelManager //TODO
    {
        private static readonly Dictionary<string, Channel> channels = new Dictionary<string, Channel>();
        public static Dictionary<string, Channel> Channels => channels;

        public static void Initialize()
        {
            channels.Add("#osu", new Channel());
            channels.Add("#broadcast", new Channel());
        }
    }
}
