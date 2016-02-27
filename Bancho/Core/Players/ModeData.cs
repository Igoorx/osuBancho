using System.Data;
using osuBancho.Helpers;

namespace osuBancho.Core.Players
{
    internal struct ModeData
    {
        internal uint RankPosition;
        internal uint TotalScore;
        internal uint RankedScore;
        internal uint PlayCount;
        internal float Accuracy;
        internal short PerformancePoints;

        internal ModeData(DataRow row)
        {
            this.RankPosition = (uint)(int) row["pp_rank"]; //This is useless because we calc later 
            this.TotalScore = (uint) row["total_score"];
            this.RankedScore = (uint) row["ranked_score"];
            this.PlayCount = (uint) row["playcount"];
            this.Accuracy = Utils.CalcAccuracy((uint) row["countmiss"], (uint) row["count50"], (uint) row["count100"], (uint) row["count300"]);
            this.PerformancePoints = (short)(int) row["pp_raw"];
        }
    }
}
