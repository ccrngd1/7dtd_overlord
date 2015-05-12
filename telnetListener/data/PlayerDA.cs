using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using MySql.Data.MySqlClient;
using lawsoncs.htg.sdtd.AdminServer.objects;

namespace lawsoncs.htg.sdtd.AdminServer.data
{
    internal static class PlayerDA
    {
        internal static void SavePlayer(Player p)
        {
            using (var conn = new MySqlConnection(SettingsSingleton.Instance.ConnectionString))
            {
                try
                {
                    conn.Open();

                    using (var cmd = new MySqlCommand("sp_SavePlayer", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var paramServerId = new MySqlParameter("@serverID", SettingsSingleton.Instance.ServerId)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramServerId);


                        var paramPKillsId = new MySqlParameter("@playerKills", p.PlayerKills)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramPKillsId);


                        var paramZKillsId = new MySqlParameter("@zombieKills", p.ZombieKills)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramZKillsId);


                        var paramDeathsId = new MySqlParameter("@deaths", p.CurrentDeaths)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramDeathsId);


                        var paramScoreId = new MySqlParameter("@score", p.CurrentScore)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramScoreId);


                        var paramPingId = new MySqlParameter("@ping", p.CurrentPing)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramPingId);

                        #region player position/rotation

                        if (SettingsSingleton.Instance.SavePlayerPosition)
                        {
                            if (p.Position != null)
                            {
                                var paramPosXId = new MySqlParameter("@posX", p.Position.Value.X)
                                {
                                    Direction = ParameterDirection.Input
                                };
                                cmd.Parameters.Add(paramPosXId);

                                var paramPosYId = new MySqlParameter("@posY", p.Position.Value.X)
                                {
                                    Direction = ParameterDirection.Input
                                };
                                cmd.Parameters.Add(paramPosYId);

                                var paramPosZId = new MySqlParameter("@posZ", p.Position.Value.X)
                                {
                                    Direction = ParameterDirection.Input
                                };
                                cmd.Parameters.Add(paramPosZId);
                            }

                            if (p.Rotation != null)
                            {
                                var paramRotXId = new MySqlParameter("@rotX", p.Rotation.Value.X)
                                {
                                    Direction = ParameterDirection.Input
                                };
                                cmd.Parameters.Add(paramRotXId);

                                var paramRotYId = new MySqlParameter("@rotY", p.Rotation.Value.X)
                                {
                                    Direction = ParameterDirection.Input
                                };
                                cmd.Parameters.Add(paramRotYId);

                                var paramRotZId = new MySqlParameter("@rotZ", p.Rotation.Value.X)
                                {
                                    Direction = ParameterDirection.Input
                                };
                                cmd.Parameters.Add(paramRotZId);
                            }
                        }

                        #endregion

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

        internal static void SaveAlias()
        {
        }

        internal static void SaveCurrentBackpack()
        {
        }

        internal static void SaveCurrentToolbar()
        {
        }

        /// <summary>
        /// Get a player based on the full or partial player name
        /// </summary>
        /// <param name="playerName">full player name or the start of the player name to be found</param>
        /// <param name="isPartialName">mark whether the player name should be treated as a partial name</param>
        /// <returns>a full player object</returns>
        internal static Player GetPlayer(string playerName, bool isPartialName=true)
        {
            var retVal = new Player();

            using (var conn = new MySqlConnection(SettingsSingleton.Instance.ConnectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("sp_GetPlayerByName", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var paramPlayerName = new MySqlParameter("@playerName", playerName)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramPlayerName);

                        var paramIsPartialName = new MySqlParameter("@isPartialName", isPartialName)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramIsPartialName);
                        
                        retVal = GeneratePlayerFromReader(cmd.ExecuteReader());
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

        /// <summary>
        /// Get a player based on the player GUID key
        /// </summary>
        /// <param name="GUID">the GUID key to pull the player back</param>
        /// <returns>a full player object</returns>
        internal static Player GetPlayer(int GUID)
        {
            var retVal = new Player();

            using (var conn = new MySqlConnection(SettingsSingleton.Instance.ConnectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("sp_GetPlayerByName", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var paramPlayerName = new MySqlParameter("@playerGUID", GUID)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramPlayerName);

                        retVal = GeneratePlayerFromReader(cmd.ExecuteReader());
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

        private static Player GeneratePlayerFromReader(MySqlDataReader reader)
        {
            return GeneratePlayerFromReader(reader, 1)[0];
        }

        /// <summary>
        /// a common method to populate a player objects from a sql data reader
        /// </summary>
        /// <param name="reader">the executed data reader to be read from</param>
        /// <param name="playerReturnLimit">how many players to return</param>
        /// <returns>player objects</returns>
        private static List<Player> GeneratePlayerFromReader(MySqlDataReader reader, int playerReturnLimit)
        {
            var retVal= new List<Player>();

            int i = 1;
            while (reader.Read())
            {
                if (i > playerReturnLimit)
                {
                    retVal = null;
                    break;
                }
                i++;

                var temp = new Player
                {
                    Name = reader["playerName"].ToString(), 
                    GUID = (int) reader["GUID"]
                };

                //todo finish filling out player obj

                retVal.Add(temp);
            }
            return retVal;
        }

        internal static IEnumerable<string> GetAlias(int guid)
        {
            var retVal = new List<string>();

            using (var conn = new MySqlConnection(SettingsSingleton.Instance.ConnectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("sp_GetServerInfoByServerId", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var paramGUIDId = new MySqlParameter("@GUID", guid)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramGUIDId);

                        var reader = cmd.ExecuteReader(); 

                        if (reader.Read())
                        {
                            retVal.Add(reader["playerAlias"].ToString()); 
                        }
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

        internal static void InsertAlias(int guid, string name)
        { 
            using (var conn = new MySqlConnection(SettingsSingleton.Instance.ConnectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("sp_AddAliasIfNew", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var paramGUIDId = new MySqlParameter("@guid", guid)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramGUIDId);

                        var paramnameAlias = new MySqlParameter("@nameAlias", name)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramnameAlias);

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
        /// Get the location of the player since the 'oldestTimeStamp' until current, into a queue with positions ordered by timestamp descending
        /// </summary>
        /// <param name="p">the player to get location history on</param>
        /// <param name="oldestTimestamp">the oldest location entry to use</param>
        /// <returns></returns>
        internal static bool GetLocationHistory(int guid, DateTime? oldestTimestamp, ref Queue<Point3D> location)
        {
            return true;
        }
    }
}
