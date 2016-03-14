using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osuBancho.Core.Players;
using osuBancho.Database.Interfaces;
using osuBancho.Helpers;

namespace osuBancho.Core.Scores
{
    class Scores
    {
        public int _beatmapId;
        public string _artist;
        public string _creator;
        public string _source;
        public string _title;
        public string _version;
        public string _file_md5;
        public int _player_id;
        public int _mode;
        public string _username;
        public Scores(int beatmapId, string artist, string creator, string source,
            string title, string version, string file_md5, int player_id, int mode, string username)
        {
            _beatmapId = beatmapId;
            _artist = artist;
            _creator = creator;
            _source = source;
            _title = title;
            _version = version;
            _file_md5 = file_md5;
            _player_id = player_id;
            _mode = mode;
            _username = username;
        }

        public Scores(Scores score)
        {
            _beatmapId = score._beatmapId;
            _artist = score._artist;
            _creator = score._creator;
            _source = score._source;
            _title = score._title;
            _version = score._version;
            _file_md5 = score._file_md5;
            _player_id = score._player_id;
            _mode = score._mode;
            _username = score._username;
        }
        public bool isMapInDatabase()
        {
            int isInDatabase = 0;
            using (IQueryAdapter dbClient = Bancho.DatabaseManager.GetQueryReactor())
            {
                dbClient.SetQuery(
                    $"SELECT * FROM beatmaps_info WHERE file_md5 = \"{_file_md5}\" LIMIT 1");
                isInDatabase = dbClient.getInteger();

            }
            if (isInDatabase == 0)
            {
                insertMapIntoDatabase();
                return false;
            }
            return true;
        }

        public void insertMapIntoDatabase()
        {
            string query =
                "INSERT INTO `beatmaps_info`(`approved`, `approved_date`, `last_update`, `set_id`, `artist`, `creator`, `source`, `title`, `version`, `file_md5`) " +
                $"VALUES (2,\"{DateTime.Now.ToString("yyyy-MM-dd")}\",\"{DateTime.Now.ToString("yyyy-MM-dd")}\"," + _beatmapId + ",\"" + _artist + "\",\"" + _creator + "\",\"\",\"" + _title + "\",\"" + _version + "\",\"" + _file_md5 + "\")";
            using (IQueryAdapter dbClient = Bancho.DatabaseManager.GetQueryReactor())
            {
                dbClient.RunQuery(query);
            }
        }

        public string GetUsernameById(int id)
        {
            //SELECT * FROM `users_info` WHERE `id`=1 LIMIT 1
            DataRow userDataRow;
            using (IQueryAdapter dbClient = Bancho.DatabaseManager.GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM users_info WHERE id = @id LIMIT 1");
                dbClient.AddParameter("id", id);
                userDataRow = dbClient.getRow();
            }
            return (string) userDataRow["username"];
        }

        public List<string> AllTheScores = new List<string>(); 
        public void getScores()
        {
            if (isMapInDatabase())
            {
                DataRow scoreDataRow;
                using (IQueryAdapter dbClient = Bancho.DatabaseManager.GetQueryReactor())
                {
                    int i = 1;
                    BeatmapManager.GetAllBeatmaps();
                    dbClient.SetQuery(
                        "SELECT * FROM users_scores_info WHERE beatmap_id = @beatmap AND playMode = @mode ORDER BY total_score DESC");
                    dbClient.AddParameter("beatmap", BeatmapManager.GetBeatmapByHash(_file_md5).id);
                    dbClient.AddParameter("mode", _mode);
                    foreach (DataRow row in dbClient.getTable().Rows)
                    {
                        string ScoreString = ScoreHelper.makeScoreString(0,
                            GetUsernameById(Convert.ToInt32(row["user_id"])),
                            Convert.ToInt32(row["total_score"]),
                            Convert.ToInt32(row["maxcombo"]),
                            Convert.ToInt32(row["count50"]),
                            Convert.ToInt32(row["count100"]),
                            Convert.ToInt32(row["count300"]),
                            Convert.ToInt32(row["countmiss"]),
                            Convert.ToInt32(row["countkatu"]),
                            Convert.ToInt32(row["countgeki"]),
                            Convert.ToInt32(row["perfect"]),
                            Convert.ToInt32(row["enabled_mods"]),
                            Convert.ToInt32(row["user_id"]),
                            i,
                            Convert.ToString(row["date"]));
                        AllTheScores.Add(ScoreString);
                        i++;
                    }
                }
            }
        }

        public int approvedState => 2;

        public new string ToString(Scores score)
        {
            return "Beatmap ID: " + score._beatmapId + " Artist: " + score._artist + " Creator: " + score._creator + " Source: " + score._source + " Title: " + score._title + " Version: " + score._version + " File MD5: " + score._file_md5;
        }
    }
}
