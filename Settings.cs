using HomeSeerAPI;

namespace HSPI_AKExample
{
    public class Settings
    {
        private IHSApplication Hs { get; set; }

        private string IniFile { get; set; }

        public Settings(string IniFile, IHSApplication Hs)
        {
            this.IniFile = IniFile;
            this.Hs = Hs;
            Load();
        }

        private string _location;
        public string Location
        {
            get => _location;
            set
            {
                _location = value;
                SaveVal("Location", _location);
            }
        }

        private string _location2;
        public string Location2
        {
            get => _location2;
            set
            {
                _location2 = value;
                SaveVal("Location2", _location2);
            }
        }

        private string _selectedDeviceRef;
        public string SelectedDeviceRef
        {
            get => _selectedDeviceRef;
            set
            {
                _selectedDeviceRef = value;
                SaveVal("SelectedDeviceRef", _selectedDeviceRef);
            }
        }

        private bool _debugLog;
        public bool DebugLog
        {
            get => _debugLog;
            set
            {
                _debugLog = value;
                SaveVal("DebugLog", _debugLog.ToString());
            }
        }


        /// <summary>
        /// Call UpdateConfiguration in main or new thread
        /// </summary>
        private bool _InitIOinThread;
        public bool InitIOinThread
        {
            get => _InitIOinThread;
            set
            {
                _InitIOinThread = value;
                SaveVal("InitIOinThread", _InitIOinThread.ToString());
            }
        }

        /// <summary>
        /// Display tables inside SliderTab
        /// </summary>
        private bool _InSlider;
        public bool InSlider
        {
            get => _InSlider;
            set
            {
                _InSlider = value;
                SaveVal("InSlider", _InSlider.ToString());
            }
        }

        /// <summary>
        /// Prevent calling SaveVal while calling Load()
        /// </summary>
        private bool loading = false;

        public void Load()
        {
            loading = true;

            InSlider = bool.Parse(LoadVal("InSlider", "true"));
            InitIOinThread = bool.Parse(LoadVal("InitIOinThread", "true"));
            DebugLog = bool.Parse(LoadVal("DebugLog", "false"));
            Location = LoadVal("Location");
            Location2 = LoadVal("Location2");
            SelectedDeviceRef = LoadVal("SelectedDeviceRef");

            loading = false;
        }

        public void Save()
        {
            SaveVal("InSlider", InSlider.ToString());
            SaveVal("InitIOinThread", InitIOinThread.ToString());
            SaveVal("DebugLog", DebugLog.ToString());
            SaveVal("Location", Location);
            SaveVal("Location2", Location2);
            SaveVal("SelectedDeviceRef", SelectedDeviceRef);
        }

        public string LoadVal(string key, string value = "", string fName = null)
        {
            if(fName==null) fName = IniFile;
            return Hs.GetINISetting("Settings", key, value, fName);
        }

        public void SaveVal(string key, string value, string fName = null)
        {
            if(!loading)
            { 
                if(fName==null) fName = IniFile;
                Hs.SaveINISetting("Settings", key, value, fName); 
            }
        }
    }
}