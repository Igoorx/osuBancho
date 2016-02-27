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
        private bMatchData matchData;

        public bMatchData MatchData
        {
            get { return matchData; }
        }

        private readonly ConcurrentDictionary<int, Player> _players = new ConcurrentDictionary<int, Player>();
        
        public IEnumerable<Player> Players
        {
            get { return _players.Select(item => item.Value); } //.Values; } //TODO: Is this a good way to do this?
        }

        //TODO: Do anything better than this, like an MatchPlayer?
        //For now this is fine, i think
        private Player[] playingPlayers;

        internal bool Locked;

        private int PlayingCount;
        private int PlayFinishCount;
        private int SkipRequestCount;
        private int LoadFinishCount;
        
        public Match(int id, Player owner, bMatchData matchData)
        {
            this.Id = id;
            this.matchData = matchData;
            this.matchData.matchId = id;

            SetHost(owner, false);
            AddPlayer(owner, false);
        }

        public void Dispose()
        {
            if (_players.Count > 0)
            {
                matchData.slotId = new int[bMatchData.MaxRoomPlayers];
                this.SendMatchUpdate();
                _players.Clear();
            }
            matchData = null;

            LobbyManager.MatchDisposed(this.Id);
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
                this.playingPlayers[slotPos] = null;
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

            PlayingCount = 0;
            PlayFinishCount = 0;
            LoadFinishCount = 0;
            SkipRequestCount = 0;
            playingPlayers = new Player[bMatchData.MaxRoomPlayers];

            for (int i = 0; i < bMatchData.MaxRoomPlayers - 1; i++)
            {
                if (matchData.slotStatus[i] == SlotStatus.NoMap || matchData.slotId[i] == -1) continue;
                Player player;
                if (!_players.TryGetValue(matchData.slotId[i], out player)) continue;
                if (player.Status.status != bStatus.Multiplayer && player.Status.status != bStatus.Afk) continue;

                matchData.slotStatus[i] = SlotStatus.Playing;
                playingPlayers[i] = player;
                PlayingCount++;

                player.QueueCommand(Commands.OUT_MatchStart, this.matchData);
            }
            if (PlayingCount == 0) SetLocked(matchData.inProgress = false);

            //TODO: This sendmatchupdate is useless when a player start the match..
            this.SendMatchUpdate();
        }

        public void FinishMatch(bool forced=false)
        {
            if (!matchData.inProgress) return;
            SetLocked(matchData.inProgress = false);

            for (int i = 0; i < bMatchData.MaxRoomPlayers - 1; i++)
            {
                if (matchData.slotStatus[i] != SlotStatus.Playing) continue;
                Player player = playingPlayers[i];
                //if (!_players.TryGetValue(matchData.slotId[i], out player)) continue;

                matchData.slotStatus[i] = SlotStatus.NotReady;
                playingPlayers[i] = null;

                player.QueueCommand(forced ? Commands.OUT_MatchAbort : Commands.OUT_MatchComplete);
            }

            this.SendMatchUpdate();
        }

        public void SetLocked(bool value)
        {
            this.Locked = value;
            //value?the room is now locked:the room no more is locked
        }

        //BUG: rarely, when two requests to "On" are do at same time this will make the int to not increment..

        public void OnPlayerEndMatch()
        {
            Interlocked.Increment(ref PlayFinishCount);
            if (PlayFinishCount < PlayingCount)
                return;

            this.FinishMatch();
        }

        public void OnPlayerSkip(int id, bool noSendRequest=false)
        {
            Interlocked.Increment(ref SkipRequestCount);
            bool sendMatchSkip = (SkipRequestCount >= PlayingCount);

            foreach (Player player in playingPlayers)
            {
                if (player == null) continue;
                if (sendMatchSkip)
                    player.QueueCommand(Commands.OUT_MatchSkip);
                else if (!noSendRequest)
                    player.QueueCommand(Commands.OUT_MatchSkipRequest, this.matchData.GetPlayerSlotPos(id));
            }
        }

        public void OnPlayerFail(int id)
        {
            foreach (Player player in playingPlayers)
            {
                if (player == null) continue;
                player.QueueCommand(Commands.OUT_MatchPlayerFailed, this.matchData.GetPlayerSlotPos(id));
            }
        }

        public void OnPlayerScoreUpdate(int id, bScoreData data)
        {
            data.byte_0 = (byte) this.matchData.GetPlayerSlotPos(id);
            foreach (Player player in playingPlayers)
            {
                if (player == null) continue;
                player.QueueCommand(Commands.OUT_MatchScoreUpdate, data);
            }
        }

        public void OnPlayerEndLoad()
        {
            Interlocked.Increment(ref LoadFinishCount);
            if (LoadFinishCount < PlayingCount)
                return;

            foreach (Player player in playingPlayers)
            {
                if (player == null) continue;
                player._matchSkipRequested = true;
                player.QueueCommand(Commands.OUT_MatchAllPlayersLoaded);
            }
        }

        public void SetMatchData(bMatchData matchData)
        {
            if (matchData.inProgress) return; //NOTE: The game sends an packet to bancho with playing room status when the match end, idk why

            bool toggledToFreeMod = matchData.specialModes != this.matchData.specialModes &&
                                    matchData.specialModes == MultiSpecialModes.FreeMod;
            Mods mods = matchData.activeMods;
            if (toggledToFreeMod)
            {
                if (mods.HasFlag(Mods.DoubleTime))
                {
                    mods &= ~Mods.DoubleTime;
                    matchData.activeMods = Mods.DoubleTime;
                }
                if (mods.HasFlag(Mods.Nightcore))
                {
                    mods &= ~Mods.Nightcore;
                    matchData.activeMods = Mods.Nightcore;
                }
                if (mods.HasFlag(Mods.HalfTime))
                {
                    mods &= ~Mods.HalfTime;
                    matchData.activeMods = Mods.HalfTime;
                }
            }

            bool toggledToTeamMode = matchData.IsTeamMode && !this.matchData.IsTeamMode;
            bool toggledToNormalMode = !matchData.IsTeamMode && this.matchData.IsTeamMode;

            for (int i = 0; i < bMatchData.MaxRoomPlayers - 1; i++)
            {
                if (matchData.slotId[i] == -1) continue;
                if (matchData.slotStatus[i] == SlotStatus.Ready &&
                    matchData.slotStatus[i] != SlotStatus.NoMap) matchData.slotStatus[i] = SlotStatus.NotReady;
                if (toggledToFreeMod) matchData.slotMods[i] = mods;
                if (toggledToTeamMode) matchData.slotTeam[i] = i%2 == 0 ? SlotTeams.Blue : SlotTeams.Red;
                if (toggledToNormalMode) matchData.slotTeam[i] = SlotTeams.Neutral;
            }

            if (matchData.gamePassword == "") matchData.gamePassword = null;
            else if (this.matchData.HasPassword) matchData.gamePassword = this.matchData.gamePassword;

            this.matchData = matchData;
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
            if (matchData.slotStatus[slotPos] != SlotStatus.NoMap && matchData.slotStatus[slotPos] != SlotStatus.Playing)
            {
                matchData.slotStatus[slotPos] = ready ? SlotStatus.Ready : SlotStatus.NotReady;
                this.SendMatchUpdate();
            }
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
                        matchData.activeMods = Mods.DoubleTime;
                        removePlayersReady = true;
                    }
                    else if (matchData.activeMods == Mods.DoubleTime)
                        matchData.activeMods = Mods.None;
                    if (mods.HasFlag(Mods.Nightcore))
                    {
                        mods &= ~Mods.Nightcore;
                        matchData.activeMods = Mods.Nightcore;
                        removePlayersReady = true;
                    }
                    else
                    {
                        if (matchData.activeMods == Mods.Nightcore)
                            matchData.activeMods = Mods.None;
                    }
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
