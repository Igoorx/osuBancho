using System;
using osuBancho.Helpers;

namespace osuBancho.Core.Serializables
{
    internal sealed class bUserStatus : bSerializable
    {
        public string beatmap;
        public string beatmapHash;
        public int idk;
        public Mods mods;
        public PlayModes playMode;
        public bStatus status;

        public bUserStatus(bStatus status, string beatmapHash, string beatmap, Mods mods, PlayModes playMode,
            int idk)
        {
            this.status = status;
            this.beatmap = beatmap;
            this.beatmapHash = beatmapHash;
            this.mods = mods;
            this.playMode = playMode;
            this.idk = idk;
        }

        public bUserStatus(SerializationReader reader)
        {
            status = (bStatus) reader.ReadByte();
            beatmapHash = reader.ReadString();
            beatmap = reader.ReadString();
            if (Bancho.Protocol > 10)
            {
                mods = (Mods) reader.ReadUInt32();
            }
            else
            {
                mods = (Mods) reader.ReadInt16();
            }
            playMode = (PlayModes) Math.Max((byte) 0, Math.Min((byte) 3, reader.ReadByte()));
            idk = reader.ReadInt32();
        }

        public void ReadFromStream(SerializationReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteToStream(SerializationWriter writer)
        {
            writer.Write((byte) status);
            writer.Write(beatmapHash);
            writer.Write(beatmap);
            writer.Write((uint) mods);
            writer.Write((byte) playMode);
            writer.Write(idk);
        }
    }
}