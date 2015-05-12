namespace lawsoncs.htg.sdtd.AdminServer.data
{
    public  static class MessageText
    {
        public static string serverStatusDirectorSpawnGeo = @"AIDirector: \S* was spawned and is moving";
        public static string serverStatusDirectorSpawned =@"Spawned \[";
        public static string serverStatusDirectorSpawningWave="Spawning this wave:";
        public static string serverStatusDirectorSpawning = "Spawning";
        public static string serverStatusPlayerGreaterStack = @"Player with ID \d* has stack for";
        public static string serverStatusPlayerOnlineMessage = @"Player set to online: ";

        public static string serverStatusPlayerConnectedMessage =
            @"Player connected, clientid=\d*, entityid=\d*, name=.*, ip=\d+.\d+.\d+.\d+";

        public static string serverStatusAddingEntityMessage =
            @"Adding observed entity: \d*, \((-?)\d.\d, (-?)\d.\d, (-?)\d.\d\), \d*";

        public static string serverStatusCreatedPlayerMessage = @"Created player with id=";
        public static string serverStatusRequestSpawnMessage = @"RequestToSpawnPlayer:";
        public static string serverStatusSayMessage = @"GMSG: ";
        public static string serverStatusRequestToEnterMessage = @"RequestToEnterGame: ";
        public static string serverStatusRemoveInvalidPlayerMessage = @"Removing player with id clientId=\d*, entityId=-1";
        public static string serverStatusInvalidLoginMessage = @"\[Steamworks.NET\] Kick player for invalid login:";
        public static string serverStatusUserStatusHandlerCallbackMessage = @"\[EAC\] UserStatusHandler callback. Status: ";
        public static string serverStatusRegisteringUserMessage = @"\[EAC\] Registering user: id=(\d)*, owner=(\d)*";
        public static string serverStatusAuthenticationCallbackMessage = @"\[Steamworks.NET\] Authentication callback. ID: \d*, owner: \d*, result: ";
        public static string serverStatusAllowingPlayerMessage = @"Allowing player with id (\d)*";
        public static string serverStatusAuthenticatingPlayerMessage = @"\[Steamworks.NET\] Authenticating player: ";
        public static string serverStatusAuthenticateUser = @"\[Steamworks.NET\] Auth.AuthenticateUser";
        public static string serverStatusTokenMessage = "Token length:";
        public static string serverStatusPlayerLoginMessage = @"PlayerLogin:";
        public static string serverStatusOnPlayerConnectMessage = @"OnPlayerConnected (\d)*";
        public static string serverStatusUserStatusHandlerMessage = @"\[EAC\] UserStatusHandler callback. Status: Disconnected GUID: ";
        public static string serverStatusOnPlayerDisConnectMessage = @"OnPlayerDisconnected (\d)*";
        public static string serverStatusRemoveEntityMessage = @"Removing observed entity (\d)*";
        public static string serverStatusInGameMessage=@"GMSG: ";
        public static string serverStatusFreeUserMessage = @"\[EAC\] FreeUser \(";
        public static string serverStatusPlayerDisConnectMessage = @"Player disconnected: ClientID=(\d)*, EntityID=(\d)*, PlayerID='(\d)*', PlayerName='";
        public static string serverStatusRemovePlayerMessage = @"Removing player with id clientId=(\d)*, entityId=(\d)*";
        public static string serverStatusPlayerSetOffMessage = "Player set to offline: ";
        public static string serverStatusTelentMessage = "Telnet executed \"";
        public static string serverStatusGenMessage = @"Time: (\d)*.(\d)*m FPS: (\d)*.(\d)* Heap: (\d)*.(\d)*MB Max: (\d)*.(\d)*MB Chunks: (\d)* CGO: (\d)* Ply: (\d)* Zom: (\d)* Ent: (\d)* \((\d)*\) Items: (\d)*";
        public static string serverStatusStatsMessage = @"STATS:";
        public static string esrverStatusFellMessage = @"Entity \[type=(\w)*, name=(\w)*, id=(\d)*\] fell";

        public static string listPlayerResponse = "[0-9]+. id=";
    }
}
