using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osuBancho.Core.Players;
using osuBancho.Database.Interfaces;

namespace osuBancho.Core.Scores
{
    internal class BeatmapManager
    {
        private static List<Beatmap> Beatmaps = new List<Beatmap>();
        public static Beatmap GetBeatmapByHash(string hash) => Beatmaps.FirstOrDefault(beatmap => beatmap.hash == hash);
        public static Beatmap GetBeatmapById(int id) => Beatmaps.FirstOrDefault(beatmap => beatmap.id == id);

        public static void GetAllBeatmaps()
        {
            using (IQueryAdapter dbClient = Bancho.DatabaseManager.GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM beatmaps_info");
                foreach (DataRow row in dbClient.getTable().Rows)
                {
                    Beatmap temp = new Beatmap(row);
                    Beatmaps.Add(temp);
                }
            }
        }
    }
}
