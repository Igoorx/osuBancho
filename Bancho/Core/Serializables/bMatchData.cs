using System;
using osuBancho.Helpers;

namespace osuBancho.Core.Serializables
{
    [Serializable]
    public class bMatchData : ICloneable, bSerializable
    {
        public Mods activeMods;
        public string beatmapChecksum;
        public int beatmapId = -1;
        public string beatmapName;
        public string gameName;
        public string gamePassword;
        public int hostId;
        public bool inProgress;
        public int matchId;
        public MatchScoringTypes matchScoringType;
        public MatchTeamTypes matchTeamType;
        public MatchTypes matchType;
        public PlayModes playMode;
        public int Seed;
        protected bool SendPassword;
        public int[] slotId = new int[MaxRoomPlayers];
        public Mods[] slotMods = new Mods[MaxRoomPlayers];
        public SlotStatus[] slotStatus = new SlotStatus[MaxRoomPlayers];
        public SlotTeams[] slotTeam = new SlotTeams[MaxRoomPlayers];
        public MultiSpecialModes specialModes;

        public bMatchData(MatchTypes matchType, MatchScoringTypes matchScoringType,
            MatchTeamTypes matchTeamType, PlayModes playMode, string gameName, string gamePassword, int maxSlots,
            string beatmapName, string beatmapChecksum, int beatmapId, Mods activeMods, int hostId, MultiSpecialModes multiSpecialMode,
            int Seed)
        {
            this.matchType = matchType;
            this.playMode = playMode;
            this.matchScoringType = matchScoringType;
            this.matchTeamType = matchTeamType;
            this.gameName = gameName;
            this.gamePassword = gamePassword;
            this.beatmapName = beatmapName;
            this.beatmapChecksum = beatmapChecksum;
            this.beatmapId = beatmapId;
            this.activeMods = activeMods;
            this.hostId = hostId;
            this.specialModes = multiSpecialMode;
            this.Seed = Seed;
            this.SendPassword = true;
            for (var i = 0; i < MaxRoomPlayers; i++)
            {
                slotStatus[i] = ((i < maxSlots) ? SlotStatus.Open : SlotStatus.Locked);
                slotId[i] = -1;
            }
            if (gameName.Length > 50)
            {
                gameName = gameName.Remove(50);
            }
        }

        public bMatchData(SerializationReader reader)
        {
            SendPassword = false;
            matchId = reader.ReadUInt16();
            inProgress = reader.ReadBoolean();
            matchType = (MatchTypes) reader.ReadByte();
            activeMods = (Mods) reader.ReadUInt32();
            gameName = reader.ReadString();
            gamePassword = reader.ReadString();
            beatmapName = reader.ReadString();
            beatmapId = reader.ReadInt32();
            beatmapChecksum = reader.ReadString();
            for (var i = 0; i < MaxRoomPlayers; i++)
            {
                slotStatus[i] = (SlotStatus) reader.ReadByte();
            }
            for (var j = 0; j < MaxRoomPlayers; j++)
            {
                slotTeam[j] = (SlotTeams) reader.ReadByte();
            }
            for (var k = 0; k < MaxRoomPlayers; k++)
            {
                slotId[k] = (((slotStatus[k] & SlotStatus.Occupied) > (SlotStatus) 0)
                    ? reader.ReadInt32()
                    : -1);
            }
            hostId = reader.ReadInt32();
            playMode = (PlayModes) reader.ReadByte();
            matchScoringType = (MatchScoringTypes) reader.ReadByte();
            matchTeamType = (MatchTeamTypes) reader.ReadByte();
            specialModes = (MultiSpecialModes) reader.ReadByte();
            if (gameName.Length > 50)
            {
                gameName = gameName.Remove(50);
            }
            if ((specialModes & MultiSpecialModes.FreeMod) > MultiSpecialModes.None)
            {
                for (var l = 0; l < MaxRoomPlayers; l++)
                {
                    slotMods[l] = (Mods) reader.ReadInt32();
                }
            }
            Seed = reader.ReadInt32();
        }

        public static int MaxRoomPlayers
        {
            get
            {
                if (Bancho.Protocol <= 18)
                {
                    return 8;
                }
                return 16;
            }
        }

        public bool HasPassword
        {
            get { return gamePassword != null; }
        }

        public void ReadFromStream(SerializationReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteToStream(SerializationWriter writer)
        {
            writer.Write((short) matchId);
            writer.Write(inProgress);
            writer.Write((byte) matchType);
            writer.Write((uint) activeMods);
            writer.Write(gameName);
            writer.Write((SendPassword || gamePassword == null)
                ? gamePassword
                : string.Empty);
            writer.Write(beatmapName);
            writer.Write(beatmapId);
            writer.Write(beatmapChecksum);
            for (var i = 0; i < MaxRoomPlayers; i++)
            {
                writer.Write((byte) slotStatus[i]);
            }
            for (var j = 0; j < MaxRoomPlayers; j++)
            {
                writer.Write((byte) slotTeam[j]);
            }
            for (var k = 0; k < MaxRoomPlayers; k++)
            {
                if ((slotStatus[k] & SlotStatus.Occupied) > 0)
                {
                    writer.Write(slotId[k]);
                }
            }
            writer.Write(hostId);
            writer.Write((byte) playMode);
            writer.Write((byte) matchScoringType);
            writer.Write((byte) matchTeamType);
            writer.Write((byte) specialModes);
            if ((specialModes & MultiSpecialModes.FreeMod) > MultiSpecialModes.None)
            {
                for (var l = 0; l < MaxRoomPlayers; l++)
                {
                    writer.Write((int) slotMods[l]);
                }
            }
            writer.Write(Seed);
        }

        public object Clone()
        {
            var bMatchData = MemberwiseClone() as bMatchData;
            bMatchData.slotStatus = (SlotStatus[]) slotStatus.Clone();
            bMatchData.slotId = (int[]) slotId.Clone();
            bMatchData.slotTeam = (SlotTeams[]) slotTeam.Clone();
            bMatchData.slotMods = (Mods[]) slotMods.Clone();
            return bMatchData;
        }

        public int GetOccupiedSlotsCount()
        {
            var num = 0;
            for (var i = 0; i < MaxRoomPlayers; i++)
            {
                if ((slotStatus[i] & SlotStatus.Occupied) > 0)
                {
                    num++;
                }
            }
            return num;
        }

        public int GetPlayingSlotsCount()
        {
            var num = 0;
            for (var i = 0; i < MaxRoomPlayers; i++)
            {
                if ((slotStatus[i] & SlotStatus.Playing) > 0)
                {
                    num++;
                }
            }
            return num;
        }

        public int GetNotLockedSlotsCount()
        {
            var num = 0;
            for (var i = 0; i < MaxRoomPlayers; i++)
            {
                if (slotStatus[i] != SlotStatus.Locked)
                {
                    num++;
                }
            }
            return num;
        }

        public int GetOpenSlotsCount()
        {
            var num = 0;
            for (var i = 0; i < MaxRoomPlayers; i++)
            {
                if (slotStatus[i] == SlotStatus.Open)
                {
                    num++;
                }
            }
            return num;
        }

        public int GetReadySlotsCount()
        {
            var num = 0;
            for (var i = 0; i < MaxRoomPlayers; i++)
            {
                if (slotStatus[i] == SlotStatus.Ready)
                {
                    num++;
                }
            }
            return num;
        }

        public bool IsTeamMode
        {
            get { return matchTeamType == MatchTeamTypes.TagTeamVs || matchTeamType == MatchTeamTypes.TeamVs; }
        }

        public bool IsInvalidTeam
        {
            get
            {
                if (!IsTeamMode)
                {
                    return true;
                }
                SlotTeams? slotTeam = null;
                for (var i = 0; i < MaxRoomPlayers; i++)
                {
                    if ((slotStatus[i] & SlotStatus.Occupied) > 0 &&
                        (slotStatus[i] & SlotStatus.NoMap) == 0)
                    {
                        if (!slotTeam.HasValue)
                        {
                            slotTeam = this.slotTeam[i];
                        }
                        else if (slotTeam != this.slotTeam[i])
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public int GetPlayerSlotPos(int int_0)
        {
            int i;
            for (i = 0; i < MaxRoomPlayers; i++)
            {
                if (slotId[i] == int_0)
                {
                    break;
                }
            }
            if (i > MaxRoomPlayers - 1)
            {
                return -1;
            }
            return i;
        }

        public bool IsR16()
        {
            for (var i = 8; i < MaxRoomPlayers; i++)
            {
                if (slotStatus[i] != SlotStatus.Locked)
                {
                    return true;
                }
            }
            return false;
        }
    }
}