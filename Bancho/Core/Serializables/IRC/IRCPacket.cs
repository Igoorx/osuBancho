using osuBancho.Helpers;
using osuBancho.Hosts.IRC;

namespace osuBancho.Core.Serializables.IRC
{
    internal sealed class IRCPacket : IIRCCommand
    {
        public string string_0;

        public void WriteCommandToStream(SerializationWriter writer)
        {
            writer.WriteRawString(string_0);
        }
    }
}