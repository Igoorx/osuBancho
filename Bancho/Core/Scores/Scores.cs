using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osuBancho.Database.Interfaces;

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
        public Scores(int beatmapId, string artist, string creator, string source,
            string title, string version, string file_md5, int player_id, int mode)
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
        }
        public bool isMapInDatabase()
        {
            DataRow scoreDataRow;
            using (IQueryAdapter dbClient = Bancho.DatabaseManager.GetQueryReactor())
            {
                dbClient.SetQuery(
                    "SELECT * FROM beatmaps_info WHERE set_id = " + _beatmapId + " AND file_md5 = \"" + _file_md5 + "\" LIMIT 1");
                scoreDataRow = dbClient.getRow();
            }
            if (scoreDataRow == null)
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
                "VALUES (3,\"0000-00-00\",\"0000-00-00\"," + _beatmapId + ",\""+_artist+"\",\""+_creator+"\",\"\",\""+_title+"\",\""+_version+"\",\""+_file_md5+"\")";
            using (IQueryAdapter dbClient = Bancho.DatabaseManager.GetQueryReactor())
            {
                dbClient.RunQuery(query);
            }
        }

        public void getScores()
        {
            bool isPersonalScore = (_player_id != 0) ? true : false;
            if (!isPersonalScore)
            {
                DataRow scoreDataRow;
                using (IQueryAdapter dbClient = Bancho.DatabaseManager.GetQueryReactor())
                {
                    dbClient.SetQuery(
                        "SELECT * FROM users_scores_info WHERE beatmap_id = :beatmap AND playMode = :mode ORDER BY total_score DESC");
                    dbClient.AddParameter(":beatmap", _beatmapId);
                    dbClient.AddParameter(":mode", _mode);
                    scoreDataRow = dbClient.getRow();
                }
            }
            else
            {
                DataRow scoreDataRow;
                using (IQueryAdapter dbClient = Bancho.DatabaseManager.GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT * FROM users_scores_info WHERE beatmap_id = :beatmap AND user_id = :playerid AND playMode = :mode ORDER BY total_score DESC LIMIT 1");
                    dbClient.AddParameter(":beatmap", _beatmapId);
                    dbClient.AddParameter(":mode", _mode);
                    dbClient.AddParameter(":playerid", _player_id);
                    scoreDataRow = dbClient.getRow();
                }
            }
        }

        public new string ToString(Scores score)
        {
            return "Beatmap ID: " + score._beatmapId + " Artist: " + score._artist + " Creator: " + score._creator + " Source: " + score._source + " Title: " + score._title + " Version: " + score._version + " File MD5: " + score._file_md5;
        }
    }
}
