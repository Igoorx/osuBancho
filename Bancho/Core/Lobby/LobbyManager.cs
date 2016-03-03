﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using osuBancho.Core.Lobby.Matches;
using osuBancho.Core.Players;
using osuBancho.Core.Serializables;

namespace osuBancho.Core.Lobby
{
    static class LobbyManager
    {
        private static readonly ConcurrentDictionary<int, Player> PlayersById = new ConcurrentDictionary<int, Player>();
        private static readonly ConcurrentDictionary<int, Match> MatchesById = new ConcurrentDictionary<int, Match>();
        private static int _lastMatchId;

        public static IEnumerable<Player> Players => PlayersById.Select(item => item.Value);

        public static IEnumerable<Match> Matches => MatchesById.Select(item => item.Value);

        public static void EnterLobby(Player player)
        {
            if (player.currentMatch != null){
                player.currentMatch.RemovePlayer(player.Id);
                player.currentMatch = null;
            }
            PlayersById.TryAdd(player.Id, player);

            //NOTE: Idk if this is the right way to do this...
            foreach (Match match in Matches)
            {
                foreach (Player mplayer in match.Players)
                {
                    player.QueueCommand(Commands.OUT_UpdateUserState, mplayer.SerializableStats);
                }

                player.QueueCommand(Commands.OUT_MatchNew, match.MatchData);
            }
        }

        public static void ExitLobby(int playerId)
        {
            Player player;
            PlayersById.TryRemove(playerId, out player);
        }

        public static void SendMatchUpdate(bMatchData matchData)
        {
            foreach (Player player in Players)
            {
                player.QueueCommand(Commands.OUT_MatchUpdate, matchData);
            }
        }

        public static bool TryEnterMatch(Player player, int id, string password)
        {
            if (player.currentMatch != null)
            {
                player.currentMatch.RemovePlayer(player.Id);
                player.currentMatch = null;
            }

            Match match;
            if (!MatchesById.TryGetValue(id, out match)) return false;
            if ((!string.IsNullOrEmpty(match.MatchData.gamePassword) && match.MatchData.gamePassword != password) ||
                !match.AddPlayer(player)) return false;
            player.currentMatch = match;
            return true;
        }

        public static void CreateMatch(Player owner, bMatchData matchData)
        {
            if (owner.currentMatch != null)
            {
                owner.currentMatch.RemovePlayer(owner.Id);
                owner.currentMatch = null;
            }

            //NOTE: _lastMatchId isn't safe 
            Match match = new Match(++_lastMatchId, owner, matchData);
            if (!MatchesById.TryAdd(match.Id, match))
            {
                match.Dispose(true);
                return;
            }
            owner.currentMatch = match;

            foreach (Player player in Players)
            {
                if (player.Id == owner.Id) continue;

                player.QueueCommand(Commands.OUT_UpdateUserState, owner.SerializableStats);
                player.QueueCommand(Commands.OUT_MatchNew, match.MatchData);
            }
        }

        public static void MatchDisposed(int matchId)
        {
            Match match;
            if (!MatchesById.TryRemove(matchId, out match)) return;
            foreach (Player player in Players)
            {
                player.QueueCommand(Commands.OUT_MatchDisband, matchId);
            }
        }
    }
}
