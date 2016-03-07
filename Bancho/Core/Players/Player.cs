using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using osuBancho.Core.Lobby;
using osuBancho.Core.Lobby.Matches;
using osuBancho.Core.Serializables;
using osuBancho.Database.Interfaces;
using osuBancho.Helpers;
using osuBancho.IRC.Objects;

namespace osuBancho.Core.Players
{
    [DebuggerDisplay("Id = {Id}, Username = {Username}")]
    class Player
    {
        internal readonly ConcurrentQueue<Command> CommandQueue = new ConcurrentQueue<Command>();
        public readonly string Token;
        public readonly int Id;
        public readonly string Username;
        public string IPAddress;
        public int LastPacketTime;

        public UserTags Tags;
        public PlayModes currentMode;
        public bUserStatus Status;
        public ModeData[] ModesDatas = new ModeData[4];

        public int TimeZone; //UTC
        public string Language;
        public int CountryId;
        public float Latitude;
        public float Longitude;

        public Player Spectating;
        private readonly ConcurrentDictionary<int, Player> _spectators = new ConcurrentDictionary<int, Player>();
        
        public IEnumerable<Player> Spectators => _spectators.Select(x => x.Value);

        public Match currentMatch;
        internal bool _matchSkipRequested;
        internal bool _matchLoadFinished;
        internal bool _matchPlayFinished;

        public Player(string token, DataRow dbRow)
        {
            Token = token;
            Id = (int)dbRow["id"];
            Username = (string)dbRow["username"];
            Tags = (UserTags)(int)dbRow["tags"];
            currentMode = (PlayModes)(sbyte)dbRow["last_played_mode"];
        }

        public bool IrcPlayer;
        public Player(DataRow dbRow)
        {
            Id = (int)dbRow["id"];
            Username = (string)dbRow["username"];
            Tags = (UserTags)(int)dbRow["tags"];
            IrcPlayer = true;
        }

        public void QueueCommand(Commands command, object serializable)
        {
            CommandQueue.Enqueue(new Command(command, serializable));
        }

        public void QueueCommand(Commands command)
        {
            CommandQueue.Enqueue(new Command(command, null));
        }

        public void QueueCommands(Command[] commands)
        {
            foreach (Command command in commands)
            {
                CommandQueue.Enqueue(command);
            }
        }

        public void SerializeCommands(Stream outStream)
        {
            if (CommandQueue.IsEmpty) return;
            var writer = new SerializationWriter(outStream);
            
            Command command;
            while (outStream.Length < 6144L && this.CommandQueue.TryDequeue(out command))
            {
                var begin = writer.BaseStream.Position;

                writer.Write(command.Id);
                writer.Write((byte)0);
                writer.Write(0);

                if (command.noHasData) continue;
                if (command.Serializable != null)
                {
                    command.Serializable.WriteToStream(writer);
                }
                else
                {
                    writer.method_0(command.RegularType); //TODO: Improve this
                }

                writer.BaseStream.Position = begin + 3; //this is weird >_>
                writer.Write((int)(writer.BaseStream.Length - begin) - 7);
                writer.Seek(0, SeekOrigin.End);
            }

            //Debug.WriteLine($"Sent: {Utils.ByteArrayRepr((writer.BaseStream as MemoryStream).ToArray())}");
            outStream.Position = 0;
        }

        public uint GetModeRank(int mode)
        {
            using (IQueryAdapter dbClient = Bancho.DatabaseManager.GetQueryReactor())
            {
                dbClient.SetQuery("SELECT FIND_IN_SET( pp_raw, (SELECT GROUP_CONCAT( pp_raw ORDER BY pp_raw DESC ) FROM users_modes_info WHERE mode_id = @mode )) AS rank FROM users_modes_info WHERE mode_id = @mode AND user_id = @id;");
                dbClient.AddParameter("mode", currentMode);
                dbClient.AddParameter("id", this.Id);
                return (uint)dbClient.getInteger();
            }
        }

        public void GetModesData()
        {
            using (IQueryAdapter dbClient = Bancho.DatabaseManager.GetQueryReactor())
            {

                dbClient.SetQuery("SELECT * " +
                                  "FROM users_modes_info " +
                                  "WHERE user_id = @id");

                dbClient.AddParameter("id", this.Id);
                foreach (DataRow row in dbClient.getTable().Rows)
                {
                    this.ModesDatas[(SByte) row["mode_id"]] = new ModeData(row)
                    {
                        RankPosition = GetModeRank((SByte) row["mode_id"])
                    };
                }
            }
        }

        public void GetModeData(int modeId)
        {
            using (IQueryAdapter dbClient = Bancho.DatabaseManager.GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * " +
                                  "FROM users_modes_info " +
                                  "WHERE user_id = @id AND mode_id = @modeid " +
                                  "LIMIT 1");

                dbClient.AddParameter("id", this.Id);
                dbClient.AddParameter("modeid", modeId);
                DataRow row = dbClient.getRow();
                if (row != null)
                    this.ModesDatas[modeId] = new ModeData(row)
                    {
                        RankPosition = GetModeRank(modeId)
                    };
            }
        }
        //TODO: Should do modedata loading and etc related in another class?

        public bUserStats SerializableStats => 
            new bUserStats(this.Id, this.Status, this.currentModeData.TotalScore,
            this.currentModeData.Accuracy,
            (int) this.currentModeData.PlayCount, this.currentModeData.RankedScore,
            (int) this.currentModeData.RankPosition, this.currentModeData.PerformancePoints);

        //NOTE: The way that i do the userstats seems very wrong, i should fix this...

        public ModeData currentModeData => this.ModesDatas[(int) this.currentMode];

        public bool IsMultiplaying => this.Status.status == bStatus.Multiplaying;

        public void AddSpectator(Player spectator)
        {
            if (!_spectators.TryAdd(spectator.Id, spectator)) return;

            foreach (Player cspectator in Spectators)
            {
                spectator.QueueCommand(Commands.OUT_FellowSpectatorJoined, cspectator.Id);
                cspectator.QueueCommand(Commands.OUT_FellowSpectatorJoined, spectator.Id);
            }
            QueueCommand(Commands.OUT_SpectatorJoined, spectator.Id);
        }

        public void RemoveSpectator(int spectatorId)
        {
            Player spectator;
            if (!_spectators.TryRemove(spectatorId, out spectator)) return;

            foreach (Player cspectator in Spectators)
            {
                cspectator.QueueCommand(Commands.OUT_FellowSpectatorLeft, spectatorId);
            }
            QueueCommand(Commands.OUT_SpectatorLeft, spectatorId);
        }

        public void RemoveSpectator(Player spectator)
        {
            Player _spectator;
            if (!_spectators.TryRemove(spectator.Id, out _spectator)) return;

            foreach (Player cspectator in Spectators)
            {
                cspectator.QueueCommand(Commands.OUT_FellowSpectatorLeft, spectator.Id);
            }
            QueueCommand(Commands.OUT_SpectatorLeft, spectator.Id);
        }
        
        public void SpectatorNoHasMap(int spectatorId)
        {
            Player _spectator;
            if (!_spectators.TryGetValue(spectatorId, out _spectator)) return;
            
            foreach (Player cspectator in Spectators)
            {
                cspectator.QueueCommand(Commands.OUT_SpectatorCantSpectate, spectatorId);
            }
            QueueCommand(Commands.OUT_SpectatorCantSpectate, spectatorId);
        }


        public void OnLoggedIn()
        {
            LastPacketTime = Environment.TickCount;

            var geoData = GeoUtil.GetDataFromIPAddress(this.IPAddress);
            if (geoData != null)
            {
                this.CountryId = GeoUtil.GetCountryId(geoData["country"]["names"]["en"].ToString());
                this.Language = CultureInfo //NOTE: This can be used to an translation ..
                    .GetCultures(CultureTypes.AllCultures)
                    .First(c => c.Name.EndsWith(geoData["country"]["iso_code"].ToString()))
                    .Name;
                this.Latitude = float.Parse(geoData["location"]["latitude"].ToString());
                this.Longitude = float.Parse(geoData["location"]["longitude"].ToString());
            }

            Status = new bUserStatus(bStatus.Idle, "", "", Mods.None, this.currentMode, 0);
            this.GetModesData();

            QueueCommands(new[]
            {
                new Command(Commands.OUT_ChoProtocol, Bancho.Protocol), //TODO: Is boxing good? idk..
                new Command(Commands.OUT_LoginResult, this.Id),
                new Command(Commands.OUT_UserTags, (int)this.Tags),
                new Command(Commands.OUT_FriendsList, new[] {2,3}), //TODO: Friendslist
                new Command(Commands.OUT_Announcement,
                    "http://puu.sh/jh7t7/20c04029ad.png|https://osu.ppy.sh/news/123912240253"),
                new Command(Commands.OUT_UpdateUserInfo,
                    new bUserInfo(this.Id, this.Username, this.TimeZone, (byte)this.CountryId, UserTags.Player, PlayModes.Osu, this.Longitude, this.Latitude, 1)),
                new Command(Commands.OUT_UpdateUserState, this.SerializableStats),
                new Command(Commands.OUT_UpdateUserInfo,
                    new bUserInfo(-3, "BanchoBot", 0, 0, UserTags.None, PlayModes.Osu, 0, 0, 0))
            });

            QueueCommand(Commands.OUT_ChannelJoinSuccess, "#osu");
            QueueCommand(Commands.OUT_ChannelJoinSuccess, "#broadcast");

            //FBUG: cant click in BanchoBot on his messages 
            //FIXED? Probally negative id is only sent in UpdateUserInfo ..
            QueueCommand(Commands.OUT_IrcMessage,
                new bIRCMessage("BanchoBot", "#osu", "Welcome to the Bancho!") {SenderId = 3}); //NOTE: This is a test message
        }

        public void OnDisconnected()
        {
            this.Spectating?.RemoveSpectator(this.Id);
            this.currentMatch?.RemovePlayer(this.Id);
            LobbyManager.ExitLobby(this.Id);
        }

        public void OnPacketReceived(Stream receivedStream)
        {
            this.LastPacketTime = Environment.TickCount;

            while (!receivedStream.IsInEnd())
            {
                Commands command = (Commands) receivedStream.ReadUInt16();
                receivedStream.Position += 1; //skip 1 byte

                uint packetLength = receivedStream.ReadUInt32();
                if (packetLength > receivedStream.Length - receivedStream.Position)
                {
                    Debug.WriteLine("Invalid packet!! x.x");
                    PlayerManager.DisconnectPlayer(this.Id, DisconnectReason.Kick);
                    return;
                }

                var reader = new SerializationReader(new MemoryStream(receivedStream.Read((int)packetLength)));

                Debug.WriteLine("[{2}] Command received: {0} [{1}]", command.ToString().Contains("_")?command.ToString().Split('_')[1]:command.ToString(),
                            Utils.ByteArrayRepr((reader.BaseStream as MemoryStream).ToArray()), this.Username);
                        
                switch (command)
                {
                    case Commands.IN_LocalUserState:
                        this.Status = new bUserStatus(reader);
                        this.currentMode = Status.playMode;
                        this.GetModeData((int)this.currentMode);

                        QueueCommand(Commands.OUT_UpdateUserState, this.SerializableStats);
                        break;
                    case Commands.IN_IrcMessage:
                        bIRCMessage message = new bIRCMessage(reader);

                        PlayerManager.QueueCommandForAll(Commands.OUT_IrcMessage, new bIRCMessage(this.Username, message.Target, message.Message) { SenderId = this.Id }, 
                                                         exclude: this.Id);
                        
                        //TODO: Better command parse
                        switch (message.Message)
                        {
                            case "!sendbanchorestart":
                                const int delay = 20000;
                                PlayerManager.QueueCommandForAll(Commands.const_86, delay);
                                break;
                            case "!closeosu":
                                this.QueueCommand(Commands.OUT_Ping, 0); //lol, i can use this for ban
                                break;
                            case "!togglelock":
                                this.currentMatch?.SetLocked(!this.currentMatch.Locked);
                                break;
                            case "!abort":
                                this.currentMatch?.FinishMatch(true);
                                break;
                            case "!start":
                                this.currentMatch?.StartMatch();
                                break;
                            case "!givemehost":
                                this.currentMatch?.SetHost(this);
                                break;
                            case "!targetmod":
                                this.currentMatch?.SetMods(Mods.Target);
                                break;
                            case "!automod":
                                this.currentMatch?.SetMods(Mods.Autoplay); //does nothing in gameplay >_>
                                break;
                        }
                        break;
                    case Commands.IN_Logout:
                        PlayerManager.DisconnectPlayer(this, DisconnectReason.Logout);
                        break;
                    case Commands.IN_UNK03:
                        //getlocaluserdata?
                        //getallplayerstoload?
                        //what the hell is this?

                        QueueCommand(Commands.OUT_UserForLoadBundle, PlayerManager.PlayersIds.ToArray()); //TODO: Improve?
                        break;
                    case Commands.IN_HeartBit:
                        break; //Do anything with this?
                    case Commands.IN_SpectatePlayer:
                    {
                        Player player = PlayerManager.GetPlayerById(reader.ReadInt32());
                        if (player == null) break;

                        if (this.Spectating != null)
                        {
                            if (this.Spectating.Id == player.Id) break;
                            this.Spectating.RemoveSpectator(this.Id);
                            this.Spectating = null;
                        }
                        player.AddSpectator(this);
                        Spectating = player;
                        //TODO: Spectator channel
                        //TODO: Spectator see others spectators
                        break;
                    }
                    case Commands.IN_StopSpectate:
                        this.Spectating?.RemoveSpectator(this.Id);
                        this.Spectating = null;
                        break;
                    case Commands.IN_SpectateFrames:
                        var replay = new bReplayBuffer(reader);
                        
                        foreach (Player spectator in this.Spectators)
                        {
                            if (replay.enum0_0 == Enum0.Start && spectator.Status.beatmapHash != this.Status.beatmapHash)
                            {
                                spectator.QueueCommand(Commands.OUT_UpdateUserState, this.SerializableStats);
                            }
                            spectator.QueueCommand(Commands.OUT_SpectateFrames, replay);
                        }
                        break;
                    case Commands.IN_CantSpectate: //No has map
                        this.Spectating?.SpectatorNoHasMap(this.Id);
                        break;
                    case Commands.IN_IrcMessagePrivate:
                        //TODO: IrcMessagePrivate
                        break;
                    case Commands.IN_LobbyPart:
                        LobbyManager.ExitLobby(this.Id);
                        break;
                    case Commands.IN_LobbyJoin:
                        LobbyManager.EnterLobby(this);
                        break;
                    case Commands.IN_MatchCreate:
                        LobbyManager.CreateMatch(this, new bMatchData(reader));
                        break;
                    case Commands.IN_MatchJoin:
                        var intstr = new bIntStr(reader);
                        if (!LobbyManager.TryEnterMatch(this, intstr.@int, intstr.str))
                            QueueCommand(Commands.OUT_MatchJoinFail);
                        break;
                    case Commands.IN_MatchLeave: 
                        currentMatch?.RemovePlayer(this.Id);
                        break;
                    case Commands.IN_MatchChangeSlot:
                        if (this.currentMatch?.Locked == false)
                            currentMatch.MovePlayerSlot(this.Id, reader.ReadInt32());
                        break;
                    case Commands.IN_MatchReady:
                        if (this.currentMatch?.Locked == false)
                            currentMatch.SetReady(true, this.Id);
                        break;
                    case Commands.IN_MatchNotReady:
                        if (this.currentMatch?.Locked == false)
                            currentMatch.SetReady(false, this.Id);
                        break;
                    case Commands.IN_MatchLockSlot:
                        if (this.currentMatch?.IsHost(this.Id) == true)
                            currentMatch.LockSlot(reader.ReadInt32());
                        break;
                    case Commands.IN_MatchChangeSettings:
                    case Commands.IN_MatchChangePassword:
                        if (this.currentMatch?.IsHost(this.Id) == true)
                            currentMatch.SetMatchData(new bMatchData(reader));
                        break;
                    case Commands.IN_MatchStart:
                        if (this.currentMatch?.IsHost(this.Id) == true)
                            currentMatch.StartMatch();
                        break;
                    case Commands.IN_MatchScoreUpdate:
                        if (this.currentMatch != null && this.IsMultiplaying)
                            try
                            {
                                currentMatch.OnPlayerScoreUpdate(this.Id, new bScoreData(reader));
                            } catch {} //NOTE: Sometimes it throw an exception, lol
                        break;
                    case Commands.IN_ChannelJoin:
                        QueueCommand(Commands.OUT_ChannelRevoked, reader.ReadString());
                        break;
                    case Commands.IN_MatchComplete:
                        if (this.currentMatch != null && this.IsMultiplaying)
                        {
                            this._matchPlayFinished = true;
                            this.currentMatch.OnPlayerEndMatch();
                        }
                        break;
                    case Commands.IN_MatchChangeMods:
                        if (this.currentMatch?.Locked == false)
                            this.currentMatch.SetMods(this.Id, (Mods)reader.ReadInt32());
                        break;
                    case Commands.IN_MatchLoadComplete:
                        if (this.currentMatch != null && this.IsMultiplaying)
                        {
                            this._matchLoadFinished = true;
                            this.currentMatch.OnPlayerEndLoad();
                        }
                        break;
                    case Commands.IN_MatchNoBeatmap:
                        currentMatch?.SetHasMap(false, this.Id);
                        break;
                    case Commands.IN_MatchFailed:
                        if (this.currentMatch != null && this.IsMultiplaying)
                            currentMatch.OnPlayerFail(this.Id);
                        break;
                    case Commands.IN_MatchHasBeatmap:
                        currentMatch?.SetHasMap(true, this.Id);
                        break;
                    case Commands.IN_MatchSkipRequest:
                        if (this.currentMatch != null && this.IsMultiplaying)
                        {
                            this._matchSkipRequested = true;
                            this.currentMatch.OnPlayerSkip(this.Id);
                        }
                        break;
                    case Commands.IN_MatchTransferHost:
                        if (this.currentMatch?.IsHost(this.Id) == true)
                            currentMatch.SetHost(reader.ReadInt32());
                        break;
                    case Commands.IN_GetUsersStats:
                    {
                        //To get status list?
                        int[] playerList = reader.ReadInts();
                        foreach (var playerId in playerList)
                        {
                            if (playerId == this.Id) continue;
                            Player player = PlayerManager.GetPlayerById(playerId);

                            if (player != null)
                                QueueCommand(Commands.OUT_UpdateUserState,
                                    player.SerializableStats);
                        }
                        break;
                    }
                    case Commands.IN_GetUsersInfo:
                    {
                        //To Load Player List?
                        int[] playerList = reader.ReadInts();
                        foreach (var playerId in playerList)
                        {
                            Player player = PlayerManager.GetPlayerById(playerId);
                            if (player != null)
                                QueueCommand(Commands.OUT_UpdateUserInfo,
                                    new bUserInfo(player.Id, player.Username, player.TimeZone, (byte) player.CountryId,
                                        player.Tags, player.currentMode, player.Longitude, player.Latitude, 1));
                            else
                                QueueCommand(Commands.OUT_UserQuit, playerId);
                        }
                        break;
                    }
                    case Commands.const_79:
                        //NOTE: idk what is
                        break;
                    case Commands.IN_AwayMessage:
                        //TODO: AwayMessage
                        break;
                    case Commands.IN_FriendAdd:
                        //TODO: FriendAdd
                        break;
                    case Commands.IN_FriendRemove:
                        //TODO: FriendRemove
                        break;
                    //TODO: UserToggleBlockNonFriendPM
                    //TODO: BanchoSwitchTourneyServer
                    case Commands.IN_InvitePlayer:
                    {
                        Player player = PlayerManager.GetPlayerById(reader.ReadInt32()); 
                        //TODO: Send using IRC
                        player?.QueueCommand(Commands.OUT_IrcMessagePrivate, new bIRCMessage(this.Username, "",
                            $"Come join my multiplayer match: [osump://{this.currentMatch.MatchData.matchId}/ {this.currentMatch.MatchData.gameName}]")
                            { SenderId = this.Id });
                        break;
                    }
                    case Commands.const_98:
                        //Content: GameBase.GameTime
                        //NOTE: This is an packet that is received apparently when more than 256 users are sended by UserForLoad
                        break;
                    default:
                        Debug.WriteLine("Undefined command: {0} [{1}]", command.ToString().Split('_')[1],
                            Utils.ByteArrayRepr((reader.BaseStream as MemoryStream).ToArray()));
                        break;
                }
            }
        }
    }
}
