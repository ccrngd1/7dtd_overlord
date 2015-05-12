using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using lawsoncs.htg.sdtd.AdminServer.data;
using lawsoncs.htg.sdtd.AdminServer.objects;

namespace lawsoncs.htg.sdtd.AdminServer
{
    internal class WorkerClass
    {
        private readonly CancellationTokenSource _tokenSource;
        private CancellationToken _token;
        private Task _workerTask;

        private readonly ConcurrentQueue<string> _incomingMessageConcurrentQueue;

        private Task _telnetTask; 

        private readonly StateObject _state;

        public WorkerClass()
        { 
            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;
            _incomingMessageConcurrentQueue = new ConcurrentQueue<string>(); 
            _state = new StateObject();
        }

        public void Start()
        {

            _workerTask = Task.Factory.StartNew(Execute, _tokenSource.Token);
        }

        public void Stop()
        {
            _tokenSource.Cancel();

            try
            {
                if (_workerTask != null)
                    _workerTask.Wait();
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    log4net.LogManager.GetLogger("log").Fatal("Worker AggragateExecption", e);
                }
            }
            catch (Exception e)
            {
                log4net.LogManager.GetLogger("log").Fatal("Worker execption", e);
            }
        }

        private void Execute()
        {
            _telnetTask = Task.Factory.StartNew(StartTelnetClient, _tokenSource.Token); 

            Player tempP; //keep this copy so we don't re-create it 

            int? lastGUID = null;

            var serverStatusMessageRegex = new Regex(MessageText.serverStatusMessage); //datetime regex with the '-24xxxx.xxx ' suffix

            while (!_token.IsCancellationRequested)
            {
                if (_incomingMessageConcurrentQueue.Count == 0) // if there are no messages to process, wait for 1 sec then try to hit it again
                {
                    Task.Delay(1000).Wait();
                }
                
                string msg="";
                try
                {
                    if (!_incomingMessageConcurrentQueue.TryDequeue(out msg)) continue;

                    #region general structure of msg

                    #region structure of unsolicited messages

                    //2014-12-31T14:25:13 -248963.962 INF Telnet executed "lp" from: 74.134.120.129:56 \\serverStatusTelentMessageRegex
                    //2014-12-31T14:25:18 -248959.214 INF Time: 5390.08m FPS: 49.53 Heap: 990.5MB Max: 1364.4MB Chunks: 1892 CGO: 149 Ply: 2 Zom: 62 Ent: 73 (571) Items: 146 //serverStatusGenMessageRegex
                    //2014-12-31T14:25:18 -248959.214 INF STATS: 5390.08,49.53,990.5,1364.4,1892,149,2,62,73,571,146 //serverStatusStatsMessageRegex
                    //2014-12-31T14:25:24 -248952.571 WRN Entity [type=EntityZombie, name=spiderzombie, id=191674] fell off the world, id=191674 pos=(-121.4, -0.4, 1171.1) //esrverStatusFellMessageRegex 
                    //2015-01-30T20:54:21 -289897.313 INF Player set to offline: 76561198021432680 //STEAMGUID //serverStatusPlayerSetOffMessageRegex 
                    //2015-01-30T20:54:21 -289897.311 INF Removing player with id clientId=77, entityId=22436  //serverStatusRemovePlayerMessageRegex 
                    //2015-01-30T20:54:21 -289897.311 INF Player disconnected: ClientID=77, EntityID=22436, PlayerID='76561198021432680', PlayerName='Mindreaper' //serverStatusPlayerDisConnectMessageRegex 
                    //2015-01-30T20:54:21 -289897.311 INF [EAC] FreeUser (Mindreaper) //serverStatusFreeUserMessageRegex
                    //2015-01-30T20:54:21 -289897.310 INF GMSG: Mindreaper left the game //covered by GMSG 
                    //2015-01-30T20:54:21 -289897.310 INF Removing observed entity 87 //serverStatusRemoveEntityMessageRegex 
                    //2015-01-30T20:54:21 -289897.298 INF OnPlayerDisconnected 77 //serverStatusOnPlayerDisConnectMessageRegex 
                    //2015-01-30T20:54:21 -289897.068 INF [EAC] UserStatusHandler callback. Status: Disconnected GUID: 76561198021432680 ReqKick: True //serverStatusUserStatusHandlerMessageRegex 
                    //2015-01-30T21:03:09 -289369.722 INF OnPlayerConnected 82 //serverStatusOnPlayerConnectMessageRegex 
                    //2015-01-30T21:03:09 -289369.634 INF PlayerLogin: 477381601395943245/AllocatedID: 0/david_contreras2002/Alpha 10.4 //serverStatusPlayerLogin 
                    //2015-01-30T21:03:09 -289369.634 INF Token length: 1368 //serverStatusTokenMessageRegex 
                    //2015-01-30T21:03:09 -289369.632 INF [Steamworks.NET] Auth.AuthenticateUser() //serverStatusAuthenticateUser 
                    //2015-01-30T21:03:09 -289369.632 INF [Steamworks.NET] Authenticating player: david_contreras2002 SteamId: 76561198009504958 TicketLen: 1024 Result: k_EBeginAuthSessionResultOK //serverStatusAuthenticatingPlayerMessageRegex 
                    //2015-01-30T21:03:09 -289369.632 INF Allowing player with id 82 //serverStatusAllowingPlayerMessageRegex 
                    //2015-01-30T21:03:09 -289369.263 INF [Steamworks.NET] Authentication callback. ID: 76561198009504958, owner: 76561198009504958, result: k_EAuthSessionResponseOK //serverStatusAuthenticationCallbackMessageRegex 
                    //2015-01-30T21:03:09 -289369.263 INF [EAC] Registering user: id=76561198009504958, owner=76561198009504958 //serverStatusRegisteringUserMessageRegex 
                    //2015-01-30T21:03:11 -289367.065 INF [EAC] UserStatusHandler callback. Status: Authenticated GUID: 76561198009504958 ReqKick: False //serverStatusUserStatusHandlerCallbackMessageRegex
                    //2015-01-30T21:03:22 -289356.988 INF [Steamworks.NET] Authentication callback. ID: 76561198009504958, owner: 76561198009504958, result: k_EAuthSessionResponseAuthTicketCanceled //serverStatusAuthenticationCallbackMessageRegex
                    //2015-01-30T21:03:22 -289356.987 INF [Steamworks.NET] Kick player for invalid login: 76561198009504958 david_contreras2002 //serverStatusInvalidLoginMessageRegex

                    //2015-01-30T21:03:22 -289356.489 INF Disconnected player not found in client list...

                    //2015-01-30T21:03:22 -289356.486 INF Removing player with id clientId=82, entityId=-1 //david_c got DC'ed by steam, entity ID was never assigned because he never corporialized //serverStatusRemoveInvalidPlayerMessageRegex
                    //2015-01-30T21:03:45 -289333.384 INF [EAC] Registering user: id=76561198009504958, owner=76561198009504958 //serverStatusRegisteringUserMessageRegex
                    //2015-01-30T21:03:47 -289331.184 INF [EAC] UserStatusHandler callback. Status: Authenticated GUID: 76561198009504958 ReqKick: False //serverStatusUserStatusHandlerCallbackMessageRegex
                    //2015-01-30T21:04:03 -289315.133 INF RequestToEnterGame: 83/david_contreras2002      //clientId/playerName     //serverStatusRequestToEnterMessageRegex
                    //2015-01-30T21:04:03 -289315.132 INF GMSG: david_contreras2002 joined the game //serverStatusSayMessageRegex 
                    //2015-01-30T21:04:05 -289313.387 INF RequestToSpawnPlayer: 1568, 83, david_contreras2002, 11  //id, clientID, playername, unk    //serverStatusRequestSpawnMessageRegex
                    //2015-01-30T21:04:05 -289313.343 INF Created player with id=1568  //serverStatusCreatedPlayerMessageRegex
                    //2015-01-30T21:04:05 -289313.342 INF Adding observed entity: 95, (-705.4, 105.0, -620.3), 11  //serverStatusAddingEntityMessageRegex
                    //2015-01-30T21:04:05 -289313.342 INF Player connected, clientid=83, entityid=1568, name=david_contreras2002, steamid=76561198009504958, ip=24.243.182.208   /serverStatusPlayerConnectedMessageRegex
                    //2015-01-30T21:04:05 -289313.341 INF Player set to online: 76561198009504958     //serverStatusPlayerOnlineMessageRegex
                    //2015-01-30T21:04:05 -289313.341 INF GMSG: Kale: chat message //serverStatusSayMessageRegex

                    //2015-02-09T16:08:34 22148.915 INF Player with ID 22436 has stack for "goldenRodTea" greater than allowed (125 > 15) //serverStatusPlayerGreaterStackRegex
                    //2015-02-09T16:08:50 22165.215 INF AIDirector: Spawning scouts @ ((-1172.0, 64.0, 7773.0)) heading towards ((-1067.0, 53.0, 7807.0)) //serverStatusDirectorSpawningRegex
                    //2015-02-09T16:08:50 22165.216 INF Spawning this wave: 4 //serverStatusSpawningWaveRegex
                    //2015-02-09T16:08:50 22165.221 INF Spawned [type=EntityZombie, name=spiderzombie, id=301141] at (-1170.5, 64.2, 7776.5) Day=22 TotalInWave=1 CurrentWave=1 //serverStatusDirectorSpawnedRegex
                    //2015-02-09T16:08:50 22165.222 INF AIDirector: scout horde zombie '[type=EntityZombie, name=spiderzombie, id=301141]' was spawned and is moving towards point of interest. //serverStatusDirectorSpawnGeoRegex



                    #endregion

                    var serverStatusDirectorSpawnGeoRegex = new Regex(MessageText.serverStatusDirectorSpawnGeo);
                    var serverStatusDirectorSpawnedRegex = new Regex(MessageText.serverStatusDirectorSpawned);
                    var serverStatusDirectorSpawningWaveRegex = new Regex(MessageText.serverStatusDirectorSpawningWave);
                    var serverStatusDirectorSpawningRegex = new Regex(MessageText.serverStatusDirectorSpawning);
                    var serverStatusPlayerGreaterStackRegex = new Regex(MessageText.serverStatusPlayerGreaterStack);
                    var serverStatusPlayerOnlineMessageRegex = new Regex(MessageText.serverStatusPlayerOnlineMessage);
                    var serverStatusPlayerConnectedMessageRegex = new Regex(MessageText.serverStatusPlayerConnectedMessage);
                    var serverStatusAddingEntityMessageRegex = new Regex(MessageText.serverStatusAddingEntityMessage);
                    var serverStatusCreatedPlayerMessageRegex = new Regex(MessageText.serverStatusCreatedPlayerMessage);
                    var serverStatusRequestSpawnMessageRegex = new Regex(MessageText.serverStatusRequestSpawnMessage);
                    var serverStatusSayMessageRegex = new Regex(MessageText.serverStatusSayMessage);
                    var serverStatusRequestToEnterMessageRegex = new Regex(MessageText.serverStatusRequestToEnterMessage);
                    var serverStatusRemoveInvalidPlayerMessageRegex = new Regex(MessageText.serverStatusRemoveInvalidPlayerMessage);
                    var serverStatusInvalidLoginMessageRegex = new Regex(MessageText.serverStatusInvalidLoginMessage);
                    var serverStatusUserStatusHandlerCallbackMessageRegex = new Regex(MessageText.serverStatusUserStatusHandlerCallbackMessage);
                    var serverStatusRegisteringUserMessageRegex = new Regex(MessageText.serverStatusRegisteringUserMessage);
                    var serverStatusAuthenticationCallbackMessageRegex = new Regex(MessageText.serverStatusAuthenticationCallbackMessage);
                    var serverStatusAllowingPlayerMessageRegex = new Regex(MessageText.serverStatusAllowingPlayerMessage);
                    var serverStatusAuthenticatingPlayerMessageRegex = new Regex(MessageText.serverStatusAuthenticatingPlayerMessage);
                    var serverStatusAuthenticateUserRegex = new Regex(MessageText.serverStatusAuthenticateUser);
                    var serverStatusTokenMessageRegex = new Regex(MessageText.serverStatusTokenMessage);
                    var serverStatusPlayerLoginMessageRegex = new Regex(MessageText.serverStatusPlayerLoginMessage);
                    var serverStatusOnPlayerConnectMessageRegex = new Regex(MessageText.serverStatusOnPlayerConnectMessage);
                    var serverStatusUserStatusHandlerMessageRegex = new Regex(MessageText.serverStatusUserStatusHandlerMessage);
                    var serverStatusOnPlayerDisConnectMessageRegex = new Regex(MessageText.serverStatusOnPlayerDisConnectMessage);
                    var serverStatusRemoveEntityMessageRegex = new Regex(MessageText.serverStatusRemoveEntityMessage);
                    var serverStatusInGameMessageRegex = new Regex(MessageText.serverStatusInGameMessage);
                    var serverStatusFreeUserMessageRegex = new Regex(MessageText.serverStatusFreeUserMessage);
                    var serverStatusPlayerDisConnectMessageRegex = new Regex(MessageText.serverStatusPlayerDisConnectMessage);
                    var serverStatusRemovePlayerMessageRegex = new Regex(MessageText.serverStatusRemovePlayerMessage);
                    var serverStatusPlayerSetOffMessageRegex = new Regex(MessageText.serverStatusPlayerSetOffMessage);
                    var serverStatusTelentMessageRegex = new Regex(MessageText.serverStatusTelentMessage);
                    var serverStatusGenMessageRegex = new Regex(MessageText.serverStatusGenMessage);
                    var serverStatusStatsMessageRegex = new Regex(MessageText.serverStatusStatsMessage);
                    var esrverStatusFellMessageRegex = new Regex(MessageText.esrverStatusFellMessage);
                    #region structure of solicited message responses
                    //an executed 'lp' cmd from this telnet terminal will yield
                    //1. id=212, =HTG=JesseBilyk, pos=(-53.4, 38.0, 1208.0), rot=(9.8, -28.1, 0.0), remote=True, health=30, deaths=73, zombies=649, players=11, score=284, steamid=76561198059471064, ip=50.71.136.166, ping=0 //listPlayerResponseRegex
                    //2. id=167598, whitetopdawg, pos=(-1991.1, 63.9, 686.9), rot=(-12.7, 137.8, 0.0), remote=True, health=30, deaths=19, zombies=76, players=0, score=0, steamid=76561198040017510, ip=173.216.8.170, ping=0 //listPlayerResponseRegex
                    //Total of 2 in the game

                    //Day 56, 4:15 //getTimeResponseRegex

                    ////These may have the std yyyy-mm-ddThh:mm:ss -xxxxxxx.xxx LVL Message format as they are above in the 'general structure'
                    //Player connected, clientid=1, entityid=171, name=ccrngd1, steamid=11111, ip=1.1.1.1 //playerConnectedRegex

                    //Telnet connection from: 127.0.0.1:45104 //telnetConnectedRegex

                    //Telnet connection closed by client: 127.0.0.1:45104 //telnetDisconnectedRegex

                    //Telnet executed "gt" from: 127.0.0.1:45104 //telnetCommandExecutedRegex

                    //Execute command "lp" from player "ccrngd1" //playerCommandExecutedRegex

                    //Player with ID 171 has stack for "stick" greater than allowed (2000 > 250) //stackWarningRegex

                    // /rpm player hours - this is a custom command to render the location history for user PLAYER for the last HOURS

                    //llp result
                    //Player "ccrngd1 (123steamdID) owns N keystones (protected: True, current hardness multiplier: 0)
                    //(123,-1,2)
                    #endregion
                    #endregion

                    #region regex

                    var listPlayerResponseRegex = new Regex(MessageText.listPlayerResponse);

                    var getTimeResponseRegex = new Regex(MessageText.getTimeReponse);

                    var helpResponseRegex = new Regex(MessageText.helpResponse);

                    var landProtectionResponseRegex = new Regex(MessageText.landProectionResponse);
                    var landProtectionExtendedRegex = new Regex(MessageText.landProectionExtended);

                    var listKnownPlayerResponseRegex = new Regex(MessageText.listKnownPlayerResponse);

                    var renderPlayerMapRegex = new Regex(MessageText.renderPlayerMap);

                    var startupmsgServerVersionRegex = new Regex(MessageText.startupmsgServerVersion);
                    var startupmsgAllocStatusRegex = new Regex(MessageText.startupmsgAllocStatus);
                    var startupmsgMaxPlayersRegex = new Regex(MessageText.startupmsgMaxPlayer);
                    var startupmsgGameModeRegex = new Regex(MessageText.startupmsgGameMode);
                    var startupmsgWorldRegex = new Regex(MessageText.startupmsgWorld);
                    var startupmsgGameNameRegex = new Regex(MessageText.startupmsgGameName);
                    var startupmsgDifficultyRegex = new Regex(MessageText.startupmsgDifficulty);
                    #endregion

                    if (String.IsNullOrWhiteSpace(msg)) continue;

                    #region INF/WRN/DBG/ERR message

                    if (serverStatusMessageRegex.IsMatch(msg))
                    {
                        var split = serverStatusMessageRegex.Split(msg);

                        var a = serverStatusMessageRegex.Match(msg);

                        DateTime dtStamp =DateTime.ParseExact(a.Value.Split(' ')[0],
                                       "yyyy-MM-dd'T'HH:mm:ss",
                                       System.Globalization.CultureInfo.InvariantCulture,
                                       System.Globalization.DateTimeStyles.AssumeUniversal |
                                       System.Globalization.DateTimeStyles.AdjustToUniversal);

                        //throw away the part that matched the regex, use the other half
                        if (!HandleMessageType(split[split.Length - 1], dtStamp))
                        {
                            //it was not a known msg type so need another handler
                        }
                    }
                    #endregion

                    #region lp command result message
                    else if (msg.Length>8 && listPlayerResponseRegex.IsMatch(msg.Substring(0, 8)))
                    {
                        var splitLp = msg.Substring(0, 8).Split(',');

                        //?  1. id=212, //not sure if this is stripped off, need to check because it would change the offset of the rest of the elements
                        //0  =HTG=JesseBilyk, 
                        //1  pos=(-53.4, 
                        //2  38.0, 
                        //3  1208.0), 
                        //4  rot=(9.8, 
                        //5  -28.1, 
                        //6  0.0), 
                        //7  remote=True, 
                        //8  health=30, 
                        //9  deaths=73, 
                        //10 zombies=649, 
                        //11 players=11, 
                        //12 score=284, 
                        //13 steamid=76561198059471064, 
                        //14 ip=50.71.136.166, 
                        //15 ping=0

                        tempP = new Player();

                        var regexGUID = new Regex("[0-9]*");

                        if (regexGUID.IsMatch(splitLp[13])) throw new ArgumentException("GUID expected at field 13");

                        tempP.GUID = Convert.ToInt32(regexGUID.Match(splitLp[13]));

                        ServerStatusSingleton.Instance.CurrentPlayers.TryGetValue(tempP.GUID, out tempP);

                        var zKills = Convert.ToInt32(splitLp[10].Remove(0, 8));
                        if (tempP.ZombieKills > zKills) continue; //this means the data is stale break out and throw away the msg
                        tempP.ZombieKills = zKills;

                        var pKills = Convert.ToInt32(splitLp[11].Remove(0, 8));
                        if (tempP.PlayerKills > pKills) continue; //this means the data is stale break out and throw away the msg
                        tempP.PlayerKills = pKills;

                        var deaths = Convert.ToInt32(splitLp[9].Remove(0, 7));
                        if (tempP.CurrentDeaths > deaths) continue; //this means the data is stale break out and throw away the msg
                        tempP.CurrentDeaths = deaths;

                        tempP.CurrentIP = splitLp[14].Remove(0, 3);
                        tempP.CurrentPing = Convert.ToInt32(splitLp[15].Remove(0, 5));
                        tempP.CurrentScore = Convert.ToInt32(splitLp[12].Remove(0, 6));
                        tempP.Health = Convert.ToInt32(splitLp[8].Remove(0, 7));

                        var name = splitLp[0];
                        if (tempP.Name != name)
                        {
                            tempP.AliasList.Add(name);
                            tempP.Name = name;
                        }

                        //this should let us save far less often
                        if (SettingsSingleton.Instance.SavePlayerPosition)
                        {
                            double x = Convert.ToDouble(splitLp[1].Remove(0, 5));
                            double y = Convert.ToDouble(splitLp[2]);
                            double z = Convert.ToDouble(splitLp[3].Remove(splitLp[3].Length - 1, 1));

                            tempP.Position = new Point3D {X = x, Y = y, Z = z};

                            double rX = Convert.ToDouble(splitLp[4].Remove(0, 5));
                            double rY = Convert.ToDouble(splitLp[5]);
                            double rZ = Convert.ToDouble(splitLp[6].Remove(splitLp[6].Length - 1, 1));

                            tempP.Rotation = new Point3D {X = rX, Y = rY, Z = rZ};
                        }

                        tempP.Save();
                    }
                        #endregion
                    #region getTime result message
                    else if (getTimeResponseRegex.IsMatch(msg))
                    {
                        var s = msg.Split(',');
                        ServerStatusSingleton.Instance.TimeInGame = new GameTime(Convert.ToInt32(s[0].Substring(5,s[0].Length-4)), s[1]);
                    }
                    #endregion
                    #region help command result message
                    else if (helpResponseRegex.IsMatch(msg)) //this adds all available commands reported out by 'help' to an internal deictionary so we have them persi
                    {
                        var cmdSplit = msg.Split(new[] {"=>"}, StringSplitOptions.RemoveEmptyEntries);

                        if(cmdSplit.Length<2) continue;

                        var cmdAbbreviationSplit = cmdSplit[0].Split(' ');

                        ServerStatusSingleton.Instance.AddAvailableCommand(cmdAbbreviationSplit[0], cmdSplit[1]); 
                    }
                    #endregion
                    #region list land protection llp result message
                    else if (landProtectionResponseRegex.IsMatch(msg))
                    {
                        var llpMsgSplit = msg.Split(' ');

                        //0 - Player
                        //1 - "ccrngd1
                        //2 - (123)"  //(steamid)
                        //3 - owns
                        //4 - 5
                        //5 - keystones
                        //(protected:
                        //True
                        //current
                        //hardness
                        //multiplier:
                        //0) 

                        lastGUID = Int32.Parse(llpMsgSplit[2].Substring(1, llpMsgSplit.Length - 2));
                         
                        continue;
                    }
                    else if (landProtectionExtendedRegex.IsMatch(msg)) // subsequent listLandProtection calls will return (x,y,z) so we need to save who the last person found was
                    {
                        var llpLocationSplit = msg.Split(new[] {", "}, StringSplitOptions.RemoveEmptyEntries);

                        //  (-33
                        //31
                        //119)

                        if(!lastGUID.HasValue) 
                            throw new NullReferenceException("lastGuid can't be null when saving off keystone information");

                        ServerStatusSingleton.Instance.CurrentPlayers[lastGUID.Value].AddKeystone(Convert.ToDouble(llpLocationSplit[0].Substring(llpLocationSplit[0].IndexOf('('), llpLocationSplit[0].Length-2)),
                                                                                             Convert.ToDouble(llpLocationSplit[1]),
                                                                                             Convert.ToDouble(llpLocationSplit[2].Substring(0, llpLocationSplit[2].Length-1)));

                        continue;
                    }
                    #endregion
                    #region list known players result message
                    else if (listKnownPlayerResponseRegex.IsMatch(msg))
                    {
                        //1.
                        //ccrngd1,
                        //id=1234,
                        //steamid=1234,
                        //online=False,
                        //ip=1.1.1.1,
                        //playtime=1 m,
                        //seen=2015-12-31 23:59 
                        continue;
                    }
                    #endregion
                    #region renderPlayerMap result message
                    else if (renderPlayerMapRegex.IsMatch(msg)) //this will pull player location data for a give date range and map them out in relation to the other players last known position
                    {
                        var splitRpm = msg.Split(' ');
                        
                        int? hours = null;

                        if (splitRpm.Length >= 3)
                        {
                            int temp;
                            if(Int32.TryParse(splitRpm[2], out temp)) hours = temp;
                        }

                        ServerStatusSingleton.Instance.DrawLocationHistory(ServerStatusSingleton.Instance.FindPlayer(splitRpm[1]), hours==null?null:(DateTime?)DateTime.Now.AddHours(-1*hours.Value));
                    }
                    #endregion 
                }
                catch (Exception e)
                {
                    if (log4net.LogManager.GetLogger("log").IsInfoEnabled)
                        log4net.LogManager.GetLogger("log").Info(string.Format("error working with msg {0}", msg), e);
                }
            }

            _telnetTask.Wait();
        }

        private static bool HandleMessageType(string message, DateTime timestamp)
        {
            var msgType = (MessageTyes)Enum.Parse(typeof(MessageTyes), message.Substring(0,3));

            if (!Enum.IsDefined(typeof(MessageTyes), msgType))
                return false;

            data.LoggingDA.CommitLogMessage(message, timestamp, msgType, null, null);

            //switch (msgType)
            //{
            //    case MessageTyes.DBG:
            //        break;
            //    case MessageTyes.ERR:
            //        break;
            //    case MessageTyes.INF:
            //        break;
            //    case MessageTyes.WRN:
            //        break;
            //}



            return true;
        }

        private void StartTelnetClient()
        {
            Socket client = null;
            while (!_token.IsCancellationRequested)
            {
                // Connect to a remote device.
                try
                {
                    // Establish the remote endpoint for the socket.
                    IPAddress ipAddress = IPAddress.Parse(SettingsSingleton.Instance.ServerIP);

                    var remoteEP = new IPEndPoint(ipAddress, SettingsSingleton.Instance.ServerTelnetPort);

                    // Create a TCP/IP socket.
                    client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    // Connect to the remote endpoint.
                    client.Connect(remoteEP);

                    // Send test data to the remote device.
                    Send(client, SettingsSingleton.Instance.ServerAdminPassword + "\n"); 

                    while (!_token.IsCancellationRequested)
                    {
                        try
                        {
                            // Receive the response from the remote device.
                            Receive(client);

                            int concurrentCmdsRun = 0; //this will allow for us to break out after we run a set number of commands

                            //check for any cmds that need to be executed
                            while (ServerStatusSingleton.Instance.PendingCommandsToRun.Count > 0)
                            {
                                if (concurrentCmdsRun > SettingsSingleton.Instance.MaxCommandsToProcessConcurrently) break;

                                string cmd;

                                if (!ServerStatusSingleton.Instance.PendingCommandsToRun.TryDequeue(out cmd)) break;
                                if (cmd == null) continue;

                                Send(client, cmd + "\n");

                                concurrentCmdsRun++;
                            }
                        }
                        catch (Exception ex)//the rx or ex loop failed, don't worry carry on but log the error
                        {
                            if (log4net.LogManager.GetLogger("log").IsErrorEnabled)
                                log4net.LogManager.GetLogger("log").Error("Exception in rx/ex loop", ex);
                        }
                    }

                    //// Write the response to the console.
                    //Console.WriteLine("Response received : {0}", response);

                    // Release the socket.
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());

                    if (log4net.LogManager.GetLogger("log").IsFatalEnabled)
                        log4net.LogManager.GetLogger("log").Fatal("Exception in telnetClient", e);
                }
                finally
                {
                    try
                    {
                        if (client != null)
                        {
                            client.Shutdown(SocketShutdown.Both);
                            client.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        if (log4net.LogManager.GetLogger("log").IsWarnEnabled)
                            log4net.LogManager.GetLogger("log").Warn("problem with continuing the telnet", e);
                    }
                }
            }
        }
        
        private void Receive(Socket client)
        {
            try
            {
                // Begin receiving the data from the remote device.
                int bytesRead = client.Receive(_state.Buffer, 0, StateObject.BufferSize, 0);

                if (bytesRead > 0)
                {
                    //The last read may not have been complete, so add this read onto what ever is stored in state.Sb                    
                    string s =_state.Sb + (Encoding.ASCII.GetString(_state.Buffer, 0, bytesRead));
                    
                    var split = s.Split(new[] {"\r\n"}, StringSplitOptions.None);

                    int splitLength = split.Length;

                    //this means it is not a complete read because mulitlines ends with \r\n, sinle lines will have no \r\n at all, 
                    //so save it off to state.Sb so we can finish reading it next time around
                    if (!s.EndsWith("\r\n")) 
                    {
                        _state.Sb = new StringBuilder();
                        _state.Sb.Append(split[split.Length - 1]); // this holds the left over piece that was cut off from the last read
                        splitLength--;
                    }

                    for (int i = 0; i < splitLength; i++)
                    {
                        _incomingMessageConcurrentQueue.Enqueue(split[i]);
                    } 
                }
                else
                {
                    // All the data has arrived; put it in response.
                    if (_state.Sb.Length > 1)
                    {
                        _incomingMessageConcurrentQueue.Enqueue(_state.Sb.ToString());
                    } 
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            client.Send(byteData, 0, byteData.Length, 0);
        }
    }

    // State object for receiving data from remote device.
    public class StateObject
    { 
        // Size of receive buffer.
        public const int BufferSize = 512;
        // Receive buffer.
        public readonly byte[] Buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder Sb = new StringBuilder();
    }

    [Flags]
    public enum MessageTyes
    {
        DBG=100,
        INF=200,
        WRN=500,
        ERR=999,
    } 
}
