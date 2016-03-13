// ReSharper disable InconsistentNaming

namespace osuBancho.Helpers
{
    class ScoreHelper
    {
        public static string makeScoreString(int replayId, string name, int score, int combo, int count50, int count100, int count300, int countMiss, int countKatu, int countGeki, int FC, int mods, int avatarID, int rank, string timestamp)
        {
            return $"{replayId}|{name}|{score}|{combo}|{count50}|{count100}|{count300}|{countMiss}|{countKatu}|{countGeki}|{FC}|{mods}|{avatarID}|{rank}|{timestamp}|1";
        }
    }
}
