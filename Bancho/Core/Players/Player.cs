using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public UserTags Tags;
        public PlayModes currentMode;
        public bUserStatus Status;
        public ModeData[] ModesDatas = new ModeData[4];
        public int TimeZone; //UTC
        public int CountryId;

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
            CountryId = 0/*BR:31*/; //TODO: How get country id? a: maybe make an array - raple
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
            SerializationWriter writer = new SerializationWriter(outStream);
            Command command;
            long begin;
            while (outStream.Length < 6144L && this.CommandQueue.TryDequeue(out command))
            {
                begin = writer.BaseStream.Position;
                writer.Write((short)command.Id);
                writer.Write((byte)0);
                writer.Write((int)0);
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

        public bUserStats SerializableStats => new bUserStats(this.Id, this.Status, this.currentModeData.TotalScore,
            this.currentModeData.Accuracy,
            (int) this.currentModeData.PlayCount, this.currentModeData.RankedScore,
            (int) this.currentModeData.RankPosition, this.currentModeData.PerformancePoints);

        //NOTE: The way that i do the userstats seems very wrong, i should fix this...

        public ModeData currentModeData => this.ModesDatas[(int) this.currentMode];

        public bool IsMultiplaying => this.Status.status == bStatus.Multiplaying;

        public void AddSpectator(Player spectator)
        {
            if (_spectators.TryAdd(spectator.Id, spectator))
                QueueCommand(Commands.OUT_SpectatorJoined, spectator.Id);
        }

        public void RemoveSpectator(int spectatorId)
        {
            Player spectator;
            if (_spectators.TryRemove(spectatorId, out spectator))
                QueueCommand(Commands.OUT_SpectatorLeft, spectatorId);
        }

        public void RemoveSpectator(Player spectator)
        {
            Player _spectator;
            if (_spectators.TryRemove(spectator.Id, out _spectator))
                QueueCommand(Commands.OUT_SpectatorLeft, spectator.Id);
        }

        public void OnLoggedIn()
        {
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
                new Command(Commands.UpdateUserInfo,
                    new bUserInfo(this.Id, this.Username, this.TimeZone, (byte)this.CountryId, UserTags.Player, PlayModes.Osu, 1f, 1f, 1)),
                new Command(Commands.OUT_UpdateUserState, this.SerializableStats),
                new Command(Commands.UpdateUserInfo,
                    new bUserInfo(-3, "BanchoBot", 0, 0, UserTags.None, PlayModes.Osu, 0, 0, 0))
            });

            QueueCommand(Commands.OUT_ChannelJoinSuccess, "#osu");

            //BUG: cant click in BanchoBot on his messages
            QueueCommand(Commands.OUT_IrcMessage,
                new bIRCMessage("BanchoBot", "#osu", "Welcome to the osu!p :)") {int_0 = -3});
            QueueCommand(Commands.OUT_IrcMessage,
                new bIRCMessage("BanchoBot", "#osu", "Click [http://google.com.br/ here] to see news") {int_0 = -3});
            QueueCommand(Commands.OUT_IrcMessage,
                new bIRCMessage("BanchoBot", "#osu", "Click [http://google.com.br/ here] to see changelog") { int_0 = -3 });
        }

        public void OnDisconnected()
        {
            this.Spectating?.RemoveSpectator(this.Id);
            this.currentMatch?.RemovePlayer(this.Id);
            LobbyManager.ExitLobby(this.Id);
        }

        public void OnPacketReceived(Stream receivedStream)
        {
            while (!receivedStream.IsInEnd())
            {
                Commands command = (Commands) receivedStream.ReadUInt16();
                
                receivedStream.Position += 1;
                uint cmdLen = receivedStream.ReadUInt32();
                if (cmdLen > receivedStream.Length - receivedStream.Position)
                {
                    Debug.WriteLine("Invalid packet!! x.x");
                    PlayerManager.DisconnectPlayer(this.Id, DisconnectReason.Kick);
                    return;
                }

                SerializationReader reader = new SerializationReader(new MemoryStream(receivedStream.Read((int)cmdLen)));

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

                        PlayerManager.QueueCommandForAll(Commands.OUT_IrcMessage, new bIRCMessage(this.Username, message.Target, message.Message) { int_0 = this.Id }, 
                                                         exclude:this.Id);

                        // QueueCommand(Commands.OUT_IrcMessage,
                        //     new bIRCMessage("BanchoBot", message.Target, "RECEIVED") {int_0 = -3});


                        //TODO: Better command parse
                        if (message.Message == "!sendbanchorestart")
                        {
                            const int delay = 20000;
                            PlayerManager.QueueCommandForAll(Commands.const_86, delay);
                        }
                        if (message.Message == "!closeosu")
                        {
                            this.QueueCommand(Commands.OUT_Ping, 0); //lol, i can use this for ban
                        }
                        if (message.Message == "!togglelock")
                        {
                            this.currentMatch.SetLocked(!this.currentMatch.Locked);
                        }
                        if (message.Message == "!abort")
                        {
                            this.currentMatch.FinishMatch(true);
                        }
                        if (message.Message == "!start")
                        {
                            this.currentMatch.StartMatch();
                        }
                        if (message.Message == "!givemehost")
                        {
                            this.currentMatch.SetHost(this);
                        }
                        if (message.Message == "!targetmod")
                        {
                            this.currentMatch.SetMods(Mods.Target);
                        }
                        if (message.Message == "!automod")
                        {
                            this.currentMatch.SetMods(Mods.Autoplay); //does nothing >_>
                        }
                        break;
                    case Commands.IN_Logout:
                        PlayerManager.DisconnectPlayer(this, DisconnectReason.Logout);
                        break;
                    case Commands.IN_UNK03:
                        //getlocaluserdata?
                        //getallplayerstoload?
                        QueueCommand(Commands.LUserForLoad, PlayerManager.PlayersIds.ToArray()); //TODO: Improve?
                        break;
                    case Commands.IN_HeartBit:
                        break; //Do anything with this?
                    case Commands.IN_SpectatePlayer: //TODO Spectator channel
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
                        break;
                    }
                    case Commands.IN_StopSpectate:
                        if (this.Spectating == null) break;
                        this.Spectating.RemoveSpectator(this.Id);
                        this.Spectating = null;
                        break;
                    case Commands.IN_SpectateFrames:
                        var replay = new bReplayBuffer(reader);
                        Debug.WriteLine(replay.enum0_0);
                        
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
                        this.Spectating?.QueueCommand(Commands.OUT_SpectatorCantSpectate, this.Id);
                        break;
                    case Commands.IN_IrcMessagePrivate:
                        break; //TODO IrcMessagePrivate
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
                    case Commands.IN_MatchLeave: //Is this right?
                        if (this.currentMatch!=null)
                            currentMatch.RemovePlayer(this.Id);
                        break;
                    case Commands.IN_MatchChangeSlot:
                        if (this.currentMatch != null && !this.currentMatch.Locked)
                            currentMatch.MovePlayerSlot(this.Id, reader.ReadInt32());
                        break;
                    case Commands.IN_MatchReady:
                        if (this.currentMatch != null && !this.currentMatch.Locked)
                            currentMatch.SetReady(true, this.Id);
                        break;
                    case Commands.IN_MatchNotReady: //55, not in order but is better here
                        if (this.currentMatch != null && !this.currentMatch.Locked)
                            currentMatch.SetReady(false, this.Id);
                        break;
                    case Commands.IN_MatchLockSlot:
                        if (this.currentMatch != null && this.currentMatch.IsHost(this.Id))
                            currentMatch.LockSlot(reader.ReadInt32());
                        break;
                    case Commands.IN_MatchChangeSettings:
                    case Commands.IN_MatchChangePassword:
                        if (this.currentMatch != null && this.currentMatch.IsHost(this.Id))
                            currentMatch.SetMatchData(new bMatchData(reader));
                        break;
                    case Commands.IN_MatchStart:
                        if (this.currentMatch != null && this.currentMatch.IsHost(this.Id))
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
                        if (this.currentMatch != null && !this.currentMatch.Locked)
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
                        if (this.currentMatch != null)
                            currentMatch.SetHasMap(false, this.Id);
                        break;
                    case Commands.IN_MatchFailed:
                        if (this.currentMatch != null && this.IsMultiplaying)
                            currentMatch.OnPlayerFail(this.Id);
                        break;
                    case Commands.IN_MatchHasBeatmap:
                        if (this.currentMatch != null)
                            currentMatch.SetHasMap(true, this.Id);
                        break;
                    case Commands.IN_MatchSkipRequest:
                        if (this.currentMatch != null && this.IsMultiplaying)
                        {
                            this._matchSkipRequested = true;
                            this.currentMatch.OnPlayerSkip(this.Id);
                        }
                        break;
                    case Commands.IN_MatchTransferHost:
                        if (this.currentMatch != null && this.currentMatch.IsHost(this.Id))
                            currentMatch.SetHost(reader.ReadInt32());
                        break;
                    case Commands.GetUsersStats:
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
                            //else
                            //    QueueCommand(Commands.UserExits, playerId); //BUG: This disconnect BanchoBot
                        }
                        break;
                    }
                    case Commands.GetUsersInfo:
                    {
                        //To Load Player List?
                        int[] playerList = reader.ReadInts();
                        foreach (var playerId in playerList)
                        {
                            Player player = PlayerManager.GetPlayerById(playerId);
                            if (player != null)
                                QueueCommand(Commands.UpdateUserInfo,
                                    new bUserInfo(player.Id, player.Username, player.TimeZone, (byte) player.CountryId,
                                        player.Tags, player.currentMode, 0, 0, 1));
                            else
                                QueueCommand(Commands.OUT_UserQuit, playerId);
                        }
                        break;
                    }
                    //case Commands.const_79: //received when first open the bancho idk what is
                    //    break;
                    case Commands.IN_InvitePlayer:
                    {
                        Player player = PlayerManager.GetPlayerById(reader.ReadInt32());
                        if (player == null) break;
                        bIRCMessage iC = new bIRCMessage(this.Username, "",
                            "Come join my multiplayer match: [osump://" + this.currentMatch.MatchData.matchId + "/ " +
                            this.currentMatch.MatchData.gameName + "]") {int_0 = this.Id};
                        player.QueueCommand(Commands.OUT_IrcMessagePrivate, iC);
                        break;
                    }
                    case Commands.const_98:
                        //Contant: GameBase.GameTime
                        //An packet that is received apparently when more than 256 users are sended by UserForLoad
                        QueueCommand(Commands.OUT_IrcMessage,
                            new bIRCMessage("BanchoBot", "#osu", "PACKET 98 RECEIVED = " + reader.ReadInt32().ToString()) { int_0 = -3 });
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
