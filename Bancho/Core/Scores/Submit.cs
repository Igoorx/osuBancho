using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osuBancho.Core.Helpers;
using osuBancho.Core.Players;
using osuBancho.Database.Interfaces;
using osuBancho.Helpers;

// ReSharper disable ArrangeThisQualifier
// ReSharper disable InconsistentNaming

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
        private string passwordHash;

        public Submit(string scoreData, string ivString, string passwordhash)
        {
            this.scoreData = AES._AESDecrypt(scoreData, ivString);
            ConvertScoreDataToData();
            this.passwordHash = passwordhash;
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
            this.version = Convert.ToInt32(pieces[17].Split(new[] { "    " }, StringSplitOptions.None)[0]);
        }

        public void SubmitScore()
        {
            if (pass)
            {
                using (IQueryAdapter dbClient = Bancho.DatabaseManager.GetQueryReactor())
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
            //Updates Userpanel
            UpdateUsersModesInfo();
        }

        public DataRow GetUsersModesInfoRow()
        {
            using (IQueryAdapter dbClient = Bancho.DatabaseManager.GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `users_modes_info` WHERE `user_id`=@userid AND `mode_id`=@modeid LIMIT 1");
                dbClient.AddParameter("userid", PlayerManager.GetPlayerByUsername(username).Id);
                dbClient.AddParameter("modeid", playmode);
                return dbClient.getRow();
            }
        }

        public void UpdateUsersModesInfo()
        {
            DataRow Info = GetUsersModesInfoRow();
            using (IQueryAdapter dbClient = Bancho.DatabaseManager.GetQueryReactor())
            {
                #region if info isnt null
                if (Info != null)
                {
                    int sscount = 0, scount = 0, acount = 0;
                    dbClient.SetQuery("UPDATE `users_modes_info` SET `count300`= @count300,`count100`= @count100,`count50`= @count50,`countmiss`= @countmiss,`playcount`= @playcount,`total_score`= @totalscore,`ranked_score`= @rankedscore,`pp_rank`= @pprank,`pp_raw`= @ppraw,`count_rank_ss`= @sscount,`count_rank_s`= @scount,`count_rank_a`=@acount,`pp_country_rank`= @countryrank WHERE `user_id`= @userid");
                    dbClient.AddParameter("count300", Convert.ToInt32(Info["count300"]) + count300);
                    dbClient.AddParameter("count100", Convert.ToInt32(Info["count100"]) + count100);
                    dbClient.AddParameter("count50", Convert.ToInt32(Info["count50"]) + count50);
                    dbClient.AddParameter("countmiss", Convert.ToInt32(Info["countmiss"]) + countMiss);
                    dbClient.AddParameter("playcount", Convert.ToInt32(Info["playcount"]) + 1);
                    dbClient.AddParameter("totalscore", Convert.ToInt32(Info["total_score"]) + score);
                    dbClient.AddParameter("rankedscore", Convert.ToInt32(Info["ranked_score"]) + score);
                    dbClient.AddParameter("pprank", Convert.ToInt32(Info["pp_rank"]) + 0);
                    dbClient.AddParameter("ppraw", Convert.ToInt32(Info["pp_raw"]) + 0);
                    if (countMiss == 0 && count50 == 0 && count100 == 0 && count300 != 0)
                    {
                        sscount = Convert.ToInt32(Info["count_rank_ss"]) + 1;
                        scount = Convert.ToInt32(Info["count_rank_s"]) + 0;
                        acount = Convert.ToInt32(Info["count_rank_a"]) + 0;
                    }
                    else if (countMiss == 0 && Utils.CalcAccuracy((uint) countMiss, (uint) count50, (uint) count100, (uint) count300) >= 95)
                    {
                        sscount =  Convert.ToInt32(Info["count_rank_ss"]) + 0;
                        scount = Convert.ToInt32(Info["count_rank_s"]) + 1;
                        acount = Convert.ToInt32(Info["count_rank_a"]) + 0;
                    }
                    else if (Utils.CalcAccuracy((uint) countMiss, (uint) count50, (uint) count100, (uint) count300) >= 90)
                    {
                        sscount = Convert.ToInt32(Info["count_rank_ss"]) + 0;
                        scount = Convert.ToInt32(Info["count_rank_s"]) + 0;
                        acount = Convert.ToInt32(Info["count_rank_a"]) + 1;
                    }
                    dbClient.AddParameter("sscount", sscount);
                    dbClient.AddParameter("scount", scount);
                    dbClient.AddParameter("acount", acount);
                    dbClient.AddParameter("countryrank", Convert.ToInt32(Info["pp_country_rank"]) + 0);
                    dbClient.AddParameter("userid", PlayerManager.GetPlayerByUsername(username).Id);
                    dbClient.RunQuery();
                    //TODO: Implement Userpanel updating
                }
                #endregion
                #region if it is null
                else
                {
                    int sscount = 0, scount = 0, acount = 0;
                    dbClient.SetQuery("INSERT INTO `users_modes_info`(`user_id`, `mode_id`, `count300`, `count100`, `count50`, `countmiss`, `playcount`, `total_score`, `ranked_score`, `pp_rank`, `pp_raw`, `count_rank_ss`, `count_rank_s`, `count_rank_a`, `pp_country_rank`) VALUES " +
                                      "(@userid,@modeid,@count300,@count100,@count50,@countMiss,@playcount,@totalscore,@rankedscore,@pprank,@ppraw,@sscount,@scount,@acount,@countryrank)");
                    dbClient.AddParameter("userid", PlayerManager.GetPlayerByUsername(username).Id);
                    dbClient.AddParameter("modeid", playmode);

                    dbClient.AddParameter("count300", count300);
                    dbClient.AddParameter("count100", count100);
                    dbClient.AddParameter("count50",  count50);
                    dbClient.AddParameter("countmiss", countMiss);
                    dbClient.AddParameter("playcount", 1);
                    dbClient.AddParameter("totalscore", score);
                    dbClient.AddParameter("rankedscore", score);
                    dbClient.AddParameter("pprank", 0);
                    dbClient.AddParameter("ppraw", 0);
                    if (countMiss == 0 && count50 == 0 && count100 == 0 && count300 != 0)
                    {
                        sscount =  1;
                        scount = 0;
                        acount = 0;
                    }
                    else if (countMiss == 0 && Utils.CalcAccuracy((uint)countMiss, (uint)count50, (uint)count100, (uint)count300) >= 95)
                    {
                        sscount = 0;
                        scount = 1;
                        acount = 0;
                    }
                    else if (Utils.CalcAccuracy((uint)countMiss, (uint)count50, (uint)count100, (uint)count300) >= 90)
                    {
                        sscount = 0;
                        scount = 0;
                        acount = 1;
                    }
                    dbClient.AddParameter("sscount", sscount);
                    dbClient.AddParameter("scount", scount);
                    dbClient.AddParameter("acount", acount);
                    dbClient.AddParameter("countryrank", 1);
                    dbClient.RunQuery();
                    //TODO: Implement Userpanel updating
                }
                #endregion
            }
        }
        private bool DidPass => (isPass == 2) ? true : false;
    }
}
