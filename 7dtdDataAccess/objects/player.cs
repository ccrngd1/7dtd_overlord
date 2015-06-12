using System;

using System.Collections.Concurrent;
using System.Collections.Generic;

using System.Text;
using lawsoncs.htg.sdtd.AdminServer.objects;
using lawsoncs.htg.sdtd.data;

namespace lawsoncs.htg.sdtd.data.objects
{
    public class Player
    {
        private int _guid;
        private string _name; 
        private int _playerKills; 
        private int _zombieKills; 
        private int _currentDeaths; 
        private int _currentScore;
        private string _currentIP; 
        private int _totalVisits; 
        private int _health;

        public Point3D? Position;
        public Point3D? Rotation;

        private readonly ConcurrentDictionary<string, Tuple<int, Point3D>> _keystonesList = new ConcurrentDictionary<string, Tuple<int, Point3D>>();

        public void AddKeystone(double x, double y, double z)
        {
            string hash = new StringBuilder(x.ToString()).Append(y.ToString()).Append(z.ToString()).ToString();

            Tuple<int, Point3D> value;

            if (_keystonesList.TryGetValue(hash, out value))//the keystone was save off before, increment the int of <int, point3d> and save back
            {
                _keystonesList[hash] = new Tuple<int, Point3D>(value.Item1+1, value.Item2);
            }
            else //keystone was never saved off, insert it
            {
                _keystonesList.TryAdd(hash, new Tuple<int, Point3D>(1, new Point3D {X = x, Y = y, Z = z}));
            }
        }

        private bool _isPlayerDirty;
        private bool _isBackpackDirty;
        private bool _isToolbarDirty;

        private List<string> _newAlias = new List<string>(); 

        public int GUID
        {
            get { return _guid; }
            set
            {
                if (value == _guid) return;
    
                _isPlayerDirty = true;

                _guid = value;

                AliasList.AddRange(PlayerDA.GetAlias(_guid)); //as soon as we have the GUID, bring in all of the alias we know of so far
            }
        }
        public string Name
        {
            get { return _name; }
            set
            {
                if (value == _name) return;

                _isPlayerDirty = true;

                AliasList.Add(Name);
                
                _name = value;
            }
        }
        public int PlayerKills
        {
            get { return _playerKills; }
            set
            {
                if (value == _playerKills) return;

                _isPlayerDirty = true;

                _playerKills = value;
            }
        }
        public int ZombieKills
        {
            get { return _zombieKills; }
            set
            {
                if (value == _zombieKills) return;

                _isPlayerDirty = true; 
                
                _zombieKills = value;
            }
        }
        public int CurrentDeaths
        {
            get { return _currentDeaths; }
            set
            {
                if (value == _currentDeaths) return;

                _isPlayerDirty = true; 
                
                _currentDeaths = value;
            }
        }
        public int CurrentScore
        {
            get { return _currentScore; }
            set
            {
                if (value == _currentScore) return;

                _isPlayerDirty = true;
                
                _currentScore = value;
            }
        }

        public int CurrentPing { get; set; }

        public string CurrentIP
        {
            get { return _currentIP; }
            set
            {
                if (value == _currentIP) return;

                _isPlayerDirty = true;
                
                _currentIP = value;
            }
        }
        public int TotalVisits
        {
            get { return _totalVisits; }
            set
            {
                if (value == _totalVisits) return;

                _isPlayerDirty = true;

                _totalVisits = value;
            }
        }
        public int Health
        {
            get { return _health; }
            set
            {
                if (value == _health) return;

                _isPlayerDirty = true;

                _health = value;
            }
        }

        //lkp vars
        public int ReportedPlayTime;
        public DateTime ReportedLastSeen;

        public Dictionary<refGameItem, int> CurrentBackpackItems { get; private set; }
        public Dictionary<refGameItem, int> CurrentToolbarItems { get; private set; }
        public List<string> AliasList { get; private set; }

        public void UpdateBackpack(refGameItem item, int quantity)
        {
            if (CurrentBackpackItems.ContainsKey(item))
            {
                if (CurrentBackpackItems[item] == quantity)
                {
                    return;
                }

                CurrentBackpackItems[item] = quantity;
            }
            else
            {
                CurrentBackpackItems.Add(item, quantity);
            }
            _isBackpackDirty = true;
        }

        public Player()
        {
            AliasList = new List<string>();
            CurrentBackpackItems = new Dictionary<refGameItem, int>();
            CurrentToolbarItems = new Dictionary<refGameItem, int>();
        }
        public bool Save()
        {
            bool retVal = false;

            try
            {
                if (_isPlayerDirty) PlayerDA.SavePlayer(this);

                if (_newAlias.Count != 0)
                {
                    PlayerDA.SaveAlias();
                    _newAlias = new List<string>();
                }

                if (_isBackpackDirty) PlayerDA.SaveCurrentBackpack();

                if (_isToolbarDirty) PlayerDA.SaveCurrentToolbar();

                _isBackpackDirty = false;
                _isPlayerDirty = false;
                _isToolbarDirty = false;
                retVal = true;
            }
            catch (Exception ex)
            {
                if (log4net.LogManager.GetLogger("log").IsWarnEnabled)
                    log4net.LogManager.GetLogger("log").Warn(string.Format("error saving part of player"), ex);
            }

            return retVal;
        }

        public Queue<Point3D> GetLocationHistory(DateTime? oldestTimeStamp)
        {
            var retVal = new Queue<Point3D>();

            if (Position.HasValue)
                retVal.Enqueue(Position.Value);

            PlayerDA.GetLocationHistory(GUID, oldestTimeStamp, ref retVal);

            return retVal;
        }
    }

    public struct Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    } 
}
