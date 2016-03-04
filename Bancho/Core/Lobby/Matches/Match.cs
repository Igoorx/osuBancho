using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osuBancho.Core.Players;
using osuBancho.Core.Serializables;

namespace osuBancho.Core.Lobby.Matches
{
    class Match
    {
        public readonly int Id;
        internal bool Locked;

        private bMatchData matchData;
        public bMatchData MatchData => matchData;

        private readonly ConcurrentDictionary<int, Player> _players = new ConcurrentDictionary<int, Player>();
        public IEnumerable<Player> Players => _players.Select(item => item.Value);
        //TODO: Do anything like an MatchPlayer?

        private int _playingCount;
        private int _playFinishCount;
        private int _skipRequestCount;
        private int _loadFinishCount;
        private object _matchLock = new object();

        public Match(int id, Player owner, bMatchData matchData)
        {
            this.Id = id;
            this.matchData = matchData;
            this.matchData.matchId = id;

            SetHost(owner, false);
            AddPlayer(owner, false);
        }

        public void Dispose(bool fromlobby = false)
        {
            if (_players.Count > 0)
            {
                matchData.slotId = new int[bMatchData.MaxRoomPlayers];
                this.SendMatchUpdate();
                _players.Clear();
            }
            matchData = null;

            if (!fromlobby) LobbyManager.MatchDisposed(this.Id);
        }

        public bool IsFull;

        private bool CheckIsFull()
        {
            return matchData.GetOpenSlotsCount() == 0;
        }

        public bool IsHost(int playerId)
        {
            return playerId == matchData.hostId;
        }

        public void SendMatchUpdate()
        {
            foreach (Player player in Players)
            {
                player.QueueCommand(Commands.OUT_MatchUpdate, this.matchData);
            }
            LobbyManager.SendMatchUpdate(this.matchData);
        }

        public bool AddPlayer(Player player, bool sendMatchUpdate=true)
        {
            if (IsFull) return false;
            if (!_players.TryAdd(player.Id, player)) return false;

            foreach (Player mplayer in Players)
            {
                mplayer.QueueCommand(Commands.OUT_UpdateUserState, player.SerializableStats);
            }

            for (int i = 0; i < bMatchData.MaxRoomPlayers-1; i++)
            {
                if (matchData.slotStatus[i] != SlotStatus.Open) continue;
                matchData.slotStatus[i] = SlotStatus.NotReady;
                matchData.slotId[i] = player.Id;
                if (matchData.IsTeamMode)
                    matchData.slotTeam[i] = i%2 == 0 ? SlotTeams.Blue : SlotTeams.Red;

                break;
            }
            if (sendMatchUpdate) this.SendMatchUpdate();
            IsFull = CheckIsFull();

            player._matchLoadFinished = false;
            player._matchPlayFinished = false;
            player._matchSkipRequested = false;

            player.QueueCommand(Commands.OUT_MatchJoinSuccess, this.matchData);
            //TODO Match Channel
            return true;
        }

        public void RemovePlayer(int playerId)
        {
            Player player;
            if (!_players.TryRemove(playerId, out player)) return;
            if (_players.IsEmpty)
            {
                Dispose();
                return;
            }
            IsFull = CheckIsFull();

            int slotPos = matchData.GetPlayerSlotPos(playerId);

            if (matchData.inProgress && player.IsMultiplaying)
            {
                if (!player._matchLoadFinished)
                    OnPlayerEndLoad();
                if (!player._matchSkipRequested)
                    OnPlayerSkip(playerId, true);
                if (!player._matchPlayFinished)
                    OnPlayerEndMatch();
            }

            if (slotPos != -1)
            {
                matchData.slotId[slotPos] = -1;
                matchData.slotStatus[slotPos] = SlotStatus.Open;
                matchData.slotMods[slotPos] = Mods.None;
                if (matchData.hostId == playerId)
                {
                    SetHost(Players.First());
                }
            }
            SendMatchUpdate();
        }

        public void StartMatch()
        {
            if (matchData.inProgress) return;
            SetLocked(matchData.inProgress = true);

            _playingCount = 0;
            _playFinishCount = 0;
            _loadFinishCount = 0;
            _skipRequestCount = 0;

            for (int i = 0; i < bMatchData.MaxRoomPlayers - 1; i++)
            {
                if (matchData.slotStatus[i] == SlotStatus.NoMap || matchData.slotId[i] == -1) continue;
                Player player;
                if (!_players.TryGetValue(matchData.slotId[i], out player)) continue;
                if (player.Status.status != bStatus.Multiplayer && player.Status.status != bStatus.Afk) continue;

                matchData.slotStatus[i] = SlotStatus.Playing;
                _playingCount++;

                player.QueueCommand(Commands.OUT_MatchStart, this.matchData);
            }
            if (_playingCount == 0) SetLocked(matchData.inProgress = false);

            //NOTE: This sendmatchupdate is useless when a player start the match..
            this.SendMatchUpdate();
        }

        public void FinishMatch(bool forced=false)
        {
            if (!matchData.inProgress) return;
            SetLocked(matchData.inProgress = false);

            for (int i = 0; i < bMatchData.MaxRoomPlayers - 1; i++)
            {
                if (matchData.slotStatus[i] != SlotStatus.Playing) continue;

                Player player;
                if (!_players.TryGetValue(matchData.slotId[i], out player)) continue;

                matchData.slotStatus[i] = SlotStatus.NotReady;

                player.QueueCommand(forced ? Commands.OUT_MatchAbort : Commands.OUT_MatchComplete);
            }

            this.SendMatchUpdate();
        }

        public void SetLocked(bool value)
        {
            this.Locked = value;
        }

        //BUG: rarely, when two requests to "On" are do at same time this will make the int to not increment..

        public void OnPlayerEndMatch()
        {
            lock (_matchLock) //NOTE: Try to fix the rare bug
            {
                if (++_playFinishCount < _playingCount)
                    return;

                this.FinishMatch();
            }
        }

        public void OnPlayerSkip(int id, bool noSendRequest=false)
        {
            lock (_matchLock)
            {
                bool sendMatchSkip = (++_skipRequestCount >= _playingCount);

                foreach (Player player in Players.Where(player => player.IsMultiplaying))
                {
                    if (sendMatchSkip)
                        player.QueueCommand(Commands.OUT_MatchSkip);
                    else if (!noSendRequest)
                        player.QueueCommand(Commands.OUT_MatchSkipRequest, this.matchData.GetPlayerSlotPos(id));
                }
            }
        }

        public void OnPlayerEndLoad()
        {
            lock (_matchLock)
            {
                if (++_loadFinishCount < _playingCount)
                    return;

                foreach (Player player in Players.Where(player => player.IsMultiplaying))
                {
                    player._matchSkipRequested = true;
                    player.QueueCommand(Commands.OUT_MatchAllPlayersLoaded);
                }
            }
        }

        public void OnPlayerFail(int id)
        {
            foreach (Player player in Players.Where(player => player.IsMultiplaying))
            {
                player?.QueueCommand(Commands.OUT_MatchPlayerFailed, this.matchData.GetPlayerSlotPos(id));
            }
        }

        public void OnPlayerScoreUpdate(int id, bScoreData data)
        {
            data.byte_0 = (byte) this.matchData.GetPlayerSlotPos(id);
            foreach (Player player in Players.Where(player => player.IsMultiplaying))
            {
                player?.QueueCommand(Commands.OUT_MatchScoreUpdate, data);
            }
        }

        //NOTE: When a match is created or when a match end (and he back to room), the host send an packet to change match settings, but, why?
        public void SetMatchData(bMatchData newMatchData)
        {
            if (newMatchData.inProgress) return; //NOTE: The host sends an packet to bancho with playing room status when the match end

            bool toggledToFreeMod = newMatchData.specialModes != this.matchData.specialModes &&
                                    newMatchData.specialModes == MultiSpecialModes.FreeMod;
            Mods mods = newMatchData.activeMods;
            if (toggledToFreeMod)
            {
                newMatchData.activeMods = Mods.None;
                if (mods.HasFlag(Mods.DoubleTime))
                {
                    mods &= ~Mods.DoubleTime;
                    if (mods.HasFlag(Mods.Nightcore))
                    {
                        mods &= ~Mods.Nightcore;
                        newMatchData.activeMods = Mods.DoubleTime | Mods.Nightcore;
                    } else newMatchData.activeMods = Mods.DoubleTime;
                }
                if (mods.HasFlag(Mods.HalfTime))
                {
                    mods &= ~Mods.HalfTime;
                    newMatchData.activeMods = Mods.HalfTime;
                }
            }

            bool toggledToTeamMode = newMatchData.IsTeamMode && !this.matchData.IsTeamMode;
            bool toggledToNormalMode = !newMatchData.IsTeamMode && this.matchData.IsTeamMode;

            for (int i = 0; i < bMatchData.MaxRoomPlayers - 1; i++)
            {
                if (newMatchData.slotId[i] == -1) continue;
                if (newMatchData.slotStatus[i] == SlotStatus.Ready &&
                    newMatchData.slotStatus[i] != SlotStatus.NoMap) newMatchData.slotStatus[i] = SlotStatus.NotReady;
                if (toggledToFreeMod) newMatchData.slotMods[i] = mods;
                if (toggledToTeamMode) newMatchData.slotTeam[i] = i%2 == 0 ? SlotTeams.Blue : SlotTeams.Red;
                else if (toggledToNormalMode) newMatchData.slotTeam[i] = SlotTeams.Neutral;
            }

            if (newMatchData.gamePassword == "") newMatchData.gamePassword = null;
            else if (this.matchData.HasPassword) newMatchData.gamePassword = this.matchData.gamePassword;

            this.matchData = newMatchData;
            this.SendMatchUpdate();
        }

        public void SetHasMap(bool has, int playerId)
        {
            int slotPos = matchData.GetPlayerSlotPos(playerId);
            matchData.slotStatus[slotPos] = has ? SlotStatus.NotReady : SlotStatus.NoMap;
            this.SendMatchUpdate();
        }

        public void SetReady(bool ready, int playerId)
        {
            int slotPos = matchData.GetPlayerSlotPos(playerId);
            if (matchData.slotStatus[slotPos] == SlotStatus.NoMap || matchData.slotStatus[slotPos] == SlotStatus.Playing)
                return;
            matchData.slotStatus[slotPos] = ready ? SlotStatus.Ready : SlotStatus.NotReady;
            this.SendMatchUpdate();
        }

        public void LockSlot(int slotPos)
        {
            bool almostClosed = matchData.GetNotLockedSlotsCount() == 2;

            if (matchData.slotStatus[slotPos] == SlotStatus.Locked)
            {
                matchData.slotStatus[slotPos] = SlotStatus.Open;
            }
            else
            {
                if (matchData.slotId[slotPos] == matchData.hostId) return;
                matchData.slotStatus[slotPos] = almostClosed ? SlotStatus.Open : SlotStatus.Locked;
                matchData.slotId[slotPos] = -1;
                matchData.slotMods[slotPos] = Mods.None;
            }
            SendMatchUpdate();
        }

        public void MovePlayerSlot(int playerId, int slotPos)
        {
            if (slotPos < 0 || slotPos > bMatchData.MaxRoomPlayers) return;
            if (matchData.slotStatus[slotPos] != SlotStatus.Open) return;

            int currentSlotPos = matchData.GetPlayerSlotPos(playerId);

            matchData.slotId[slotPos] = matchData.slotId[currentSlotPos];
            matchData.slotStatus[slotPos] = matchData.slotStatus[currentSlotPos];
            matchData.slotMods[slotPos] = matchData.slotMods[currentSlotPos];
            matchData.slotTeam[slotPos] = matchData.slotTeam[currentSlotPos];

            matchData.slotId[currentSlotPos] = -1;
            matchData.slotStatus[currentSlotPos] = SlotStatus.Open;
            matchData.slotMods[currentSlotPos] = Mods.None;
            matchData.slotTeam[currentSlotPos] = SlotTeams.Neutral;

            this.SendMatchUpdate();
        }

        public void SetMods(int playerId, Mods mods)
        {
            bool removePlayersReady = false;

            if (matchData.specialModes == MultiSpecialModes.FreeMod)
            {
                #region Check Speed mods
                if (IsHost(playerId))
                {
                    if (mods.HasFlag(Mods.DoubleTime))
                    {
                        mods &= ~Mods.DoubleTime;
                        removePlayersReady = true;
                        if (mods.HasFlag(Mods.Nightcore))
                        {
                            mods &= ~Mods.Nightcore;
                            matchData.activeMods = Mods.DoubleTime | Mods.Nightcore;
                        } else matchData.activeMods = Mods.DoubleTime;
                    }
                    else if (matchData.activeMods == Mods.DoubleTime ||
                             matchData.activeMods == (Mods.DoubleTime | Mods.Nightcore))
                        matchData.activeMods = Mods.None;
                    if (mods.HasFlag(Mods.HalfTime))
                    {
                        mods &= ~Mods.HalfTime;
                        matchData.activeMods = Mods.HalfTime;
                        removePlayersReady = true;
                    }
                    else if (matchData.activeMods == Mods.HalfTime)
                        matchData.activeMods = Mods.None;
                }
                #endregion

                this.matchData.slotMods[matchData.GetPlayerSlotPos(playerId)] = mods;
            }
            else if (this.IsHost(playerId))
            {
                this.matchData.activeMods = mods;
                removePlayersReady = true;
            }

            if (removePlayersReady)
                for (int i = 0; i < bMatchData.MaxRoomPlayers - 1; i++)
                {
                    if (matchData.slotStatus[i] == SlotStatus.Ready) matchData.slotStatus[i] = SlotStatus.NotReady;
                }

            this.SendMatchUpdate();
        }

        public void SetMods(Mods mods)
        {
            this.matchData.activeMods = mods;

            for (int i = 0; i < bMatchData.MaxRoomPlayers - 1; i++)
            {
                if (matchData.slotStatus[i] == SlotStatus.Ready) matchData.slotStatus[i] = SlotStatus.NotReady;
            }

            this.SendMatchUpdate();
        }

        public void SetHost(Player player, bool sendMatchUpdate=true)
        {
            matchData.hostId = player.Id;
            player.QueueCommand(Commands.OUT_MatchTransferHost);

            if (sendMatchUpdate) this.SendMatchUpdate();
        }

        public void SetHost(int slotPos)
        {
            if (slotPos<0 || slotPos > bMatchData.MaxRoomPlayers) return;

            Player player = PlayerManager.GetPlayerById(matchData.slotId[slotPos]);
            if (player == null) return;

            matchData.hostId = player.Id;
            player.QueueCommand(Commands.OUT_MatchTransferHost);

            this.SendMatchUpdate();
        }
    }
}
