using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using System.Net;
using System.IO; 

namespace lawsoncs.htg.sdtd.data
{
    using System;
    using System.Configuration; 

    public class SettingsSingleton
    {
        private static SettingsSingleton _settingsSingleton;
        private static readonly object LockCtor = new object();

        private readonly string _mysqlHost;
        private readonly int _mysqlHostPort;

        private readonly string _mysqlUser;
        private readonly string _mysqlPass;

        public readonly string ConnectionString;

        public readonly int ServerId;
        public string ServerName;
        public readonly string ServerIP;
        public int ServerPort;
        public readonly int ServerTelnetPort;
        public int ServerWebPort;
        public readonly string ServerAdminPassword;
        public string ExternalIP;

        public string SDTDHost;
        public string SDTDTelnetPort;
        public string SDTDWebPort;
        public string SDTDPort;
        public string SDTDTelnetUser;
        public string SDTDTelnetPass;
        public string SDTDServerUser;
        public string SDTDServerPass;

        public string SDTDFtpAllocMapLocation;
        public string SDTDFtpAddress;
        public string SDTDFtpPort;
        public string SDTDFtpUser;
        public string SDTDFtpPass;

        public int MaxCommandsToProcessConcurrently;

        public string PublicFileHost;

        public bool SavePlayerPosition;

        public static SettingsSingleton Instance
        {
            get
            {
                if (_settingsSingleton != null) return _settingsSingleton;

                lock (LockCtor)
                {
                    _settingsSingleton = new SettingsSingleton();
                }

                return _settingsSingleton;
            } 
        }

        private SettingsSingleton()
        {
            _mysqlHost = ConfigurationManager.AppSettings["mysql"];

            _mysqlHostPort = Convert.ToInt32(ConfigurationManager.AppSettings["mysqlPort"]);

            _mysqlUser = ConfigurationManager.AppSettings["mysqluser"];

            _mysqlPass = ConfigurationManager.AppSettings["mysqlpass"];

            ServerId = Convert.ToInt32(ConfigurationManager.AppSettings["serverId"]);

            ConnectionString = string.Format("server={0};Port={3};Uid={1};Pwd={2};database=htg_7dtd", _mysqlHost, _mysqlUser, _mysqlPass, _mysqlHostPort);

            WebRequest wrGeturl = WebRequest.Create("http://checkip.dyndns.org");

            wrGeturl.Proxy = WebProxy.GetDefaultProxy();

            using (Stream objStream = wrGeturl.GetResponse().GetResponseStream())
            {
                if (objStream != null)
                {
                    var objReader = new StreamReader(objStream);

                    var sb = new StringBuilder();
                    var sLine = "";

                    while (sLine != null)
                    {
                        sLine = objReader.ReadLine();
                        if (sLine != null)
                            sb.Append(sLine);
                    }

                    var r = new Regex("[0-9]+.[0-9]+.[0-9]+.[0-9]+");
                    var match = r.Match(sb.ToString());

                    if (match.Length > 0)
                        ExternalIP = match.Value;
                }
            }

            using (var conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    conn.Open(); 
                    if(log4net.LogManager.GetLogger("log").IsDebugEnabled)
                        log4net.LogManager.GetLogger("log").Debug(string.Format("MySQL version : {0}", conn.ServerVersion));

                    using (var cmd = new MySqlCommand("sp_GetServerInfoByServerId", conn))
                    { 
                        cmd.CommandType = CommandType.StoredProcedure;

                        var paramServerId = new MySqlParameter("@servID", ServerId)
                        {
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(paramServerId);


                        var reader = cmd.ExecuteReader();

                        if (reader.Read())
                        {
                            ServerName = reader["serverName"].ToString();
                            ServerIP = reader["serverIP"].ToString();
                            ServerPort = Convert.ToInt32(reader["serverPort"].ToString());
                            ServerTelnetPort = Convert.ToInt32(reader["serverTelnetPort"].ToString());
                            ServerWebPort = Convert.ToInt32(reader["serverWebPort"].ToString());
                            ServerAdminPassword = reader["serverAdminPassword"].ToString(); 
                        }
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
                        if(log4net.LogManager.GetLogger("log").IsInfoEnabled)
                            log4net.LogManager.GetLogger("log").Info("error closing mysql conn", ex);
                    }
                }
            }
        }

        public void SaveSettings()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config.AppSettings.Settings["mysql"].Value = _mysqlHost;

            config.AppSettings.Settings["mysqlPort"].Value = _mysqlHostPort.ToString();

            config.AppSettings.Settings["mysqluser"].Value = _mysqlUser;

            config.AppSettings.Settings["mysqlpass"].Value = _mysqlPass;

            config.AppSettings.Settings["serverId"].Value = ServerId.ToString();

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
