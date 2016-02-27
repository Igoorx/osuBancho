using System;
using System.Collections.Generic;
using System.Text;

namespace osuBancho.Helpers
{
    public class SerializationWriter : System.IO.BinaryWriter
    {
        public SerializationWriter(System.IO.Stream s)
            : base(s, System.Text.Encoding.UTF8)
        {
        }

        public override void Write(string value)
        {
            if (value == null)
            {
                this.Write((byte)0);
            }
            else
            {
                this.Write((byte)11);
                base.Write(value);
            }
        }

        public override void Write(byte[] buffer)
        {
            if (buffer == null)
            {
                this.Write(-1);
            }
            else
            {
                int length = buffer.Length;
                this.Write(length);
                if (length <= 0)
                    return;
                base.Write(buffer);
            }
        }

        public override void Write(char[] chars)
        {
            if (chars == null)
            {
                this.Write(-1);
            }
            else
            {
                int length = chars.Length;
                this.Write(length);
                if (length <= 0)
                    return;
                base.Write(chars);
            }
        }

        public void Write(int[] ints)
        {
            if (ints == null)
            {
                this.Write((short)0);
            }
            else
            {
                int length = ints.Length;
                this.Write((short)length);
                if (length <= 0)
                    return;
                foreach (int _int in ints)
                {
                    base.Write(_int);
                }
            }
        }

        public void Write(List<int> ints)
        {
            if (ints == null)
            {
                this.Write((short)0);
            }
            else
            {
                int length = ints.Count;
                this.Write((short)length);
                if (length <= 0)
                    return;
                foreach (int _int in ints)
                {
                    base.Write(_int);
                }
            }
        }

        public void Write(DateTime dateTime_0)
        {
            this.Write(dateTime_0.ToUniversalTime().Ticks);
        }

        public void Write<T, U>(IDictionary<T, U> idictionary_0)
        {
            if (idictionary_0 == null)
            {
                this.Write(-1);
            }
            else
            {
                this.Write(idictionary_0.Count);
                foreach (KeyValuePair<T, U> keyValuePair in (IEnumerable<KeyValuePair<T, U>>)idictionary_0)
                {
                    this.method_0((object)keyValuePair.Key);
                    this.method_0((object)keyValuePair.Value);
                }
            }
        }

        public void method_0(object object_0)
        {
            if (object_0 == null)
            {
                this.Write((byte)0);
            }
            else
            {
                switch (object_0.GetType().Name)
                {
                    case "Boolean":
                        //this.Write((byte)1);
                        this.Write((bool)object_0);
                        break;
                    case "Byte":
                        //this.Write((byte)2);
                        this.Write((byte)object_0);
                        break;
                    case "UInt16":
                        //this.Write((byte)3);
                        this.Write((ushort)object_0);
                        break;
                    case "UInt32":
                        //this.Write((byte)4);
                        this.Write((uint)object_0);
                        break;
                    case "UInt64":
                        //this.Write((byte)5);
                        this.Write((ulong)object_0);
                        break;
                    case "SByte":
                        //this.Write((byte)6);
                        this.Write((sbyte)object_0);
                        break;
                    case "Int16":
                        //this.Write((byte)7);
                        this.Write((short)object_0);
                        break;
                    case "Int32":
                        //this.Write((byte)8);
                        this.Write((int)object_0);
                        break;
                    case "Int64":
                        //this.Write((byte)9);
                        this.Write((long)object_0);
                        break;
                    case "Char":
                        //this.Write((byte)10);
                        this.Write((char)object_0);
                        break;
                    case "String":
                        //this.Write((byte)11);
                        this.Write((string)object_0);
                        break;
                    case "Single":
                        //this.Write((byte)12);
                        this.Write((float)object_0);
                        break;
                    case "Double":
                        //this.Write((byte)13);
                        this.Write((double)object_0);
                        break;
                    case "Decimal":
                        //this.Write((byte)14);
                        this.Write((Decimal)object_0);
                        break;
                    case "DateTime":
                        //this.Write((byte)15);
                        this.Write((DateTime)object_0);
                        break;
                    case "Byte[]":
                        //this.Write((byte)16);
                        this.Write((byte[])object_0);
                        break;
                    case "Char[]":
                        //this.Write((byte)17);
                        this.Write((char[])object_0);
                        break;
                    case "Int32[]":
                        this.Write((int[]) object_0);
                        break;
                    default:
                        /*this.Write((byte)18);
                        new BinaryFormatter()
                        {
                            AssemblyFormat = FormatterAssemblyStyle.Simple,
                            TypeFormat = FormatterTypeStyle.TypesWhenNeeded
                        }.Serialize(this.BaseStream, object_0);*/
                        break;
                }
            }
        }

        public void WriteRawBytes(byte[] byte_0)
        {
            base.Write(byte_0);
        }

        public void WriteBytes(byte[] byte_0)
        {
            if (byte_0 == null)
            {
                this.Write(-1);
            }
            else
            {
                int length = byte_0.Length;
                this.Write(length);
                if (length <= 0)
                    return;
                base.Write(byte_0);
            }
        }

        internal void WriteRawString(string string_0)
        {
            this.WriteRawBytes(Encoding.UTF8.GetBytes(string_0));
        }
    }
}
