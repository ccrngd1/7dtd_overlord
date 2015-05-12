using System; 
using System.Collections.Concurrent; 
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using lawsoncs.htg.sdtd.AdminServer.data;
using lawsoncs.htg.sdtd.AdminServer.objects;

namespace lawsoncs.htg.sdtd.AdminServer
{
    public class ServerStatusSingleton
    {
        public ConcurrentQueue<string> PendingCommandsToRun;

        private static ServerStatusSingleton _serverStatusSingleton;
        private static readonly object LockCtor = new object();

        private string _connectionStatus;
        private static readonly object LockConnStatus = new object();

        public string ConnectionStatus
        {
            get { return _connectionStatus; }
            set
            {
                lock (LockConnStatus)
                {
                    _connectionStatus = value;
                }
            }
        }

        private int _playerLimit;
        private static readonly object LockPlayerLimit = new object();
        public int PlayerLimit
        {
            get { return _playerLimit; }
            set
            {
                lock (LockPlayerLimit)
                {
                    _playerLimit = value;
                }
            }
        }

        private GameTime _gameTime;
        private static readonly object LockGameTimeStatus = new object();


        public GameTime TimeInGame
        {
            get { return _gameTime; }
            set
            {
                if (value < _gameTime) return;

                lock (LockGameTimeStatus)
                {
                    _gameTime = value;
                }
            }
        } 

        public ConcurrentDictionary<int, Player> CurrentPlayers ;

        private ConcurrentDictionary<string, string> AvailableCommandsDictionary;

        public void AddAvailableCommand(string key, string value)
        {
            string s;

            if (AvailableCommandsDictionary.TryGetValue(key, out s)) return;

            AvailableCommandsDictionary.TryAdd(key, value);
            ServerDA.AddServerCommand(key, value);
        }

        public static ServerStatusSingleton Instance
        {
            get
            {
                if (_serverStatusSingleton != null) return _serverStatusSingleton;

                lock (LockCtor)
                {
                    _serverStatusSingleton = new ServerStatusSingleton();
                }

                try
                {
                    _serverStatusSingleton.Refresh();
                }
                catch (Exception ex)
                {
                    if (log4net.LogManager.GetLogger("log").IsErrorEnabled)
                        log4net.LogManager.GetLogger("log").Error("Exception in rx/ex loop", ex);
                }

                return _serverStatusSingleton;
            }
        }

        private void Refresh() { ServerDA.GetPendingCommands(ref PendingCommandsToRun, SettingsSingleton.Instance.ServerId); }

        private ServerStatusSingleton()
        {
            PendingCommandsToRun = new ConcurrentQueue<string>(); 
            CurrentPlayers = new ConcurrentDictionary<int, Player>();
            AvailableCommandsDictionary = ServerDA.GetServerCommands();
        }

        public void DrawLocationHistory(Player p, DateTime? oldestTimeStamp)
        {
            if (p == null) return;

            //where to save the picture? needs to be a config location, preferably ftp based so we can throw it to a website
            const string plainMapLocation = "C:\\temp\\";
            var resultLocation = "C:\\temp\\" + p.Name + "(" + p.GUID + ")"; 
            
            Bitmap map = null;
            if (ServerDA.GetCombinedServerMap(plainMapLocation))
            {
                map = new Bitmap(plainMapLocation);
            }

            if (map == null)
            {
                var extents = DetermineImageExtent(p);
                map = new Bitmap((int)Math.Ceiling(extents.Item2.X-extents.Item1.X), (int)Math.Ceiling(extents.Item2.Y-extents.Item1.Y));
            }

            var flagGfx = Graphics.FromImage(map);

                        flagGfx.SmoothingMode = SmoothingMode.AntiAlias;
                        flagGfx.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        flagGfx.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var playerLocations = p.GetLocationHistory(oldestTimeStamp);

            if (playerLocations != null && playerLocations.Count>0)
            {
                int i = 0;
                foreach (var playerLocation in playerLocations)
                {
                    if (i == 0) //for the  most recent location, make it red
                    {
                        flagGfx.FillEllipse(Brushes.Red, (int) playerLocation.X, (int) playerLocation.Y, 5, 5);

                        i++;
                    }
                    else //all subsequent locations should be in pink,
                         //todo it would be cool if each subsequent location was lighter red until the last step which would be barely pink
                         //todo would also be nice if we drew lines between the points to show how the person was moving
                    {
                        flagGfx.FillEllipse(Brushes.Pink, (int)playerLocation.X, (int)playerLocation.Y, 5, 5);
                    }

                    flagGfx.Save();

                    //now put the location 
                    flagGfx.DrawString(string.Format("({0},{1}) - 1", playerLocation.X, playerLocation.Y), new Font("Tahoma", 8),
                        Brushes.Black, (int)playerLocation.X - 5, (int)playerLocation.Y - 5);
                }
            }
             
            foreach (var currentPlayer in CurrentPlayers)
            {
                if (currentPlayer.Value == null || currentPlayer.Value.Position == null) continue;

                flagGfx.FillEllipse(Brushes.Green, (int)currentPlayer.Value.Position.Value.X, (int)currentPlayer.Value.Position.Value.Y, 5,5);
            }

            flagGfx.Save();

            using (var fs = new FileStream(resultLocation, FileMode.OpenOrCreate))
            {
                map.Save(fs, ImageFormat.Bmp);
            }
        }

        private Tuple<Point3D, Point3D> DetermineImageExtent(Player p)
        {
            if (p == null) return null;
            if (p.Position == null) return null;

            var min = new Point3D{X=p.Position.Value.X, Y=p.Position.Value.Y};
            var max = new Point3D{X=p.Position.Value.X, Y=p.Position.Value.Y};

            foreach (var currentPlayer in CurrentPlayers)
            {
                if(currentPlayer.Value==null)continue;
                if(!currentPlayer.Value.Position.HasValue) continue;

                if (currentPlayer.Value.Position.Value.X > max.X) max.X = currentPlayer.Value.Position.Value.X;
                if (currentPlayer.Value.Position.Value.Y > max.Y) max.Y = currentPlayer.Value.Position.Value.Y;
                if (currentPlayer.Value.Position.Value.X < min.X) min.X = currentPlayer.Value.Position.Value.X;
                if (currentPlayer.Value.Position.Value.Y < min.Y) min.Y = currentPlayer.Value.Position.Value.Y;
            }

            return new Tuple<Point3D, Point3D>(min, max);
        }

        internal Player FindPlayer(string p)
        {
            if (p.Length < 5)
            {
                //todo submit /say (or /pm to the user who issued the command) username has to be >5 chars to try to match

                PendingCommandsToRun.Enqueue("/say {0} is too short, it has to be 5 characters or longer");
            }

            var foundPlayer = CurrentPlayers.FirstOrDefault(c => c.Value.Name.StartsWith(p));

            if (foundPlayer.Value != null && foundPlayer.Value.Name != null)
            {
                return foundPlayer.Value;
            }

            //else we need to hit the db and get data about this user
            return PlayerDA.GetPlayer(p);
        }
    }

    public class GameTime
    {
        public int Day;
        public int Hour;
        public int Minute;

        public GameTime(int day, string time)
        {
            Day = day;
            var t = time.Split(':');
            Hour = Convert.ToInt32(t[0]);
            Minute = Convert.ToInt32(t[1]);
        }

        public static bool operator <(GameTime a, GameTime b)
        {
            if (a.Day < b.Day) return true;

            if (a.Day == b.Day)
            {
                if (a.Hour < b.Hour) return true;

                if (a.Hour == b.Hour)
                {
                    if (a.Minute < b.Minute) return true;
                }
            }

            return false;
        }

        public static bool operator >(GameTime a, GameTime b)
        {
            if (a.Day > b.Day) return true;

            if (a.Day == b.Day)
            {
                if (a.Hour > b.Hour) return true;

                if (a.Hour == b.Hour)
                {
                    if (a.Minute > b.Minute) return true;
                }
            }

            return false;
        }

    }
}
