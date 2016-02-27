using osuBancho.Helpers;

namespace osuBancho.IRC
{
    internal interface IIRCCommand
    {
        void WriteCommandToStream(SerializationWriter writer);
    }
}