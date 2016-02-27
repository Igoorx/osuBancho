using System;
using osuBancho.Core.Serializables;
using osuBancho.Helpers;

namespace osuBancho.IRC.Objects
{
    internal sealed class bIRCMessage : bSerializable, IIRCCommand
    {
        public int int_0;
        public string Message;
        public object Source;
        public string Target;

        public bIRCMessage(object Source, string target, string Message)
        {
            this.Source = (Source ?? string.Empty);
            this.Message = Message;
            Target = target;
            /*Class24 @class = this.Source as Class24;
            if (@class != null)
            {
                this.int_0 = @class.int_0;
            }*/
        }

        public bIRCMessage(SerializationReader reader)
        {
            Source = reader.ReadString();
            Message = reader.ReadString();
            Target = reader.ReadString();
            if (Bancho.Protocol > 14)
            {
                int_0 = reader.ReadInt32();
            }
        }

        public void ReadFromStream(SerializationReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteToStream(SerializationWriter writer)
        {
            writer.Write(Source.ToString());
            writer.Write(Message);
            var value = Target;
            if (Target != null)
            {
                if (Target.StartsWith("#mp_"))
                {
                    value = "#multiplayer";
                }
                else if (Target.StartsWith("#spect_"))
                {
                    value = "#spectator";
                }
            }
            writer.Write(value);
            writer.Write(int_0);
        }

        //TODO: Osu Bancho IRC
        //TODO: Search for more parts like this!
        public void WriteCommandToStream(SerializationWriter writer)
        {
/*
            Class24 @class = this.Source as Class24;
            string text;
            if (@class != null)
            {
                text = @class.method_0();
            }
            else
            {
                text = this.Source.ToString();
            }
            osuBinaryWriter0.method_3(string.Concat(new string[]
			{
				":",
				text,
				" PRIVMSG ",
				this.Target.Replace(' ', '_'),
				" :",
				this.Message,
				"\n"
			}));*/
        }

        public bool method_0()
        {
            return Target.Length == 0 || Target[0] != '#';
        }
    }
}