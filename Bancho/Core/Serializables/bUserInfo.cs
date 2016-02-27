using System;
using osuBancho.Helpers;

namespace osuBancho.Core.Serializables
{
    internal sealed class bUserInfo : bSerializable
    {
        public readonly UserTags Tags;
        public bool NotNegativeId;
        public byte CountryID;
        public float Longitude;
        public float Latitude;
        public int ID;
        public int idk;
        public string Name;
        public PlayModes PlayMode;
        public int TimeZone;

        public bUserInfo(int ID, string Name, int TimeZone, byte CountryID, UserTags Tags, PlayModes PlayMode,
            float Longitude, float Latitude, int idk)
        {
            this.ID = ID;
            if (this.ID < 0)
            {
                this.ID = -this.ID;
            }
            else
            {
                NotNegativeId = (this.ID != 0);
            }
            this.Name = Name;
            this.TimeZone = TimeZone;
            this.CountryID = CountryID;
            this.Tags = Tags;
            this.PlayMode = PlayMode;
            this.Longitude = Longitude;
            this.Latitude = Latitude;
            this.idk = idk;
        }

        public bUserInfo(SerializationReader r)
        {
            ID = r.ReadInt32();
            if (ID < 0)
            {
                ID = -ID;
            }
            else
            {
                NotNegativeId = (ID != 0);
            }
            Name = r.ReadString();
            TimeZone = r.ReadByte() - 24;
            CountryID = r.ReadByte();
            var b = r.ReadByte();
            Tags = ((UserTags) b & (UserTags) (-225));
            PlayMode = (PlayModes) Math.Max(0, Math.Min(3, (b & 224) >> 5));
            Longitude = r.ReadSingle();
            Latitude = r.ReadSingle();
            idk = r.ReadInt32();
        }

        public void ReadFromStream(SerializationReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteToStream(SerializationWriter writer)
        {
            writer.Write(NotNegativeId ? ID : (-ID));
            writer.Write(Name);
            writer.Write((byte) (TimeZone + 24));
            writer.Write(CountryID);
            writer.Write((byte) ((byte) Tags | (byte) PlayMode << 5));
            writer.Write(Longitude);
            writer.Write(Latitude);
            writer.Write(idk);
        }
    }
}