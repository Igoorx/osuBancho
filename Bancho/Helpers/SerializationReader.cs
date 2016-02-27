namespace osuBancho.Helpers
{
    public class SerializationReader : System.IO.BinaryReader
    {
        public SerializationReader(System.IO.Stream s)
            : base(s, System.Text.Encoding.UTF8)
        {
        }

        public override string ReadString()
        {
            if (this.ReadByte() == 0)
            {
                return null;
            }
            return base.ReadString();
        }

        public byte[] ReadByteArray()
        {
            int num = this.ReadInt32();
            if (num > 0)
            {
                return this.ReadBytes(num);
            }
            if (num < 0)
            {
                return null;
            }
            return new byte[0];
        }

        public char[] ReadCharArray()
        {
            int num = this.ReadInt32();
            if (num > 0)
            {
                return this.ReadChars(num);
            }
            if (num < 0)
            {
                return null;
            }
            return new char[0];
        }

        public int[] ReadInts()
        {
            short num = this.ReadInt16();
            if (num < 0)
            {
                return null;
            }
            int[] ints = new int[num];
            for (int i = 0; i < num; i++)
            {
                ints[i] = this.ReadInt32();
            }
            return ints;
        }

        public System.DateTime ReadDateTime()
        {
            long num = this.ReadInt64();
            if (num < 0L)
            {
                throw new System.Threading.AbandonedMutexException("oops");
            }
            return new System.DateTime(num, System.DateTimeKind.Utc);
        }

        public System.Collections.Generic.IDictionary<T, U> method_3<T, U>()
        {
            int num = this.ReadInt32();
            if (num < 0)
            {
                return null;
            }
            System.Collections.Generic.IDictionary<T, U> dictionary = new System.Collections.Generic.Dictionary<T, U>();
            for (int i = 0; i < num; i++)
            {
                dictionary[(T)((object)this.ReadObject())] = (U)((object)this.ReadObject());
            }
            return dictionary;
        }

        public object ReadObject()
        {
            switch (this.ReadByte())
            {
                case 1:
                    return this.ReadBoolean();
                case 2:
                    return this.ReadByte();
                case 3:
                    return this.ReadUInt16();
                case 4:
                    return this.ReadUInt32();
                case 5:
                    return this.ReadUInt64();
                case 6:
                    return this.ReadSByte();
                case 7:
                    return this.ReadInt16();
                case 8:
                    return this.ReadInt32();
                case 9:
                    return this.ReadInt64();
                case 10:
                    return this.ReadChar();
                case 11:
                    return base.ReadString();
                case 12:
                    return this.ReadSingle();
                case 13:
                    return this.ReadDouble();
                case 14:
                    return this.ReadDecimal();
                case 15:
                    return this.ReadDateTime();
                case 16:
                    return this.ReadByteArray();
                case 17:
                    return this.ReadCharArray();
                /*case 18:
                    return BinaryObjectSerializer.Deserialize(this.BaseStream);*/
                default:
                    return null;
            }
        }
    }
}
