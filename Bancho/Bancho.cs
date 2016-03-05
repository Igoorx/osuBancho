using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Linq;
using MySql.Data.MySqlClient;
using osuBancho.Core.Players;
using osuBancho.Database;
using osuBancho.Database.Interfaces;
using osuBancho.Helpers;
using osuBancho.HTTP;
using osuBancho.Core;

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
        public static bool IsRestricted;
        public static CultureInfo CultureInfo;
        public static DateTime ServerStarted;

        private static Timer workerTimer;

        private static DatabaseManager _databaseManager;
        public static DatabaseManager DatabaseManager => _databaseManager; 

        static void Main()
        {
            ServerStarted = DateTime.Now;

            if (File.Exists("MOTD.txt"))
            {
                var motd = File.ReadAllText("MOTD.txt");
                Console.WriteLine(motd.Substring(0, motd.LastIndexOf("\r\n\r\n")) + Environment.NewLine);

                MOTD = Encoding.Default.GetBytes($"<pre>\n{motd.InsertHrefInUrls()}\n</pre>");
            }

            Console.Write("Initializing Bancho");
            if (IsDebug) Console.Write(" in debug mode");
            Console.WriteLine("..");

            Process.GetCurrentProcess().PriorityBoostEnabled = true;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            Console.CursorVisible = false;
            Console.Title = (IsDebug?"[DEBUG] ":"") + "osu!Bancho";

            GeoUtil.Initialize();

            if (!File.Exists("config.ini"))
                File.WriteAllText("config.ini", IniFile.DefaultIni);

            IniFile ini = new IniFile("config.ini");
            Bancho.IsRestricted = ini.GetValue("Bancho", "Restricted", false);

            CultureInfo = CultureInfo.CreateSpecificCulture("en-GB");

            Console.WriteLine("Initializing Database..");

            var connectionString = new MySqlConnectionStringBuilder
            {
                ConnectionTimeout = ini.GetValue("DatabaseConnection", "ConnectionTimeout", 10u),
                Database = ini.GetValue("DatabaseConnection", "Database", "osu!"),
                DefaultCommandTimeout = ini.GetValue("DatabaseConnection", "CommandTimeout", 30u),
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

            workerTimer = new Timer(
                (state) =>
                    {
                        foreach (Player player in PlayerManager.Players
                            .Where(player => (Environment.TickCount - player.LastPacketTime) >= 80000))
                        {
                            PlayerManager.DisconnectPlayer(player, DisconnectReason.Timeout);
                        }
                        UpdateOnlineNow();
                    },
                null, 0, 15000);

#if DEBUG
            Debug.Listeners.Add(new ConsoleTraceListener());
#endif
            
            var port = ini.GetValue("Bancho", "Port", 80);
            Console.WriteLine($"Initializing HTTP in port {port.ToString()}..");

            HttpAsyncHost http = new HttpAsyncHost(IsDebug? 1 : 120);
            http.Run("http://+:"+port.ToString()+"/");

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
