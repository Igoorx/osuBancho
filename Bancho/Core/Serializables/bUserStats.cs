using System;
using osuBancho.Helpers;

namespace osuBancho.Core.Serializables
{
    internal sealed class bUserStats : bSerializable
    {
        public bUserStatus Status;
        public float Accuracy;
        public int playCount;
        public int rankPosition;
        public int Id;
        public long totalScore;
        public long rankedScore;
        public short perfomancePoints;

        public bUserStats(int ID, bUserStatus info, long totalScore, float Accuracy, int playCount, long rankedScore, int rankPosition,
            short pp)
        {
            Id = ID;
            Status = info;
            this.totalScore = totalScore;
            this.Accuracy = Accuracy;
            this.playCount = playCount;
            this.rankedScore = rankedScore;
            this.rankPosition = rankPosition;
            this.perfomancePoints = pp;
        }

        public bUserStats(SerializationReader reader)
        {
            Id = reader.ReadInt32(); 
            Status = new bUserStatus(reader);
            totalScore = reader.ReadInt64();
            Accuracy = reader.ReadSingle();
            playCount = reader.ReadInt32();
            rankedScore = reader.ReadInt64();
            rankPosition = reader.ReadInt32();
            perfomancePoints = reader.ReadInt16();
        }

        public void ReadFromStream(SerializationReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteToStream(SerializationWriter writer)
        {
            writer.Write(Id);
            Status.WriteToStream(writer);
            writer.Write(totalScore);
            writer.Write(Accuracy);
            writer.Write(playCount);
            writer.Write(rankedScore);
            writer.Write(rankPosition);
            writer.Write(perfomancePoints);
        }
    }
}