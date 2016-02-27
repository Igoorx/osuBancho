using osuBancho.Helpers;

namespace osuBancho.Core.Serializables
{
    public interface bSerializable
    {
        void ReadFromStream(SerializationReader reader);
        void WriteToStream(SerializationWriter writer);
    }
}