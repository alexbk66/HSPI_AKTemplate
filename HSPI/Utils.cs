using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

using HomeSeerAPI;
using Scheduler.Classes;
using static HomeSeerAPI.PlugExtraData;

namespace HSPI_AKTemplate
{
    using HSDevice = Scheduler.Classes.DeviceClass;
    using DeviceDictHS = Dictionary<int, DeviceClass>;

    public class Utils
    {

        public static IHSApplication Hs { get; set; }
        public static string PluginName { get; set; }

        public static bool DeSerializingWarnings = false;

        #region Log

        public enum LogType
        {
            Debug = -1,
            Normal = 0,
            Warning = 1,
            Error = 2
        }

        ///<summary>
        ///Logging
        ///</summary>
        ///<param name="Message">The message to be logged</param>
        ///<param name="Log_Level">Normal, Warning or Error</param>
        ///<remarks>HSPI_SAMPLE_BASIC</remarks>
        public static void Log(string message, LogType logLevel = LogType.Normal)
        {
            try
            {
                switch (logLevel)
                {
                    case LogType.Debug:
                        //if (_settings.DebugLog)
                        {
                            Hs.WriteLog(PluginName + " Debug", message);
                        }

                        break;
                    case LogType.Normal:
                        Hs.WriteLog(PluginName, message);
                        break;


                    case LogType.Warning:
                        Hs.WriteLog(PluginName + " Warning", message);
                        break;

                    case LogType.Error:
                        Hs.WriteLog(PluginName + " ERROR", message);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Log(): '{message}'. {ex.Message}");
            }
        }

        #endregion Log


        #region SerializeObject and clsPlugExtraData

        ///<summary>
        /// Adds serialized PluginExtraData to a device, removes and adds if it already exists
        /// Low level function
        ///</summary>
        ///<param name="PED">The PED object to return data to (ByRef)</param>
        ///<param name="PEDName">The key to look for. Moskus: I only use the pl</param>
        ///<param name="PEDValue"></param>
        ///<remarks></remarks>
        public static void PedAdd<T>(ref clsPlugExtraData ped, string pedName, T pedValue)
        {
            if (ped == null)
            {
                ped = new clsPlugExtraData();
            }

            SerializeObject(pedValue, out byte[] byteObject);

            bool ret1 = ped.RemoveNamed(pedName);
            bool ret2 = ped.AddNamed(pedName, byteObject);
        }

        /// <summary>
        /// Get named PED
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pedName"></param>
        /// <returns></returns>
        public static T GetPED<T>(string pedName, clsPlugExtraData ped, T deflt = default(T))
        {
            object val = PedGet(ped, pedName);
            if (val == null)
                val = deflt;
            try
            {
                return (T)val;
            }
            catch
            {
                return deflt;
            }
        }

        /// <summary>
        /// Get named PED (nullable)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pedName"></param>
        /// <returns></returns>
        public static T GetPEDnull<T>(string pedName, clsPlugExtraData ped)
                    where T : class
        {
            return GetPED<T>(pedName, ped, deflt: (T)null);
        }


        ///<summary>
        /// Returns serialized pluginExtraData from a device
        /// Low level function
        ///</summary>
        ///<param name="ped"></param>
        ///<param name="pedName"></param>
        ///<returns></returns>
        ///<remarks></remarks>
        public static object PedGet(clsPlugExtraData ped, string pedName)
        {
            if (ped == null) return null;

            object value = ped.GetNamed(pedName);

            if (value == null || !(value is byte[])) return value;

            byte[] byteObject = (byte[])value;

            if (!DeSerializeObject(ref byteObject, out object returnValue))
            {
                if (DeSerializingWarnings)
                {
                    string err = $"DeSerializing object: {pedName}";
                    Console.WriteLine(err);
                    Log(err, LogType.Warning);
                }
            }
            return returnValue;
        }


        ///<summary>
        ///Used to serialize an object to a bytestream, which can be stored in a device ("PDE" or "clsPlugExtraData"), Action or Trigger
        ///</summary>
        ///<param name="objIn">Input object</param>
        ///<param name="byteOut">Output bytes</param>
        ///<returns>True/False success</returns>
        ///<remarks>By HomeSeer</remarks>
        public static bool SerializeObject(object objIn, out byte[] byteOut)
        {
            byteOut = null;
            if (objIn == null) return false;

            var memStream = new MemoryStream();
            var formatter = new BinaryFormatter();

            try
            {
                formatter.Serialize(memStream, objIn);
                byteOut = new byte[memStream.Length - 1];
                byteOut = memStream.ToArray();
                return true;
            }
            catch (Exception ex)
            {
                string err = $"Serializing object {objIn} :\n {ex.Message}";
                Console.WriteLine(err);
                Log(err, LogType.Error);
                return false;
            }
        }

        ///<summary>
        ///Used to deserialze bytestream to an object, stored in a device ("PDE" or "clsPlugExtraData"), Action or Trigger
        ///</summary>
        ///<param name="byteIn">Input bytes</param>
        ///<param name="objOut">Output object</param>
        ///<returns>True/False success</returns>
        ///<remarks>By HomeSeer</remarks>
        public static bool DeSerializeObject(ref byte[] byteIn, out object objOut)
        {
            objOut = null;

            //If input and/or output is nothing then it failed (we need some objects to work with), so return False
            if (byteIn == null || byteIn.Length == 0) return false;

            try
            {
                MemoryStream memStream = new MemoryStream(byteIn);
                BinaryFormatter formatter = new BinaryFormatter();
                //formatter.Binder = new Version1ToVersion2DeserializationBinder();

                objOut = formatter.Deserialize(memStream);
                return (objOut == null) ? false : true;
            }
            catch (InvalidCastException exIC)
            {
                string err = "DeSerializing object - Invalid cast exception: " + exIC.Message;
                Console.WriteLine(err);
                Log(err, LogType.Error);
                return false;
            }
            catch (Exception ex)
            {
                if (DeSerializingWarnings)
                {
                    string err = "DeSerializing object: " + ex.Message;
                    Console.WriteLine(err);
                    Log(err, LogType.Warning);
                }
                return false;
            }
        }

        /// <summary>
        /// Get dictionary of all named peds,
        /// </summary>
        /// <param name="ped">Device PED</param>
        /// <param name="part">Part of the PED name to filter peds</param>
        /// <returns></returns>
        public static Dictionary<string, object> GetAllPEDs(clsPlugExtraData ped, string part = null)
        {
            var peds = new Dictionary<string, object>();

            if (ped != null && ped.GetNamedKeys() != null)
            {
                foreach (string name in ped.GetNamedKeys())
                {
                    if(part==null || name.Contains(part.ToLower()))
                        peds[name] = PedGet(ped, name);
                }
            }

            return peds;
        }

        /// <summary>
        /// Convert GetAllPEDs to string
        /// </summary>
        /// <param name="ped"></param>
        /// <returns></returns>
        public static string GetAllPEDsStr(clsPlugExtraData ped)
        {
            string lst = "";
            var peds = GetAllPEDs(ped);
            foreach (string name in peds.Keys)
            {
                string val = (peds[name] != null) ? peds[name].ToString() : "";
                lst += $"{name}: {val}\n";
            }
            return lst;
        }


        #endregion SerializeObject


        #region Device Lists

        ///<summary>
        ///List of all devices in HomeSeer. Used to enable Linq queries on devices.
        ///</summary>
        ///<returns>Generic.List() of all devices</returns>
        ///<remarks>By Moskus</remarks>
        public static DeviceDictHS Devices()
        {
            DeviceDictHS ret = new DeviceDictHS();
            Scheduler.Classes.clsDeviceEnumeration deviceEnumeration = (clsDeviceEnumeration)Hs.GetDeviceEnumerator();
            while (!deviceEnumeration.Finished)
            {
                HSDevice device = deviceEnumeration.GetNext();
                ret[device.get_Ref(null)] = device;
            }
            return ret;
        }

        /// <summary>
        /// 1. Get list of devices or properties (i.e. Locations) - based on combobox "name"
        /// </summary>
        /// <param name="name">Selection for defining what to get</param>
        /// <param name="devices">Full DeviceDictHS, can be "null", then will retrieve inside</param>
        /// <param name="exclude">List<dev_ids> to exclude</param>
        /// <returns>List of pairs (Name=Value=item) for dropbox</returns>
        // TEMP - TODO: change 'name' to Enum
        public static MyPairList GetDevicesProps(string name, DeviceDictHS devices = null, DeviceIdList exclude = null, string Location = null, string Location2 = null)
        {
            IHSApplication _hs = null;
            //IHSApplication _hs = Hs;

            if (devices == null)
                devices = Devices();

            switch (name)
            {
                //case "StateDeviceId":
                default:
                    return GetDevices(Location, Location2, devices, min_vspsCount: 1, exclude: exclude);

                case "DropListLocation":
                    Func<HSDevice, Int32, string> func1 = (x, i) => x.get_Location(_hs);
                    return GetDevicesProps(func1, devices, exclude: exclude);

                case "DropListLocation2":
                    Func<HSDevice, Int32, string> func2 = (x, i) => x.get_Location2(_hs);
                    return GetDevicesProps(func2, devices, exclude: exclude);
            }
            //return null;
        }


        /// <summary>
        /// 2. Helper for above GetDevices - selects devices filtered by Location and/or Location2
        /// </summary>
        /// <param name="Location"></param>
        /// <param name="Location2"></param>
        /// <param name="devices">Full DeviceDictHS, can be "null", then will retrieve inside</param>
        /// <param name="min_vspsCount">Filter devices if their vspsCount exceeds this number</param>
        /// <param name="exclude">List<dev_ids> to exclude</param>
        /// <returns>List of pairs (Name=name, Value=deviceID) for dropbox</returns>
        public static MyPairList GetDevices(string Location,
                                     string Location2,
                                     DeviceDictHS devices = null,
                                     int min_vspsCount = 0,
                                     DeviceIdList exclude = null)
        {
            IHSApplication _hs = null;
            //IHSApplication _hs = Hs;

            if (devices == null)
                devices = Devices();

            if (exclude == null)
                exclude = new DeviceIdList();

            MyPairList items = new MyPairList();

            foreach (int deviceId in devices.Keys)
            {
                if (exclude.Contains(deviceId))
                    continue;

                HSDevice device = devices[deviceId];
                string name = device.get_Name(_hs);
                string dev_loc = device.get_Location(_hs);
                string dev_loc2 = device.get_Location2(_hs);
                // Select devices matching Loc/Loc2, or if Loc/Loc2 not specified
                if ((String.IsNullOrEmpty(Location) || Location == dev_loc) &&
                    (String.IsNullOrEmpty(Location2) || Location2 == dev_loc2)
                    )
                {
                    // If Loc/Loc2 not specified - prepend them to device name
                    if (String.IsNullOrEmpty(Location))
                        name = "[" + dev_loc + "] " + name;
                    if (String.IsNullOrEmpty(Location2))
                        name = "[" + dev_loc2 + "] " + name;

                    int num_vsps = 0;
                    if (min_vspsCount > 0)
                    {
                        VSVGPairs.VSPair[] vsps = Hs.DeviceVSP_GetAllStatus(deviceId);
                        num_vsps = vsps.Length;
                    }

                    if (num_vsps >= min_vspsCount)
                    {
                        items.Add(new MyPair(name, deviceId));
                    }
                }
            }
            return items;
        }

        /// <summary>
        /// 2. Helper for above GetDevices, which takes selection function for extracting items from DeviceDictHS
        /// </summary>
        /// <param name="func"></param>
        /// <param name="devices">Full DeviceDictHS, can be "null", then will retrieve inside</param>
        /// <param name="exclude">List<dev_ids> to exclude</param>
        /// <returns>List of pairs (Name=Value=item) for dropbox</returns>
        public static MyPairList GetDevicesProps(Func<HSDevice, Int32, string> func, DeviceDictHS devices = null, DeviceIdList exclude = null)
        {
            if (devices == null)
                devices = Devices();

            if (exclude == null)
                exclude = new DeviceIdList();

            // TEMP - TODO: exclude?  && Int32.TryParse(item, out int id) && !exclude.Contains(id)

            IEnumerable<string> sel = devices.Values.Select(func);
            List<string> sel1 = sel.Distinct().OrderBy(x => x).ToList();

            MyPairList items = new MyPairList();
            foreach (var item in sel1)
            {
                if (item != "") // avoid duplicated ""
                {
                    items.Add(new MyPair(item, item));
                }
            }
            return items;
        }


        #endregion Device Lists


        #region Helpers

        /// <summary>
        /// Not Used
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
        {
            string folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath)) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }

        /// <summary>
        /// used to compare objects by just comparing public properties
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="to"></param>
        /// <param name="ignore"></param>
        /// <returns></returns>
        public static bool PublicInstancePropertiesEqual<T>(T self, T to, params string[] ignore) where T : class
        {
            if (self != null && to != null)
            {
                Type type = typeof(T);
                List<string> ignoreList = new List<string>(ignore);
                foreach (PropertyInfo pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!ignoreList.Contains(pi.Name))
                    {
                        object selfValue = type.GetProperty(pi.Name).GetValue(self, null);
                        object toValue = type.GetProperty(pi.Name).GetValue(to, null);

                        if (selfValue != toValue && (selfValue == null || !selfValue.Equals(toValue)))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return self == to;
        }

        /// <summary>
        /// Convert DateTime to string
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        public static string ToString(DateTime ts, string fmt = "dd MMM hh\\:mm\\:ss")
        {
            if (ts.Year <= 1970)
                return "";
            // If date==today - return time only
            if (ts.Date == DateTime.Today)
                return ts.TimeOfDay.ToString("hh\\:mm\\:ss");
            else
                return ts.ToString(fmt);
        }

        /// <summary>
        /// For checkboxes HS sends "checked"/"unchecked"
        /// For SliderTab HS sends "open"/"close"
        /// Convert these string to bool
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool? StrToBool(string value)
        {
            // For checkboxes HS sends "checked"/"unchecked"
            // For SliderTab HS sends "open"/"close"
            // But we store "bool"
            string[] true_strings = { "checked", "open", true.ToString() };
            string[] false_strings = { "unchecked", "close", false.ToString() };
            if (true_strings.Contains(value))
                return true;
            else if (false_strings.Contains(value))
                return false;

            return null;
        }

        public static string CheckBoolStr(string value)
        {
            return StrToBool(value).ToString();
        }

        /// <summary>
        /// COMMENT
        /// </summary>
        /// <param name="o"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TrySetProperty(object obj, string name, string value, bool ifChanged)
        {
            bool? bVal = StrToBool(value);

            // If ifChanged=true - check if saved value equals new value, return false (not changed)
            if (ifChanged &&
                TryGetProperty(obj, name, out object saved) &&
                saved != null &&
                (value == saved.ToString() || (bVal != null && bVal == (bool?)saved))
                )
                return false;

            var types = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var propertyInfo = obj.GetType().GetProperty(name, bindingAttr: types);
            if (propertyInfo != null)
            {
                // For checkboxes HS sends "checked"/"unchecked"
                if (bVal != null)
                {
                    value = bVal.ToString();
                }

                try
                {
                    // Convert from string to property value
                    object value1 = value;

                    // For empty string and int property set default to "0"
                    if (propertyInfo.PropertyType == typeof(int) && String.IsNullOrEmpty(value))
                    {
                        value1 = default(int);
                    }
                    else if (propertyInfo.PropertyType.IsEnum)
                    {
                        value1 = int.Parse(value);
                    }
                    else
                    {
                        // Check if the property type is nullable - then use UnderlyingType instead
                        Type type = propertyInfo.PropertyType;
                        Type type_null = Nullable.GetUnderlyingType(type);
                        if (type_null != null)
                            type = type_null;

                        // Convert from string to property value
                        value1 = Convert.ChangeType(value, type);
                    }

                    propertyInfo.SetValue(obj, value1, null);
                }
                catch (Exception ex)
                {
                    string err = $"Error TrySetProperty '{name}': '{value}'. {ex.Message}";
                    Console.WriteLine(err);
                    Log(err, LogType.Error);
                    return false;
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// If "obj" has the "name" property, set it
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns>true if "obj" has the "name" property</returns>
        public static bool TryGetProperty(object obj, string name, out object value)
        {
            var types = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var propertyInfo = obj.GetType().GetProperty(name, bindingAttr: types);
            if (propertyInfo != null)
            {
                value = propertyInfo.GetValue(obj, null);
                return true;
            }
            value = null;
            return false;
        }

        public static string FuncName([System.Runtime.CompilerServices.CallerMemberName] string CallerName = "")
        {
            return CallerName;
        }


        #endregion Helpers


        #region Events

        ///<summary>
        ///List of all events in HomeSeer. Used to enable Linq queries on events.
        ///</summary>
        ///<returns>Generic.List() of all EventData</returns>
        ///<remarks>By Moskus</remarks>
        public List<HomeSeerAPI.strEventData> Events()
        {
            var ret = new List<HomeSeerAPI.strEventData>();
            foreach (HomeSeerAPI.strEventData eventData in Hs.Event_Info_All())
            {
                ret.Add(eventData);
            }
            return ret;
        }

        ///<summary>
        ///List of all events in HomeSeer. Used to enable Linq queries on events.
        ///</summary>
        ///<returns>Generic.List() of all EventData</returns>
        ///<remarks>By Moskus</remarks>
        public List<HomeSeerAPI.strEventData> Events1()
        {
            var ret = new List<HomeSeerAPI.strEventData>();
            var allEvents = Hs.Event_Info_All().ToList();
            return allEvents;
        }

        #endregion Events

    }
}
