using System;
using System.Diagnostics;
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
        public static byte[] MOTD;
        public static byte Protocol = 19;
#if DEBUG
        public static bool IsDebug = true;
#else
        public static bool IsDebug = false;
#endif
        public static bool IsRestricted = false;
        public static CultureInfo CultureInfo;
        public static DateTime ServerStarted;

        private static DatabaseManager _databaseManager;
        public static DatabaseManager DatabaseManager => _databaseManager; 

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

            if (File.Exists("MOTD.txt"))
                MOTD = Encoding.Default.GetBytes($"<pre>\n{File.ReadAllText("MOTD.txt").InsertHrefInUrls()}\n</pre>");

            if (!File.Exists("config.ini"))
                File.WriteAllText("config.ini", IniFile.DefaultIni);

            IniFile ini = new IniFile("config.ini");

            CultureInfo = CultureInfo.CreateSpecificCulture("en-GB");

            Console.WriteLine("Initializing Database..");

            var connectionString = new MySqlConnectionStringBuilder
            {
                ConnectionTimeout = ini.GetValue("DatabaseConnection", "Timeout", 10u),
                Database = ini.GetValue("DatabaseConnection", "Database", "osu!"),
                DefaultCommandTimeout = 30,
                Logging = false,
                MaximumPoolSize = ini.GetValue("DatabaseConnection", "MaximumPoolSize", 250u),
                MinimumPoolSize = ini.GetValue("DatabaseConnection", "MinimumPoolSize", 10u),
                Password = ini.GetValue("DatabaseConnection", "Password", ""),
                Pooling = true,
                Port = ini.GetValue("DatabaseConnection", "Port", 3306u),
                Server = ini.GetValue("DatabaseConnection", "Server", "127.0.0.1"),
                UserID = ini.GetValue("DatabaseConnection", "User", "root"),
                AllowZeroDateTime = true,
                ConvertZeroDateTime = true,
            };

            _databaseManager = new DatabaseManager(connectionString.ToString());
            if (!_databaseManager.IsConnected())
            {
                Console.Error.WriteLine("Failed to connect to the specified MySQL server.");
                Console.ReadKey(true);
                Environment.Exit(1);
            }

            UpdateOnlineNow();
            //TODO: Do a worker thread to update periodcally the onlines now, and kill zombies

#if DEBUG
            Debug.Listeners.Add(new ConsoleTraceListener());
#endif

            Console.WriteLine("Initializing HTTP..");
            HttpAsyncHost http = new HttpAsyncHost(IsDebug? 1 : 120);
            http.Run("http://+:"+ini.GetValue("Bancho", "Port", "80")+"/");

            Console.ReadLine();
        }

        public static void UpdateOnlineNow()
        {
            using (IQueryAdapter dbClient = DatabaseManager.GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE osu_info SET value=@value WHERE name=@name");
                dbClient.AddParameter("name", "online_now");
                dbClient.AddParameter("value", PlayerManager.PlayersCount.ToString());
                dbClient.RunQuery();
            }
        }
    }
}
