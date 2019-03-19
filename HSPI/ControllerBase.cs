using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using static HomeSeerAPI.CAPI;
using HomeSeerAPI;
using System.Collections.Specialized;
using System.Threading;

namespace HSPI_AKTemplate
{

    using HSDevice = Scheduler.Classes.DeviceClass;
    // Dict of HS devices
    using DeviceDictHS = Dictionary<int, Scheduler.Classes.DeviceClass>;
    // Dict of DeviceInfo devices
    using DeviceDict = Dictionary<int, DeviceBase>;

    public class ControllerBase
    {
        // Dict of ALL HS devices
        public DeviceDictHS devices = null;

        public HspiBase2 plugin = null;

        public IHSApplication HS { get; set; }

        /// <summary>
        /// Ctor
        /// </summary>
        public ControllerBase(HspiBase2 plugin)
        {
            this.plugin = plugin;
            // Keep local HS separate from plugin.HS
            // It will be set to null when connection with HS is lost
            // And reset to the new HS when connection is restored
            // But only after UpdateConfiguration is completed!
            HS = plugin.HS;

            UpdateDeviceList();
            CheckAndCreateDevices();
        }


        public void Log(string msg, bool error = false, DeviceBase dev = null)
        {
            if (dev != null)
            {
                dev.Log(msg, error);
            }
            else
            {
                Console.WriteLine(msg); // TEMP - TODO: only if console visible?
                Utils.Log(msg, error ? Utils.LogType.Error : Utils.LogType.Normal);
            }
        }


        #region Devices

        /// <summary>
        /// Virtual
        /// </summary>
        public virtual void CheckAndCreateDevices()
        {
        }

        /// <summary>
        /// Update devices and configuration
        /// </summary>
        public virtual void Update(bool InitIOinThread)
        {
        }

        /// <summary>
        /// Get fresh list of all devices in HS
        /// </summary>
        public void UpdateDeviceList()
        {
            this.devices = Utils.Devices();
        }

        /// <summary>
        /// Get HSDevice By Address
        /// </summary>
        /// <param name="addr">Device Address</param>
        /// <param name="created">Returns True if new device was created</param>
        /// <param name="create">Create if not found</param>
        /// <param name="name">Name used to create if not found</param>
        /// <returns></returns>
        public HSDevice GetHSDeviceByAddress(string addr, out bool created, bool create = false, string name = null)
        {
            created = false;

            int devID = HS.DeviceExistsAddress(addr, CaseSensitive: false);
            HSDevice device = GetHSDeviceByRef(devID);
            if(!create || device!=null)
                return device;

            if(name==null)
                name = addr;

            devID = HS.NewDeviceRef(name);
            //this.devices[devID] = (HSDevice)HS.GetDeviceByRef(devID);
            UpdateDeviceList();
            created = true;
            return GetHSDeviceByRef(devID);
        }

        /// <summary>
        /// Get HSDevice By Ref ID
        /// </summary>
        /// <param name="devID">Ref ID</param>
        /// <returns></returns>
        public HSDevice GetHSDeviceByRef(int devID)
        {
            if (!this.devices.ContainsKey(devID))
            {
                // Shouldn't get here as all HS devices should be in the list
                // if devID doesn't exist - i.e. deleted device?
                return null;
            }

            return this.devices[devID];
        }

        // Note: SetIOMulti() is called ONLY for devices owned by this plugin
        // To recive value update for device from another plugin need to  
        // RegisterEventCB(HSEvent.VALUE_CHANGE), then HSEvent() is called for EVERY device
        // Keep list of status device refs for RegisterEventCB(HSEvent.VALUE_CHANGE)
        // When HSEvent() is called for "VALUE_CHANGE" - for performace inspect only 
        // devices in this list as they are registered as "Linked Status Device"
        // This dictionary is setup in UpdateConfiguration
        public DeviceDict UsedDevices = new DeviceDict();

        /// <summary>
        /// Keep newly created devices only temporary during UpdateConfiguration
        /// </summary>
        public DeviceDict UsedDevicesTemp = null;

        /// <summary>
        /// Get device either from UsedDevices dict (if there), or constructed new from devID
        /// </summary>
        /// <param name="devID"></param>
        /// <param name="addToUsedDevices">Add to UsedDevices dict</param>
        /// <returns></returns>
        public DeviceBase GetDevice(int devID, bool addToUsedDevices = true)
        {
            if (devID <= 0)
                return null;

            // Keep newly created devices only temporary during UpdateConfiguration
            if(UsedDevicesTemp!=null && UsedDevicesTemp.ContainsKey(devID))
                return UsedDevicesTemp[devID];

            if (UsedDevices.ContainsKey(devID) && UsedDevices[devID] != null)
                return UsedDevices[devID];

            DeviceBase device = NewDevice(devID);

            if (device.deviceHS == null)
            {
                // Deleted device
                Log($"Device {devID} doesn't exist in the system", error: true);
            }
            else
            {
                if (addToUsedDevices)
                    UsedDevices[devID] = device;
                else if (UsedDevicesTemp != null)
                    UsedDevicesTemp[devID] = device;
            }
            return device;
        }

        /// <summary>
        /// Create new DeviceBase (or derived device) instance
        /// Note: NOT create HS device
        /// </summary>
        /// <param name="devID"></param>
        /// <returns></returns>
        protected virtual DeviceBase NewDevice(int devID)
        {
            return new DeviceBase(this, devID);
        }

        /// <summary>
        /// Check if device is used by this plugin for HSEvent() 
        /// Device is used - if it's in UsedDevices dict, see UpdateConfiguration
        /// </summary>
        /// <param name="devID"></param>
        /// <returns></returns>
        public bool DeviceUsed(int devID)
        {
            return UsedDevices.ContainsKey(devID);
        }

        public void AddDevice(DeviceBase dev)
        {
            if (dev != null && dev.RefId > 0)
            {
                dev.update_info();
                UsedDevices[dev.RefId] = dev;
            }
        }

        /// <summary>
        /// Set Device Value and DeviceString
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="value"></param>
        public void SetDeviceValue(int deviceId, double value)
        {
            DeviceBase device = GetDevice(deviceId);
            device.SetValue(value);
        }

        /// <summary>
        /// Called from HSPI.ConfigDevice
        /// Return HTML for the device config page
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public virtual string ConfigDevice(int deviceId)
        {
            StopwatchEx watch = new StopwatchEx("ConfigDevice");

            // TEMP - Update device list?
            UpdateDeviceList();

            DeviceBase dev = GetDevice(deviceId);
            return dev.GetDeviceConfig();
        }

        /// <summary>
        /// Called when a user posts information from your plugin tab on the device utility page
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="parts">The post data split into ID=value pairs</param>
        /// <returns></returns>
        public virtual Enums.ConfigDevicePostReturn ConfigDevicePost(int deviceId, NameValueCollection parts)
        {
            DeviceBase dev = GetDevice(deviceId);
            return dev.ConfigDevicePost(parts);
        }


        #endregion Devices


        #region ValueChanged

        /// <summary>
        /// This function is called from plugin SetIOMulti (for my devices) or HSEvent (other devices)
        /// </summary>
        /// <param name="cc">CAPIControl if Called from SetIOMulti</param>
        /// <param name="parameters">object[]  if Called from HSEvent</param>
        /// <returns></returns>
        public bool ValueChanged(CAPIControl cc = null, object[] parameters = null)
        {
            if (cc!=null)
            {
                // Called from SetIOMulti - devices from my plugin
                // Check cc.SingleRangeEntry

                // CAPI doesn't store the new devicevalue - until receives confirmation from the plugin
                // So here, we just update the value for the device
                // Note 1: also set deviceString = null to use default state string
                // Note 2: here must use SetDeviceValueByRef, not CAPIControl
                SetDeviceValue(cc.Ref, cc.ControlValue);

                // But don't need to process any further as HS will call HSEvent after that anyway
                if(DeviceUsed(cc.Ref))
                    return true;

                // Unless for some reason my device isn't registered for HSEvent (i.e. not in DeviceUsed)
                // So just in case have this check - so we don't miss the change, but normally should be in DeviceUsed
                return ValueChanged(cc.Ref, cc.ControlValue, null, "SetIOMulti");
            }
            else
            {
                // Called from HSEvent
                // For HSEvent.VALUE_CHANGE:
                // 0. eventType
                // 1. The device's address (string)
                // 2. The new value of the device (double)
                // 3. The old value of the device (double)
                // 4. The device's reference number (integer)
                return ValueChanged((int)parameters[4], (double)parameters[2], (double)parameters[3], "HSEvent");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="devID"></param>
        /// <param name="value"></param>
        /// <param name="oldval"></param>
        /// <param name="cause">Just for log</param>
        /// <returns></returns>
        public bool ValueChanged(int devID, double value, double? oldval, string cause)
        {
            Debug.Assert(UsedDevices.ContainsKey(devID));
            DeviceBase device = UsedDevices[devID];

            //For performance keep device state in ValueCached
            device.ValueCached = value;

            // Keep prev. value (only for log)
            if (oldval!=null)
                device.ValuePrev = (double)oldval;

            //device.Log($"ValueChanged: [{device}] ({cause})");

            // Notify observers
            device.NotifyValueChange(value, cause);

            return true;
        }

        #endregion ValueChanged

    }
}
