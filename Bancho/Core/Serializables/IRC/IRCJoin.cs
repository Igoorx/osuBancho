using System;
using osuBancho.Helpers;
using osuBancho.Hosts.IRC;

namespace osuBancho.Core.Serializables.IRC
{
    internal sealed class Class9 : IIRCCommand
    {
        //public Class24 class24_0;

        public string string_0;

        public void WriteCommandToStream(SerializationWriter writer)
        {
/*
            string text = string.Concat(new string[]
			{
				":",
				this.class24_0.method_0(),
				" JOIN :",
				this.string_0,
				"\n"
			});
            string text2 = this.class24_0.method_1();
            string a;
            if (text2.Length > 0 && (a = text2) != null)
            {
                if (!(a == "+"))
                {
                    if (a == "@")
                    {
                        string text3 = text;
                        text = string.Concat(new string[]
						{
							text3,
							":BanchoBot!cho@cho.ppy.sh MODE ",
							this.string_0,
							" +o ",
							this.class24_0.string_1,
							"\n"
						});
                    }
                }
                else
                {
                    string text4 = text;
                    text = string.Concat(new string[]
					{
						text4,
						":BanchoBot!cho@cho.ppy.sh MODE ",
						this.string_0,
						" +v ",
						this.class24_0.string_1,
						"\n"
					});
                }
            }
            osuBinaryWriter0.method_3(text);*/
        }
    }
}