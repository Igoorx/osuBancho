using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using osuBancho.Core.Serializables.IRC;
using osuBancho.Database.Interfaces;
using osuBancho.Helpers;
using osuBancho.Hosts.IRC;

namespace osuBancho.Core.Players
{
    static class PlayerManager
    {
        private static readonly ConcurrentDictionary<string, Player> PlayersByToken = new ConcurrentDictionary<string, Player>();
        private static readonly ConcurrentDictionary<int, Player> PlayersById = new ConcurrentDictionary<int, Player>();
        
        public static IEnumerable<int> PlayersIds => PlayersById.Select(item => item.Key);

        public static IEnumerable<Player> Players => PlayersById.Select(item => item.Value);

        public static int PlayersCount => PlayersById.Skip(0).Count();

        public static Player GetPlayerBySessionToken(string token)
        {
            Player player;
            PlayersByToken.TryGetValue(token, out player);
            return player;
        }

        public static Player GetPlayerById(int id)
        {
            Player player;
            PlayersById.TryGetValue(id, out player);
            return player;
        }

        public static Player GetPlayerByUsername(string username)
        {
            return Players.FirstOrDefault(player => player.Username == username);
        }

        //TODO?: do worker things on a threaded OnCycle method, to kill zombies and update others things like onlines now (or onlines now is better in a separated worker thread?)

        public static void QueueCommandForAll(Commands command, object serializable)
        {
            foreach (Player player in Players)
            {
                player.QueueCommand(command, serializable);
            }
        }

        public static void QueueCommandForAll(Commands command, object serializable, int exclude)
        {
            foreach (Player player in Players)
            {
                if (player.Id == exclude) continue;
                player.QueueCommand(command, serializable);
            }
        }

        public static bool AuthenticatePlayer(string username, string passHash, out Player player)
        {
            DataRow playerData;
            using (IQueryAdapter dbClient = Bancho.DatabaseManager.GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * " +
                                  "FROM users_info " +
                                  "WHERE username = @name AND " +
                                  "password = @password LIMIT 1");

                dbClient.AddParameter("name", username);
                dbClient.AddParameter("password", passHash); //TODO: if is from irc, check the irc password
                playerData = dbClient.getRow();
            }
            if (playerData == null)
            {
                player = null;
                return false;
            }

            player = new Player(Guid.NewGuid().ToString(), playerData);
            if (Bancho.IsRestricted && !(player.Tags.HasFlag(UserTags.Admin) || player.Tags.HasFlag(UserTags.Peppy)))
            {
                throw new CanNotAccessBanchoException();
            }

            if (!PlayersById.TryAdd(player.Id, player))
            {
                if (!player.Tags.HasFlag(UserTags.TournamentStaff) /*&& Bancho.isTourneyMode*/)
                {
                    DisconnectPlayer(player.Id, DisconnectReason.Kick); //TODO?: Use TryUpdate?
                    if (!PlayersById.TryAdd(player.Id, player))
                        //TODO?: should i implement an temporary ip ban to floods of login?
                        throw new Exception("Can't disconnect the another player!");
                }
            }
            else
            {
                //TODO: Improve this?
                QueueCommandForAll(Commands.OUT_UserForLoad, player.Id, player.Id);
                //NOTE: Osu automatic logout when see that an user with same id has logged in
            }

            if (PlayersByToken.TryAdd(player.Token, player))
                QueueCommandForAll(Commands.OUT_IrcMessage,
                    new bIRCMessage("BanchoBot", "#broadcast", $"New session: {player.Token}") {SenderId = 3});
            //NOTE: Test message

            return true;
        }

        public static void DisconnectPlayer(int playerId, DisconnectReason reason)
        {
            Player player;
            if (!PlayersById.TryRemove(playerId, out player)) return;
            if (PlayersByToken.TryRemove(player.Token, out player))
                QueueCommandForAll(Commands.OUT_IrcMessage, new bIRCMessage("BanchoBot", "#broadcast", $"Destroyed session: {player.Token}") { SenderId = 3 }); //NOTE: Test message

            player.Dispose();
            //NOTE: Improve this?
            QueueCommandForAll(Commands.OUT_UserQuit, new bIRCQuit(playerId, bIRCQuit.Enum1.const_0));

            Debug.WriteLine("{0} has disconnected ({1})", player.Username, reason);
        }

        public static void DisconnectPlayer(Player player, DisconnectReason reason)
        {
            Player _player;
            if (PlayersByToken.TryRemove(player.Token, out _player))
                QueueCommandForAll(Commands.OUT_IrcMessage, new bIRCMessage("BanchoBot", "#broadcast", $"Destroyed session: {player.Token}") { SenderId = 3 }); //NOTE: Test message

            if (PlayersById.TryRemove(player.Id, out player))
            {
                player.Dispose();
                //NOTE: Improve this?
                QueueCommandForAll(Commands.OUT_UserQuit, new bIRCQuit(player.Id, bIRCQuit.Enum1.const_0));
            }

            Debug.WriteLine("{0} has disconnected ({1})", player?.Username, reason);
        }

        /*public static async Task<bool> OnPacketReceived(string Token, Stream receivedStream, MemoryStream outStream)
        {
            if (receivedStream.Length < 7)
                return false;

            Player player = GetPlayerBySessionToken(Token);

            if (player == null) return false;
            receivedStream.Position = 0;
               
            await Task.Run(() => player.OnPacketReceived(receivedStream));
            player.SerializeCommands(outStream);
                
            return true;
        }*/
    }
}
