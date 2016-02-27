using System;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;
using osuBancho.Core.Players;
using osuBancho.Database;
using osuBancho.Database.Interfaces;
using osuBancho.Helpers;
using osuBancho.HTTP;

namespace osuBancho
{
    static class Bancho
    {
        public static byte[] MOTD = Encoding.Default.GetBytes("<pre>\n" + File.ReadAllText("MOTD.txt").InsertHrefInUrls() + "\n</pre>");
        public static byte Protocol = 19;
#if DEBUG
        public static bool IsDebug = true;
#else
        public static bool IsDebug = false;
#endif
        public static bool IsRestricted = true;
        public static CultureInfo CultureInfo;
        public static DateTime ServerStarted;

        private static DatabaseManager _databaseManager;
        public static DatabaseManager DatabaseManager
        {
            get { return _databaseManager; }
        }

        static void Main()
        {
            ServerStarted = DateTime.Now;
            Console.Write("Initializing Bancho");
            if (IsDebug) Console.Write(" in debug mode");
            Console.WriteLine("..");

            Process.GetCurrentProcess().PriorityBoostEnabled = true;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            Console.CursorVisible = false;
            Console.Title = "osu!Bancho";

            CultureInfo = CultureInfo.CreateSpecificCulture("en-GB"); //TODO anything with this?
            //TODO config file

            Console.WriteLine("Initializing Database..");

            var connectionString = new MySqlConnectionStringBuilder
            {
                ConnectionTimeout = 10,
                Database = "osu!",
                DefaultCommandTimeout = 30,
                Logging = false,
                MaximumPoolSize = 250,
                MinimumPoolSize = 10,
                Password = "root",
                Pooling = true,
                Port = 3306,
                Server = "127.0.0.1",
                UserID = "root",
                AllowZeroDateTime = true,
                ConvertZeroDateTime = true,
            };

            _databaseManager = new DatabaseManager(connectionString.ToString());
            if (!_databaseManager.IsConnected())
            {
                Console.Error.WriteLine("Failed to connect to the specified MySQL server.");
                Console.ReadKey(true);
                Environment.Exit(1);
                return;
            }

            UpdateOnlineNow();
            //TODO: Do a worker thread to update periodcally the onlines now, and kill zombies

#if DEBUG
            Debug.Listeners.Add(new ConsoleTraceListener());
#endif

            Console.WriteLine("Initializing HTTP..");
            HttpAsyncHost http = new HttpAsyncHost(120);
            Console.WriteLine("Bancho is UP!");
            http.Run(new[] { "http://+:890/" });

            Console.ReadLine();
        }

        public static void UpdateOnlineNow()
        {
            using (IQueryAdapter dbClient = Bancho.DatabaseManager.GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE osu_info SET value=@value WHERE name=@name");
                dbClient.AddParameter("name", "online_now");
                dbClient.AddParameter("value", PlayerManager.PlayersCount.ToString());
                dbClient.RunQuery();
            }
        }
    }
}
