using System;
using osuBancho.Helpers;
using osuBancho.Hosts.IRC;

namespace osuBancho.Core.Serializables.IRC
{
    internal sealed class bIRCQuit : bSerializable, IIRCCommand
    {
        public Enum1 enum1_0;
        public int UserId;
        public string QuitReason;

        public bIRCQuit(SerializationReader reader)
        {
            UserId = reader.ReadInt32();
            enum1_0 = (Enum1) reader.ReadByte();
        }

        public bIRCQuit(int id, Enum1 enum1)
        {
            UserId = id;
            enum1_0 = enum1;
        }

        public void ReadFromStream(SerializationReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteToStream(SerializationWriter writer)
        {
            writer.Write(UserId);
            writer.Write((byte) enum1_0);
        }

        public void WriteCommandToStream(SerializationWriter writer)
        {
            /* if (this.enum1_0 != Enum1.const_0)
            {
                return;
            }
            string text = string.Concat(new string[]
			{
				":",
				this.class24_0.method_0(),
				" QUIT :",
				this.string_0,
				"\n"
			});
            osuBinaryWriter0.method_3(text);*/
        }

        //private Class24 class24_0;

        internal enum Enum1
        {
            const_0,
            const_1,
            const_2
        }
    }
}