using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osuBancho.Core.Helpers;
using osuBancho.Core.Players;
using osuBancho.Database.Interfaces;

namespace osuBancho.Core.Scores
{
    class Submit
    {
        private int isPass = 2; //0: Fail, 1: Quit, 2: Pass
        private string scoreData = "";

        private string beatmapHash;
        private string username;
        private string verificationHash;
        private int count300;
        private int count100;
        private int count50;
        private int countGeki;
        private int countKatu;
        private int countMiss;
        private int score;
        private int combo;
        private bool FC;
        private string rank;
        private int mods;
        private bool pass;
        private int playmode;
        private string date;
        private int version;

        public Submit(string scoreData, string ivString)
        {
            this.scoreData = AES._AESDecrypt(scoreData, ivString);
            ConvertScoreDataToData();
        }

        private void ConvertScoreDataToData()
        {
            string[] pieces = scoreData.Split(':');
            beatmapHash = pieces[0];
            this.username = pieces[1];
            this.username = this.username.Substring(0, username.Length - 1);
            this.verificationHash = pieces[2];
            this.count300 = Convert.ToInt32(pieces[3]);
            this.count100 = Convert.ToInt32(pieces[4]);
            this.count50 = Convert.ToInt32(pieces[5]);
            this.countGeki = Convert.ToInt32(pieces[6]);
            this.countKatu = Convert.ToInt32(pieces[7]);
            this.countMiss = Convert.ToInt32(pieces[8]);
            this.score = Convert.ToInt32(pieces[9]);
            this.combo = Convert.ToInt32(pieces[10]);
            FC = Convert.ToBoolean(pieces[11]);
            this.rank = pieces[12];
            this.mods = Convert.ToInt32(pieces[13]);
            this.pass = Convert.ToBoolean(pieces[14]);
            this.playmode = Convert.ToInt32(pieces[15]);
            this.date = pieces[16];
            this.version = Convert.ToInt32(pieces[17].Split(new [] { "    " }, StringSplitOptions.None)[0]);
        }

        public void SubmitScore()
        {
            if (pass)
            {
                using(IQueryAdapter dbClient = Bancho.DatabaseManager.GetQueryReactor())
            {
                    dbClient.SetQuery(@"INSERT INTO `users_scores_info`(`user_id`, `username`, `beatmap_id`, `score_id`, `playMode`, `count300`, `count100`, `count50`, `countmiss`, `total_score`, `maxcombo`, `countkatu`, `countgeki`, `perfect`, `enabled_mods`, `date`, `rank`, `pp`)
VALUES(@user_id, @username, @beatmap_id, NULL, @playMode, @count300, @count100, @count50, @countmiss, @score, @combo, @countkatu, @countgeki, @fc, @mods, @date, @rank, 0)");
                    dbClient.AddParameter("user_id", PlayerManager.GetPlayerByUsername(username).Id);
                    dbClient.AddParameter("username", username);
                    dbClient.AddParameter("beatmap_id", BeatmapManager.GetBeatmapByHash(beatmapHash).id);
                    dbClient.AddParameter("playMode", playmode);
                    dbClient.AddParameter("count300", count300);
                    dbClient.AddParameter("count100", count100);
                    dbClient.AddParameter("count50", count50);
                    dbClient.AddParameter("countmiss", countMiss);
                    dbClient.AddParameter("score", score);
                    dbClient.AddParameter("combo", combo);
                    dbClient.AddParameter("countkatu", countKatu);
                    dbClient.AddParameter("countgeki", countGeki);
                    dbClient.AddParameter("fc", FC);
                    dbClient.AddParameter("mods", mods);
                    dbClient.AddParameter("date", date);
                    dbClient.AddParameter("rank", rank);
                    dbClient.RunQuery();
                }
            }
        }
        private bool DidPass => (isPass == 2) ? true : false;
    }
}
