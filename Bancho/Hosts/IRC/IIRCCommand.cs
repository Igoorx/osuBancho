using osuBancho.Helpers;

namespace osuBancho.Hosts.IRC
{
    internal interface IIRCCommand
    {
        void WriteCommandToStream(SerializationWriter writer);
    }
}