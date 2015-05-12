using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using MySql.Data.MySqlClient;
using System.Net.FtpClient;

namespace lawsoncs.htg.sdtd.AdminServer.data
{
    public static class ServerDA
    {
        public static void GetPendingCommands(ref ConcurrentQueue<string> pendingCommandsQueue, int serverId)
        {  
            using (var conn = new MySqlConnection(SettingsSingleton.Instance.ConnectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("sp_GetPendingCommands", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var paramCmdName = new MySqlParameter("@sID", serverId)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramCmdName);

                        var reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            pendingCommandsQueue.Enqueue(reader["command"].ToString());
                        }

                        cmd.ExecuteNonQuery();
                    }
                }
                catch (MySqlException ex)
                {
                    if (log4net.LogManager.GetLogger("log").IsFatalEnabled)
                        log4net.LogManager.GetLogger("log").Fatal("error exec mysql conn", ex);
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

        /// <summary>
        /// Add an entry into the server commands table in the sql backend
        /// </summary>
        /// <param name="key">the actual command name ('help', 'quit', 'lp', etc)</param>
        /// <param name="value">the description of the command being added</param>
        public static void AddServerCommand(string key, string value)
        {
            using (var conn = new MySqlConnection(SettingsSingleton.Instance.ConnectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("sp_AddServerCommand", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var paramCmdName = new MySqlParameter("@commandName", key)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramCmdName);

                        var paramCmdDef = new MySqlParameter("@commandDef", value)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramCmdDef);

                        cmd.ExecuteNonQuery();
                    }
                }
                catch (MySqlException ex)
                {
                    if (log4net.LogManager.GetLogger("log").IsFatalEnabled)
                        log4net.LogManager.GetLogger("log").Fatal("error exec mysql conn", ex);
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

        /// <summary>
        /// Get List of server commands from the sql backend
        /// </summary>
        /// <returns>list of commands and their descriptions</returns>
        public static ConcurrentDictionary<string, string> GetServerCommands()
        {
            var retVal = new ConcurrentDictionary<string, string>();
            using (var conn = new MySqlConnection(SettingsSingleton.Instance.ConnectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("sp_GetServerCommand", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            retVal.TryAdd(reader["commandName"].ToString(), reader["commandDefinition"].ToString());
                        }
                        
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (MySqlException ex)
                {
                    if (log4net.LogManager.GetLogger("log").IsFatalEnabled)
                        log4net.LogManager.GetLogger("log").Fatal("error exec mysql conn", ex);
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

            return retVal;
        }

        public static bool GetCombinedServerMap(string createdFileLocation)
        {
            bool retVal = false;
            try
            {
                using (var ftpClient = new FtpClient())
                {
                    ftpClient.Credentials = new NetworkCredential {UserName = SettingsSingleton.Instance.SDTDFtpUser, Password = SettingsSingleton.Instance.SDTDFtpPass};
                    ftpClient.Host = SettingsSingleton.Instance.SDTDFtpAddress;
                    ftpClient.Port = Convert.ToInt32(SettingsSingleton.Instance.SDTDFtpPort);

                    ftpClient.Connect();

                    if (log4net.LogManager.GetLogger("log").IsInfoEnabled)
                        log4net.LogManager.GetLogger("log").Info(string.Format("found {0} objects in the main directory", ftpClient.GetListing().Count()));

                    //the world is located in /SaveGame/map/
                    //the images are stored there as /zoomLevel/X/y.png (ie /0/1/-1 for zoom level 0, x = 1, and y=-1
                    string fullWorkingDirectory = ftpClient.GetWorkingDirectory() + "/SaveGame/map/";

                    ftpClient.SetWorkingDirectory(fullWorkingDirectory);

                    if (log4net.LogManager.GetLogger("log").IsInfoEnabled)
                        log4net.LogManager.GetLogger("log").Info(string.Format("found {0} objects in the main directory", ftpClient.GetListing().Count()));

                    int zoomLevel = 6;
                    while (!ftpClient.DirectoryExists(zoomLevel.ToString()) && zoomLevel >= 0)
                    {
                        zoomLevel--;
                    }

                    if (zoomLevel < 0) return false;

                    var fileListing = ftpClient.GetListing();

                    if (log4net.LogManager.GetLogger("log").IsInfoEnabled)
                        log4net.LogManager.GetLogger("log").Info(string.Format("found {0} objects in the main directory", fileListing.Count()));


                    var foundMaps = new List<string>();
                    foreach (var ftpListItem in fileListing) //this should be @ /SaveGame/map/X/, so any subfiles should be Y.png's
                    {
                        if (ftpListItem.Type != FtpFileSystemObjectType.Directory) continue;

                        foundMaps.AddRange(FindSubfiles(ftpClient, createdFileLocation + "\\" + ftpListItem.Name));
                        ftpClient.SetWorkingDirectory(fullWorkingDirectory);
                    }
                }
                retVal = true;
            }
            catch (Exception ex)
            {
                if (log4net.LogManager.GetLogger("log").IsErrorEnabled)
                    log4net.LogManager.GetLogger("log").Error(string.Format("error getting map pieces", ex));
            } //if this fails, log a msg but kick it back

            return retVal;
        }

        private static IEnumerable<string> FindSubfiles(FtpClient ftpClient, string saveLocation)
        {
            var retVal = new List<string>();

            //todo finish

            foreach (var ftpListItem in ftpClient.GetListing())
            {
                if (ftpListItem.Type == FtpFileSystemObjectType.File)
                {
                    if (File.Exists(saveLocation + "." + ftpListItem.Name))continue;

                    retVal.Add(saveLocation + "." + ftpListItem.Name);

                    using(var fs = new FileStream(saveLocation+"."+ftpListItem.Name, FileMode.CreateNew))
                    using (Stream s = ftpClient.OpenRead(ftpListItem.FullName))
                    {
                        const int byteSize = 1024;
                        var buffer = new byte[1024];
                        int byteOffset = 0;
                        while (byteOffset < s.Length)
                        {
                            s.Read(buffer, byteOffset, byteSize);

                            fs.Write(buffer, byteOffset, byteSize);
                            byteOffset += byteSize;
                        }
                        fs.Flush();
                    }
                }
            }

            return retVal;
        } 
    }
}
