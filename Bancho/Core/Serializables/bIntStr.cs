using System;
using osuBancho.Helpers;

namespace osuBancho.Core.Serializables
{
    internal struct bIntStr : bSerializable
    {
        public readonly int @int;
        public readonly string str;

        public bIntStr(int @int, string str)
        {
            this.@int = @int;
            this.str = str;
        }

        public bIntStr(SerializationReader reader)
        {
            @int = reader.ReadInt32();
            str = reader.ReadString();
        }

        public void ReadFromStream(SerializationReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteToStream(SerializationWriter writer)
        {
            writer.Write(@int);
            writer.Write(str);
        }
    }
}