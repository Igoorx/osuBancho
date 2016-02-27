using osuBancho.Helpers;

namespace osuBancho.IRC.Objects
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