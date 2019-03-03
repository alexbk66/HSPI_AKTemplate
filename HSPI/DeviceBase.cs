using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;


using Scheduler;
using HomeSeerAPI;
using static HomeSeerAPI.VSVGPairs;
using static HomeSeerAPI.PlugExtraData;

namespace HSPI_AKTemplate
{
    using HSDevice = Scheduler.Classes.DeviceClass;


    /// <summary>
    /// Create minimal device if used only for reading device config(PED)
    /// 
    /// NOTE: DeviceInfo implements both IObservable and IObserver
    /// so each device can subscribe to another device - which is cool
    /// But it complicates things as device is now able to subscribe to itself
    /// And need to check using "Unsubscriber : IDisposable" - make sure it doesn't Dispose wrong device
    ///
    /// </summary>
    public partial class DeviceBase
    {
        // Underlying (wrapped) HS devise
        public HSDevice deviceHS { get; protected set; }

        // Only for getting StateDevice from UsedDevices, instead of creating a new one
        protected ControllerBase controller = null;


        // TEMP - settings?
        // Set to false, in case of error - set to true
        protected bool debug = false;


        #region Construction


        /// <summary>
        /// Constructor
        /// Pass either devID or deviceHS
        /// </summary>
        /// <param name="controller">Controller</param>
        /// <param name="devID">HSDevice</param>
        /// <param name="deviceHS">HSDevice</param>
        public DeviceBase(ControllerBase controller, int devID = 0)
        {
            FullUpdate = false;
            this.controller = controller;
            this.RefId = devID;

            //if (deviceHS==null)
            {
                deviceHS = controller.GetHSDeviceByRef(devID);
                if (deviceHS == null)
                {
                    // Deleted device. This also sets "Attension" field
                    LogErr("Device doesn't exist in the system");
                    return;
                }
            }

            this.deviceHS = deviceHS;
            this.RefId = deviceHS.get_Ref(null);
        }

        /// <summary>
        /// update device info
        /// </summary>
        /// <param name="force">COMMENT</param>
        public void update_info(bool force = true, bool reset_err = false)
        {
            // Store current setting and temporary set to true if 'force'
            bool fu = this.FullUpdate;
            FullUpdate = force;

            Name = deviceHS.get_Name(_hs_full_update);
            // Set device Type to "Virtual", only if it's not set yet and only for my plugin
            CheckType(deflt: "Virtual");

            // Don't get them here unnecessary, only when needed in vspsList()
            // Here just reset them - to force update when needed
            //_StateDevice = null;
            //triggerGroups = null;

            // restore prvious setting
            FullUpdate = fu;

            if(reset_err)
            {
                Attention = null; // Remove "Attention"
                Error = "";       // Reset Error
            }
        }

        public override string ToString()
        {
            return $"(Ref: {RefId}) {Name} ({DeviceString})";
        }

        public virtual void Create()
        {
            throw new NotImplementedException();
        }

        public void Create(string name, bool force)
        {
            string addr = "AK_" + name;

            deviceHS  = controller.GetHSDeviceByAddress(addr, out bool created, create: true, name: name);

            RefId = deviceHS.get_Ref(null);

            if(created)
            {
                DeviceTypeInfo_m.DeviceTypeInfo DT = new DeviceTypeInfo_m.DeviceTypeInfo();
                DT.Device_API = DeviceTypeInfo_m.DeviceTypeInfo.eDeviceAPI.Security;
                DT.Device_Type = (int)DeviceTypeInfo_m.DeviceTypeInfo.eDeviceAPI.Plug_In;
                deviceHS.set_DeviceType_Set(hs, DT);
                deviceHS.set_InterfaceInstance(hs, "");
                deviceHS.set_Status_Support(hs, false);//Set to True if the devices can be polled,  false if not
                deviceHS.MISC_Set(hs, Enums.dvMISC.SHOW_VALUES);
                deviceHS.MISC_Set(hs, Enums.dvMISC.NO_LOG);

                Interface = controller.plugin.Name;
                Type = "Virtual";
                Address = addr;

                //Location = "EnOcean";
                //Location2 = "EnOcean";
                //LastChange = DateTime.Now;
            }

            controller.UsedDevices[RefId] = this;

        }

        public VSPair AddVSPair(double Value, string Status, ePairControlUse ControlUse, string Graphic = null)
        {
            var svPair = new VSPair(ePairStatusControl.Both)
            {
                PairType = VSVGPairType.SingleValue,
                Value = Value,
                Status = Status,
                ControlUse = ControlUse,
                Render = Enums.CAPIControlType.Button,
                IncludeValues = true
            };

            bool ret = hs.DeviceVSP_AddPair(RefId, svPair);

            if (Graphic != null)
            {
                var vgPair = new VGPair();
                vgPair.PairType = VSVGPairType.SingleValue;
                vgPair.Set_Value = Value;
                vgPair.Graphic = Graphic;
                ret = hs.DeviceVGP_AddPair(RefId, vgPair);
            }
            return svPair;
        }


        #endregion Construction


        #region Properties


        /// <summary>
        /// If device is created from Controller.UpdateConfiguration - 
        /// we are iterating ALL devices - it's unefficient to go to HS for each set/get call
        /// Particulary slow is get_Interface call (don't know why)
        /// And it's unnecessary - UpdateConfiguration calls UpdateDeviceList() anyway
        /// So set IHSApplication for most calls to null.
        /// But for plugin.ConfigDevice() it's safer to go to HS for all set/get calls
        /// So set IHSApplication for most calls to Utils.Hs
        /// </summary>
        public bool FullUpdate { get; set; }


        /// <summary>
        /// See comment for FullUpdate
        /// </summary>
        protected IHSApplication _hs_full_update
        {
            get { return FullUpdate ? hs : null; }
        }

        /// <summary>
        /// Make static here - instead of Utils?
        /// </summary>
        protected IHSApplication hs
        {
            get
            {
                // Use controller.HS, not controller.plugin.HS
                // As it's reset to the new HS when connection is restored
                // But only after UpdateConfiguration is completed!
                return controller.HS;
            }
        }


        public int RefId { get; protected set; }

        public string Name { get; protected set; }

        public string FullName
        {
            get
            {
                return $"[{Location}] [{Location2}] {Name}";
            }
        }

        public string Address
        {
            get
            {
                if (deviceHS == null) return null;
                return deviceHS.get_Address(_hs_full_update);
            }
            set
            {
                if (value != Address)
                {
                    FullUpdate = true; // once write to HS - need to read from HS too
                    deviceHS.set_Address(hs, value);
                }
            }
        }

        public string Type
        {
            get
            {
                if (deviceHS == null) return null;
                return deviceHS.get_Device_Type_String(_hs_full_update);
            }
            set
            {
                if (value != Type)
                {
                    FullUpdate = true; // once write to HS - need to read from HS too
                    deviceHS.set_Device_Type_String(hs, value);
                }
            }
        }

        public string LastChange
        {
            get
            {
                if (deviceHS == null) return null;
                DateTime ts = this.deviceHS.get_Last_Change(hs);
                return Utils.ToString(ts);
            }

            //protected set
            //{
            //    this.deviceHS.set_Last_Change(hs, value);
            //}
        }

        public string Location
        {
            get
            {
                if (deviceHS == null) return null;
                return deviceHS.get_Location(_hs_full_update);
            }
            set
            {
                if (value != Location)
                {
                    FullUpdate = true; // once write to HS - need to read from HS too
                    deviceHS.set_Location(hs, value);
                }
            }
        }

        public string Location2
        {
            get
            {
                if (deviceHS == null) return null;
                return deviceHS.get_Location2(_hs_full_update);
            }
            set
            {
                if (value != Location2)
                {
                    FullUpdate = true; // once write to HS - need to read from HS too
                    deviceHS.set_Location2(hs, value);
                }
            }
        }

        public string Interface
        {
            get
            {
                if (deviceHS == null) return null;
                return deviceHS.get_Interface(_hs_full_update);
            }
            protected set
            {
                if (value != Interface)
                {
                    FullUpdate = true; // once write to HS - need to read from HS too
                    deviceHS.set_Interface(hs, value);
                }
            }
        }

        /// <summary>
        /// Set device status image to exclamation in case of error
        /// </summary>
        public string Attention
        {
            get
            {
                if (deviceHS == null) return null;
                return deviceHS.get_Attention(_hs_full_update);
            }
            set
            {
                if(deviceHS!=null)
                    deviceHS.set_Attention(hs, value);
            }
        }


        public bool power_fail_recovery
        {
            get
            {
                if (deviceHS == null) return false;
                return deviceHS.MISC_Check(hs, Enums.dvMISC.INCLUDE_POWERFAIL);
            }
            set
            {
                if (value)
                {
                    //deviceHS.MISC_Set(_hs_full_update, Enums.dvMISC.INCLUDE_POWERFAIL);
                    deviceHS.MISC_Set(hs, Enums.dvMISC.INCLUDE_POWERFAIL);
                }
                else
                {
                    //deviceHS.MISC_Clear(_hs_full_update, Enums.dvMISC.INCLUDE_POWERFAIL);
                    deviceHS.MISC_Clear(hs, Enums.dvMISC.INCLUDE_POWERFAIL);
                }
            }
        }

        /// <summary>
        /// Set/get device string
        /// </summary>
        public string DeviceString
        {
            set
            {
                //if(value!=null)
                _deviceString = value;
                hs.SetDeviceString(RefId, _deviceString, reset: true);
            }

            get
            {
                string str = hs.DeviceString(RefId);
                // If hs.DeviceString failed - see if we have previous value
                if (String.IsNullOrEmpty(str))
                {
                    str = _deviceString;
                }
                // If hs.DeviceString failed - try to use CAPI
                if (String.IsNullOrEmpty(str))
                {
                    CAPI.ICAPIStatus st = hs.CAPIGetStatus(RefId);
                    str = st.Status;
                }
                // If CAPI failed too - just return 'Value'
                if (String.IsNullOrEmpty(str))
                    str = Value.ToString();
                return str;
            }
        }

        string _deviceString;

        /// <summary>
        /// For setting Value - use CAPIControlHandler or SetDeviceValueByRef
        /// </summary>
        public enum ControlType
        {
            CAPI = 0,     // Use CAPIControlHandler - set to 0 to make it default value
            Both = 1,     // Use both
            SetByRef = 2, // Use SetDeviceValueByRef
        }

        /// <summary>
        /// Set device Value
        /// Note: If changing Value by plugin - should use CAPIControl
        ///       If called from SetIOMulti - can't use CAPIControl, must use SetDeviceValueByRef
        ///       So controller set this to true, and here we reset it back to false
        /// Note: if for some devices (probably othe plugin bug) CAPIControl updates device,
        ///       but not HS UI - can use both SetDeviceValueByRef and CAPIControl 
        ///       (probably never happens and can be removed in the future)
        /// </summary>
        public bool forceSetDeviceValueByRef = false;

        public double Value
        {
            get
            {
                if (deviceHS == null) return 0;
                ValueCached = this.deviceHS.get_devValue(hs);
                return ValueCached;
            }

            set
            {
                try
                {
                    ControlType type = ControlType.CAPI;

                    // If called from SetIOMulti - can't use CAPIControl, must use SetDeviceValueByRef
                    if (forceSetDeviceValueByRef)
                    {
                        type = ControlType.SetByRef;
                        forceSetDeviceValueByRef = false;
                        // Set DeviceString to null (to use default state string) only if state changes
                        if (ValueCached != value)
                            hs.SetDeviceString(RefId, null, reset: true); // Can't use DeviceString = null;
                    }
                    else
                    {
                        if (ValueCached == value)
                            return;
                    }

                    if (type == ControlType.Both || type == ControlType.SetByRef)
                    {
                        hs.SetDeviceValueByRef(this.RefId, value, trigger: true);
                    }

                    if (type == ControlType.Both || type == ControlType.CAPI)
                    {
                        CAPI.CAPIControl ctrl = this.statusPairsDict.GetCAPIControl(value);
                        CAPI.CAPIControlResponse ret = hs.CAPIControlHandler(ctrl);
                    }

                    Log($"SetValue: '{Name}' = '{value}' ({type})");
                    ValueCached = value;
                }
                catch (Exception ex)
                {
                    // Store the state value in case connection is lost
                    val_saved = value;
                    LogErr($"Error setting device value : '{value}'. {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Store the state value in case connection is lost
        /// And restore after
        /// </summary>
        protected double? val_saved = null;

        /// <summary>
        /// For performance keep device state in ValueCached
        /// </summary>
        public double ValueCached
        {
            get => _valueCached;
            set
            {
                // Keep prev. value (only for log)
                ValuePrev = _valueCached;
                _valueCached = value;
            }
        }
        protected double _valueCached = -1;

        /// <summary>
        /// Keep prev. value (only for log)
        /// </summary>
        public double ValuePrev { set; get; }

        /// <summary>
        /// If something wrong happens keep the error string
        /// </summary>
        protected string _error;
        public string Error
        {
            get => _error;

            set
            {
                _error = value;
                Attention = _error;
            }
        }


        public bool log_enable
        {
            get
            {
                return GetPED<bool>(Utils.FuncName());
            }
            set
            {
                SavePED(Utils.FuncName(), value);
            }
        }

        #endregion Properties


        #region PED Methods

        public clsPlugExtraData ped
        {
            set
            {
                if (this.deviceHS != null)
                    this.deviceHS.set_PlugExtraData_Set(null, value);
            }
            get
            {
                if (deviceHS == null) return null;
                return this.deviceHS.get_PlugExtraData_Get(null);
            }
        }


        /// <summary>
        /// Save PED to HS
        /// </summary>
        public void UpdatePED()
        {
            if (this.deviceHS != null)
                this.deviceHS.set_PlugExtraData_Set(hs, this.ped);
            hs.SaveEventsDevices();
        }


        public static readonly string ped_name_prefix = "ak_";

        /// <summary>
        /// Save named PED
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pedName"></param>
        /// <param name="Value"></param>
        /// <param name="deflt">Only for Comparer.GetPED</param>
        /// <returns>true if value changed</returns>
        public bool SavePED<T>(string pedName, T Value, T deflt = default(T), bool force = false)
        {
            pedName = CheckPedName(pedName);

            // TEMP
            Log($"SavePED: '{pedName}' = '{Value}'");

            T val_saved = GetPED(pedName, deflt);

            if (!force && 0 == Comparer<T>.Default.Compare(Value, val_saved))
                return false;

            this.ped = Utils.PedAdd(this.ped, pedName, Value);

            // Force update PED (set_PlugExtraData_Set)
            UpdatePED();

            return true;
        }

        /// <summary>
        /// Get named PED
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pedName"></param>
        /// <param name="deflt"></param>
        /// <returns></returns>
        public T GetPED<T>(string pedName, T deflt = default(T))
        {
            return Utils.GetPED<T>(CheckPedName(pedName), this.ped, deflt);
        }


        /// <summary>
        ///  Get named PED (nullable)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pedName"></param>
        /// <returns></returns>
        public T GetPEDnull<T>(string pedName)
                    where T : class
        {
            return Utils.GetPEDnull<T>(CheckPedName(pedName), this.ped);
        }

        /// <summary>
        /// Remove named PED
        /// </summary>
        /// <param name="name"></param>
        public bool DeletePED(string pedName)
        {
            bool ret = ped.RemoveNamed(CheckPedName(pedName));
            UpdatePED();
            return ret;
        }

        public static string CheckPedName(string name)
        {
            if (!name.StartsWith(ped_name_prefix))
                name = ped_name_prefix + name;
            return name;
        }

        public Dictionary<string, object> GetAllPEDs(string part = null)
        {
            return Utils.GetAllPEDs(this.ped, part);
        }

        #endregion PED Methods



        #region VSPairs


        public StatusPairsDict statusPairsDict
        {
            get
            {
                if (_statusPairsDict == null)
                    _statusPairsDict = new StatusPairsDict(hs, RefId);

                return _statusPairsDict;
            }
        }


        protected StatusPairsDict _statusPairsDict = null;

        /// <summary>
        /// Get all VSPairs, for performance keep them in dict with the key Status/Control/Both
        /// Convert VSVGPairs.VSPair[] to simple PairList
        /// Note: ranges *split* into individual states
        /// </summary>
        /// <param name="StatusControl">ePairStatusControl - Status, Control, or Both</param>
        /// <param name="splitrange">ranges *split* into individual states</param>
        /// <returns></returns>
        public MyPairList vspsList(ePairStatusControl StatusControl, SplitRangeType splitrange)
        {
            return statusPairsDict.vspsList(StatusControl, splitrange);
        }

        /// <summary>
        /// Convert VSPair list to string for display
        /// Note: ranges *NOT split* into individual states
        /// </summary>
        /// <param name="StatusControl">Append state value to name</param>
        /// <returns></returns>
        public string vspsListStr(ePairStatusControl StatusControl)
        {
            //Note: ranges *NOT split* into individual states
            MyPairList pairs = vspsList(StatusControl, SplitRangeType.NotSplit);
            List<string> states = new List<string>();
            string currentValStr = this.Value.ToString();

            foreach (MyPair pair in pairs)
            {
                if (!states.Contains(pair.Name))
                {
                    if (currentValStr == pair.ValueStr)
                        states.Add($"<b>{pair.Name}</b>");
                    else
                        states.Add(pair.Name);
                }
            }

            return String.Join(", ", states);
        }


        /// <summary>
        /// Find VSPair matching MyPair min-max range
        /// </summary>
        /// <param name="myPair"></param>
        /// <returns></returns>
        public VSPair FindVSPair(MyPair myPair)
        {
            return statusPairsDict.FindVSPair(myPair);
        }

        #endregion VSPairs


        #region Methods

        /// <summary>
        /// Log
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="error"></param>
        public void Log(string msg, bool error = false, bool warning = false)
        {
            if (log_enable || error)
            {
                msg = $"[{RefId}]: {msg}";
                Console.WriteLine(msg); // TEMP - TODO: only if console visible?
                Utils.LogType type = error ? Utils.LogType.Error : warning ? Utils.LogType.Warning : Utils.LogType.Normal;
                Utils.Log(msg, type);

                if (error)
                {
                    this.log_enable = true;
                    this.debug = true;
                }
            }
        }

        public void LogErr(string msg)
        {
            // If something wrong happens keep the error string
            Error = msg;
            Log(msg, error: true);
        }

        public void LogDeb(string msg)
        {
            if (this.debug)
                Log("  *** " + msg);
        }



        /// <summary>
        /// Virtual
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cause"></param>
        public virtual void NotifyValueChange(double value, string cause)
        {
        }



        /// <summary>
        /// If connection is lost - val_saved will have last "Value" (if it was set)
        /// Set it after restoring connection
        /// </summary>
        public void CheckSavedValue()
        {
            if (val_saved != null)
            {
                Value = (double)val_saved;
                val_saved = null;
            }
        }


        /// <summary>
        /// Set device Type to "Virtual", only if it's not set yet and only for my plugin
        /// </summary>
        /// <param name="deflt"></param>
        public void CheckType(string deflt = "Virtual")
        {
            string iface = Interface;
            if (String.IsNullOrEmpty(Type) && (String.IsNullOrEmpty(iface) || iface == controller.plugin.Name))
                Type = deflt;
        }

        /// <summary>
        /// Set/remove this device Interface to this plugin
        /// </summary>
        /// <param name="thisplugin"></param>
        public void SetInterface(bool thisplugin)
        {
            string used = null;
            string addremove = null;

            // Set this device Interface to this plugin
            if (this.Interface == "" && thisplugin)
            {
                used = "IS";
                addremove = "SETTING";
                this.Interface = controller.plugin.Name;
            }
            // Remove this device Interface, since it's not used anymore
            if (this.Interface == controller.plugin.Name && !thisplugin)
            {
                used = "NOT";
                addremove = "REMOVING";
                this.Interface = "";
            }

            if (addremove != null)
            {
                Log($"Device '{Name}' {used} used by '{controller.plugin.Name}' - {addremove} interface");
            }
        }



        #endregion Methods


        #region URL Methods

        /// <summary>
        /// Create link to config page for this device
        /// </summary>
        /// <param name="show_id">Display device id, not Name</param>
        /// <returns></returns>
        public string GetURL(bool show_id = false)
        {
            // Display device PEDs in tooltip (title)
            string title = $"{RefId}: {FullName}\n";
            Dictionary<string, object> peds = this.GetAllPEDs();
            foreach (string name in peds.Keys)
            {
                string val = (peds[name] != null) ? peds[name].ToString() : "";
                title += $"{name}: {val}\n";
            }

            string display = show_id ? $"{RefId}" : Name;
            string err = !String.IsNullOrEmpty(Error) ? PageBuilder.MyToolTip(Error, error: true) : "";
            return GetURL(RefId, title, err, display);
        }

        /// <summary>
        /// Create link to config page for this device
        /// </summary>
        /// <param name="RefId"></param>
        /// <param name="title">Tooltip</param>
        /// <param name="err"></param>
        /// <param name="display"></param>
        /// <returns></returns>
        public static string GetURL(int RefId, string title, string err, string display)
        {
            return $"<a target='_self' class='device_management_name' title='{title}'" +
                            $" href='deviceutility?ref={RefId}&edit=1'>{err}{display}</a>";
        }


        #endregion URL Methods
    }
}