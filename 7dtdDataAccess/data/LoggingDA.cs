using System;
using System.Data;
using lawsoncs.htg.sdtd.data.objects;
using MySql.Data.MySqlClient;

namespace lawsoncs.htg.sdtd.data
{
    public static class LoggingDA
    {
        //`sp_SaveLogMsg` (in logTimeStamp datetime, in messageTypeId int, in message varchar(255),  in serverId int,
        //                            in playerAsscGUID int, in adminAccsGUID int)
        public static void CommitLogMessage(string message, DateTime timestamp, MessageTyes mtype, int? playerGUID, int? adminGUID)
        {
            using (var conn = new MySqlConnection(SettingsSingleton.Instance.ConnectionString))
            {
                try
                {
                    conn.Open();

                    using (var cmd = new MySqlCommand("sp_SaveLogMsg", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var paramServerId = new MySqlParameter("@serverID", SettingsSingleton.Instance.ServerId)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramServerId);

                        var paramMessage = new MySqlParameter("@message", message)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramMessage);

                        var paramdatetime = new MySqlParameter("@logTimeStamp", timestamp)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramdatetime);

                        var paramplayerAsscGuid = new MySqlParameter("@playerAsscGUID", playerGUID)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramplayerAsscGuid);

                        var paramadminAccsGuid = new MySqlParameter("@adminAccsGUID", adminGUID)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramadminAccsGuid);

                        var parammessageTypeId = new MySqlParameter("@messageTypeId", mtype)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(parammessageTypeId);

                        cmd.ExecuteNonQuery();
                    }

                }
                catch (MySqlException ex)
                {
                    if (log4net.LogManager.GetLogger("log").IsFatalEnabled)
                        log4net.LogManager.GetLogger("log").Fatal("error closing mysql conn", ex);
                }
                finally
                {
                    try
                    {
                        conn.Close();
                    }
                    catch (Exception ex)
                    {
                        if (log4net.LogManager.GetLogger("log").IsInfoEnabled)
                            log4net.LogManager.GetLogger("log").Info("error closing mysql conn", ex);
                    }
                }
            }
        }
    }
}
