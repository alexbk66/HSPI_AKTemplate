using System;
using System.Web;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

using HomeSeerAPI;
using Scheduler;
using HSPI_AKTemplate;


namespace HSPI_AKExample
{
    using HSDevice = Scheduler.Classes.DeviceClass;
    using DeviceDictHS = Dictionary<int, Scheduler.Classes.DeviceClass>;

    public class HSPI : HspiBase2
    {
        #region Variables

        /// <summary> Definition of the configuration web page </summary>
        //private HSPI_Config configPage;
        private StatusPage statusPage;



        public Settings settings { get; protected set; }

        #endregion Variables

        #region Construction Etc.

        public void SaveSettings()
        {
            settings.Save();
        }

        protected override string GetName()
        {
            return "AK Smart Device";
        }


        /// <summary>
        /// Ctor
        /// </summary>
        public HSPI()
        {
            controller = null;
            //settings = new Settings(IniFile, HS);

            Utils.PluginName = GetName();
        }

        /// <summary>
        /// Initialize the plugin and associated hardware/software, start any threads
        /// </summary>
        /// <param name="port">The COM port for the plugin if required.</param>
        /// <returns>Warning message or empty for success.</returns>
        public override string InitIO(string port)
        {
            // Connect() should call it, but doesn't hurt to call again
            Connected(true);

            // First time create Controller
            if (controller == null)
                controller = new Controller(this);

            // After reconnect - just update, don't recreate
            controller.Update(settings.InitIOinThread);

            // Create and register the web pages

            statusPage = new StatusPage(this);

            //configPage = new HSPI_Config(this);

            // Register help page
            RegisterWebPageDesc(Name + " Help", "AKSmartDevice/AKSmartDeviceVer1.htm", "Help1", helplink: true);
            RegisterWebPageDesc("Help", "/AKSmartDevice/AKSmartDeviceVer1.htm", "Help2", helplink: false);
            RegisterWebPageDesc("Youtube Video", "http://www.youtube.com/watch?v=mmyrx8kXnWY", "Help2");

            // Note: SetIOMulti() is called ONLY for devices owned by this plugin
            // To recive value update for device from another plugin need to
            // RegisterEventCB(HSEvent.VALUE_CHANGE), then HSEvent() is called for EVERY device
            Callback.RegisterEventCB(Enums.HSEvent.VALUE_CHANGE, GetName(), InstanceFriendlyName());
            Callback.RegisterEventCB(Enums.HSEvent.CONFIG_CHANGE, GetName(), InstanceFriendlyName());

            //AppDomain currentDomain = AppDomain.CurrentDomain;
            //currentDomain.AssemblyResolve += new ResolveEventHandler(Utils.LoadFromSameFolder);

            return base.InitIO(null);
        }


        /// <summary>
        /// When HSPIBase.Connect succseeds it calls this virtual function to inform plugin
        /// </summary>
        protected override void Connected(bool isconnected)
        {
            Console.WriteLine($"HSPI Connected({isconnected})");

            if (isconnected)
            {
                // Not sure if this exactly clean?
                Utils.Hs = HS;

                settings = new Settings(IniFile, HS);

                //controller.CheckSavedValues();
            }
            else
            {
                Utils.Hs = null;
                if(controller!=null)
                    controller.HS = null;
            }
        }

        /// <summary>
        ///     Called when HomeSeer is not longer using the plugin.
        ///     This call will be made if a user disables a plugin from the interfaces configuration page
        ///     and when HomeSeer is shut down.
        /// </summary>
        public override void ShutdownIO()
        {
            // let our console wrapper know we are finished
            base.ShutdownIO();
        }

        #endregion Construction

        #region Capabilities and Status

        public override IPlugInAPI.strInterfaceStatus InterfaceStatus()
        {
            return new IPlugInAPI.strInterfaceStatus
            {
                intStatus = IPlugInAPI.enumInterfaceStatus.OK
            };
        }

        /// <summary>
        /// Plugin licensing mode: 
        /// 1 = plugin is not licensed, 
        /// 2 = plugin is licensed and user must purchase a license but there is a 30-day trial.
        /// </summary>
        /// <returns>System.Int32</returns>
        public override int AccessLevel()
        {
            #if DEBUG
            return 1;
            #else
            return 2;
            #endif
        }

 
        /// <summary>
        /// Indicate if the plugin supports the ability to add devices through the Add Device link on the device utility page.
        /// If <c>true</c>, a tab appears on the add device page that allows the user to configure specific options for the new device.
        /// </summary>
        public override bool SupportsAddDevice()
        {
            return true;
        }

        /// <summary>
        /// Indicate if the plugin allows for configuration of the devices via the device utility page.
        /// This will allow you to generate some HTML controls that will be displayed to the user for modifying the device.
        /// </summary>
        public override bool SupportsConfigDevice()
        {
            return true;
        }

        /// <summary> Indicate if the plugin manages all devices in the system. </summary>
        public override bool SupportsConfigDeviceAll()
        {
            return true;
        }

        /// <summary>
        /// Indicate if the plugin supports multiple instances.  
        /// The plugin may be launched multiple times and will be passed a unique instance name as a command line parameter to the Main function.
        /// </summary>
        public override bool SupportsMultipleInstances()
        {
            return false;
        }

        /// <summary> Indicate if plugin supports multiple instances using a single executable.</summary>
        public override bool SupportsMultipleInstancesSingleEXE()
        {
            return false;
        }

#endregion Capabilities and Status

        #region Devices

        /// <summary>
        /// When you wish to have HomeSeer call back in to your plug-in or application when certain events happen in the system,
        /// call the RegisterEventCB procedure and provide it with event you wish to monitor. 
        /// See RegisterEventCB for more information and an example and event types.
        /// </summary>
        public override void HSEvent(Enums.HSEvent eventType, object[] parameters)
        {
            if (eventType == Enums.HSEvent.VALUE_CHANGE)
            { 
                // For HSEvent.VALUE_CHANGE:
                // 0. eventType
                // 1. The device's address (string)
                // 2. The new value of the device (double)
                // 3. The old value of the device (double)
                // 4. The device's reference number (integer)

                // for performace inspect only devices in MonitoredDevices list
                // as they are registered as "Linked Status Device"
                int devID = (int)parameters[4];
                if (controller.DeviceUsed(devID))
                {
                    controller.ValueChanged(parameters: parameters);
                }
            }
            if (eventType == Enums.HSEvent.CONFIG_CHANGE)
            {
                // For HSEvent.CONFIG_CHANGE:
                // 0. eventType
                // 1. Type: 0=a device was changed 1=an event was changed 2=an event group was changed
                // 2. deprecated
                // 3. The device/event/event group reference number. 0 means that the reference is not known.
                // 4. DAC device/event was 0 = not known, 1 = Added, 2 = Deleted, 3 = Changed
                // 5. A string describing what changed
                // So if a used device was deleted:
                if((int)parameters[1]==0 && (int)parameters[4]==2)
                {
                    int devID = (int)parameters[3];
                    if (controller.DeviceUsed(devID))
                    {
                        (controller as Controller).DeviceDeleted(devID);
                    }
                }
            }
        }

        /// <summary>
        /// SetIOMulti is called by HomeSeer when a device that your plugin owns is controlled.  
        /// Your plugin owns a device when it's INTERFACE property is set to the name of your plugin.
        /// </summary>
        /// <param name="colSend">
        /// This is a collection of CAPIControl objects, one object for each device that needs to be controlled. 
        /// Look at the ControlValue property to get the value that device needs to be set to.
        /// </param>
        public override void SetIOMulti(List<CAPI.CAPIControl> colSend)
        {
            //Multiple CAPIcontrols might be sent at the same time, so we need to check each one
            foreach (CAPI.CAPIControl cc in colSend)
            {
                // This is called for devices registered for this plugin
                // before the value is actually modified inside HS - it's more like a change request
                // So for confirmation must set HS.SetDeviceValueByRef()
                // But don't need to process any further as HS will call HSEvent after that anyway
                controller.ValueChanged(cc);
            }
        }



        /// <summary>
        /// Sets the device value.
        /// </summary>
        /// <param name="refId">The device reference identifier.</param>
        /// <param name="value">The value/status of the device.</param>
        /// <param name="trigger">if set to <c>true</c> process triggers normally, otherwise only change the value.</param>
        //public override void SetDeviceValue(int RefId, double value, bool trigger = true)
        //{
        //    HS.SetDeviceValueByRef(RefId, value, trigger);
        //}


        #endregion Devices

        #region Action

        /// <summary>
        /// This function is called from the HomeSeer event page when an event is in edit mode
        /// Your plugin needs to return HTML controls so the user can make action selections.
        /// Normally this is one of the HomeSeer jquery controls such as a clsJquery.jqueryCheckbox.
        /// </summary>
        /// <param name="sUnique">A unique string that can be used with your HTML controls to identify the control. All controls need to have a unique ID.</param>
        /// <param name="ActInfo">Object that contains information about the action like current selections.</param>
        /// <returns>HTML controls that need to be displayed so the user can select the action parameters.</returns>
        public override string ActionBuildUI(string uniqueControlId, IPlugInAPI.strTrigActInfo actionInfo)
        {
            return "";
        }

        /// <summary>
        /// Return TRUE if the given action is configured properly.
        /// There may be times when a user can select invalid selections for the action and in this case you would return FALSE so HomeSeer will not allow the action to be saved.
        /// </summary>
        /// <param name="ActInfo">Object describing the action.</param>
        public override bool ActionConfigured(IPlugInAPI.strTrigActInfo actionInfo)
        {
            return true;
        }

        /// <summary> The number of actions the plugin supports. </summary>
        public override int ActionCount()
        {
            return 0;
        }

        /// <summary> Indicate if the given devices is referenced by the given action. </summary>
        public override string ActionFormatUI(IPlugInAPI.strTrigActInfo actionInfo)
        {
            return "ActionFormatUI";
        }

        /// <summary>
        /// When a user edits your event actions in the HomeSeer events, this function is called to process the selections.
        /// </summary>
        /// <param name="PostData">A collection of name value pairs that include the user's selections.</param>
        /// <param name="TrigInfoIN">Object that contains information about the action.</param>
        /// <returns>Object that holds the parsed information for the action. HomeSeer will save this information for you in the database.</returns>
        public override IPlugInAPI.strMultiReturn ActionProcessPostUI(NameValueCollection postData,
            IPlugInAPI.strTrigActInfo actionInfo)
        {
            return new IPlugInAPI.strMultiReturn();
        }

        /// <summary> Indicate if the given devices is referenced by the given action. </summary>
        public override bool ActionReferencesDevice(IPlugInAPI.strTrigActInfo actionInfo, int deviceId)
        {
            return false;
        }

        /// <summary>
        /// Return the name of the action given an action number. The name of the action will be displayed in the HomeSeer events actions list.
        /// </summary>
        /// <param name="ActionNumber">The number of the action. Each action is numbered, starting at 1.</param>
        /// <returns>Name of the action.</returns>
        public override string get_ActionName(int actionNumber)
        {
            return "";
        }

        /// <summary>
        /// When an event is triggered, this function is called to carry out the selected action. 
        /// Use the ActInfo parameter to determine what action needs to be executed then execute this action.
        /// </summary>
        public override bool HandleAction(IPlugInAPI.strTrigActInfo actionInfo)
        {
            return false;
        }

        #endregion Action

        #region Condition

        /// <summary>
        /// Set to <c>true</c> if the trigger is being used as a CONDITION.  
        /// Check this value in BuildUI and other procedures to change how the trigger is rendered if it is being used as a condition or a trigger.
        /// </summary>
        public override bool get_Condition(IPlugInAPI.strTrigActInfo actionInfo)
        {
            return false;
        }

        public override void set_Condition(IPlugInAPI.strTrigActInfo actionInfo, bool value)
        {
        }

        /// <summary> Indicate if the given trigger can also be used as a condition for the given grigger number. </summary>
        public override bool get_HasConditions(int triggerNumber)
        {
            return false;
        }

        #endregion Condition

        #region Trigger

        /// <summary> Indicate if the plugin has any triggers. </summary>
        protected override bool GetHasTriggers()
        {
            return false;
        }

        /// <summary> Number of triggers the plugin supports. </summary>
        protected override int GetTriggerCount()
        {
            return 0;
        }

        public override string TriggerBuildUI(string uniqueControlId, IPlugInAPI.strTrigActInfo triggerInfo)
        {
            return "";
        }

        /// <summary> Return the HTML controls for a given trigger. </summary>
        public override string TriggerFormatUI(IPlugInAPI.strTrigActInfo actionInfo)
        {
            return "";
        }

        /// <summary>
        /// Process a post from the events web page when a user modifies any of the controls related to a plugin trigger. 
        /// After processing the user selctions, create and return a strMultiReturn object.
        /// </summary>
        public override IPlugInAPI.strMultiReturn TriggerProcessPostUI(NameValueCollection postData,
            IPlugInAPI.strTrigActInfo actionInfo)
        {
            return new IPlugInAPI.strMultiReturn();
        }

        /// <summary> Indicate if the given device is referenced by the given trigger. </summary>
        public override bool TriggerReferencesDevice(IPlugInAPI.strTrigActInfo actionInfo, int deviceId)
        {
            return false;
        }

        /// <summary>
        /// Although this appears as a function that would be called to determine if a trigger is true or not, it is not.  
        /// Triggers notify HomeSeer of trigger states using TriggerFire , but Triggers can also be conditions, and that is where this is used.  
        /// If this function is called, TrigInfo will contain the trigger information pertaining to a trigger used as a condition.  
        /// When a user's event is triggered and it has conditions, the conditions need to be evaluated immediately, 
        /// so there is not regularity with which this function may be called in your plugin.  
        /// It may be called as often as once per second or as infrequently as once in a blue moon.
        /// </summary>
        public override bool TriggerTrue(IPlugInAPI.strTrigActInfo actionInfo)
        {
            return false;
        }

        /// <summary> Return the number of sub triggers your plugin supports. </summary>
        public override int get_SubTriggerCount(int triggerNumber)
        {
            return 0;
        }

        /// <summary> Return the text name of the sub trigger given its trugger number and sub trigger number. </summary>
        public override string get_SubTriggerName(int triggerNumber, int subTriggerNumber)
        {
            return "";
        }

        public override bool get_TriggerConfigured(IPlugInAPI.strTrigActInfo actionInfo)
        {
            return true;
        }

        /// <summary> Indicate if the given trigger is configured properly. </summary>
        public override string get_TriggerName(int triggerNumber)
        {
            return "";
        }

        #endregion Trigger

        #region Web Interface

        /// <summary>
        /// This function is available for the ease of converting older HS2 plugins,
        /// however, it is desirable to use the new clsPageBuilder class for all new development.
        /// This function is called by HomeSeer from the form or class object that a web page was registered with using RegisterConfigLink.
        /// You must have a GenPage procedure per web page that you register with HomeSeer.
        /// This page is called when the user requests the web page with an HTTP Get command,
        /// which is the default operation when the browser requests a page.
        /// </summary>
        public override string GenPage(string link)
        {
            // GenPage is called if RegisterWebPage doesn't call RegisterPage
            // Difference with GetPagePlugin - GenPage add standard header/footer, 
            // but GetPagePlugin expects full page including header/footer
            return GetPagePlugin(link, null, -1, null);
        }

        /// <summary>
        /// When your plugin web page has form elements on it, and the form is submitted,
        /// this procedure is called to handle the HTTP "Put" request.
        /// There must be one PagePut procedure in each plugin object or
        /// class that is registered as a web page in HomeSeer.
        /// </summary>
        public override string PagePut(string data)
        {
            return "PagePut: '" + data + "'";
        }

        /// <summary>
        /// A complete page needs to be created and returned.
        /// Web pages that use the clsPageBuilder class and registered with
        /// HS.RegisterLink and HS.RegisterConfigLink will then be called through this function. 
        /// Also for GetPagePlugin to be called - must call RegisterPage()
        /// </summary>
        /// <param name="page">The name of the page as passed to the HS.RegisterLink function.</param>
        /// <param name="user">The name of logged in user.</param>
        /// <param name="userRights">The rights of the logged in user.</param>
        /// <param name="queryString">The query string.</param>
        public override string GetPagePlugin(string page, string user, int userRights, string queryString)
        {
            // Get existing page from Utils dictionary
            PageBuilder pb = (PageBuilder)PageBuilder.findRegisteredPage(page);

            if (pb == null)
                return "Page not found: '" + page + "'";

            // If called from GenPage - don't add header/footer, it will be appended
            bool addHeaderFooter = !page.StartsWith("/");
            // See TableBuilder.BuildHeaderLink for queryString format
            NameValueCollection queryPairs = HttpUtility.ParseQueryString(queryString);

            return pb.GetHTML(addHeaderFooter, queryPairs);
        }

        /// <summary>
        /// When a user clicks on any controls on one of your web pages, this function is then called with the post data.
        /// You can then parse the data and process as needed.
        /// </summary>
        /// <param name="page">The name of the page as passed to the HS.RegisterLink function.</param>
        /// <param name="data">The post data.</param>
        /// <param name="user">The name of logged in user.</param>
        /// <param name="userRights">The rights of the logged in user.</param>
        /// <returns>Any serialized data that needs to be passed back to the web page, generated by the clsPageBuilder class.</returns>
        public override string PostBackProc(string page, string data, string user, int userRights)
        {
            Console.WriteLine("PostBackProc: {0} ~ {1}", page, data);
            return "PostBackProc: " + page;
        }


        /// <summary>
        /// If SupportsConfigDevice returns <c>true</c>, this function will be called when the device properties are displayed for your device.
        /// The device properties is displayed from the Device Utility page.
        /// This page displays a tab for each plugin that controls the device.
        /// Normally, only one plugin will be associated with a single device.
        /// If there is any configuration that needs to be set on the device, you can return any HTML that you would like displayed.
        /// Normally this would be any jquery controls that allow customization of the device.
        /// The returned HTML is just an HTML fragment and not a complete page.
        /// </summary>
        /// <param name="ref">The device reference id.</param>
        /// <param name="user">The name of logged in user.</param>
        /// <param name="userRights">The rights of the logged in user.</param>
        /// <param name="newDevice"><c>True</c> if this is a new device being created for the first time.
        /// In this case, the device configuration dialog may present different information than when simply editing an existing device.</param>
        /// <returns>A string containing HTML to be displayed. Return an empty string if there is not configuration needed.</returns>
        public override string ConfigDevice(int deviceId, string user, int userRights, bool newDevice)
        {
            return controller.ConfigDevice(deviceId); ;
        }


        /// <summary>
        /// Called when a user posts information from your plugin tab on the device utility page. 
        /// </summary>
        /// <param name="ref">The device reference id.</param>
        /// <param name="data">The post data.</param>
        /// <param name="user">The name of logged in user.</param>
        /// <param name="userRights">The rights of the logged in user.</param>
        /// <returns>Enums.ConfigDevicePostReturn.</returns>
        public override Enums.ConfigDevicePostReturn ConfigDevicePost(int deviceId, string data, string user, int userRights)
        {
            NameValueCollection parts = HttpUtility.ParseQueryString(data);
            return controller.ConfigDevicePost(deviceId, parts);
        }

        #endregion Web Interface

        #region Support functions



        /// <summary>
        /// Create a new device and set the names for the status display.
        /// </summary>
        /// <param name="refId">The device reference identifier for HomeSeer.</param>
        /// <param name="name">The name for the device.</param>
        /// <returns>Scheduler.Classes.DeviceClass.</returns>
        //private Scheduler.Classes.DeviceClass CreateDevice(out int refId, string name = "HikVision Camera")
        //{
        //    Scheduler.Classes.DeviceClass dv = null;
        //    refId = HS.NewDeviceRef(name);
        //    if (refId > 0)
        //    {
        //        dv = (Scheduler.Classes.DeviceClass)HS.GetDeviceByRef(refId);
        //        dv.set_Address(HS, "Camera" + refId);
        //        dv.set_Device_Type_String(HS, "HikVision Camera Alarm");
        //        DeviceTypeInfo_m.DeviceTypeInfo DT = new DeviceTypeInfo_m.DeviceTypeInfo();
        //        DT.Device_API = DeviceTypeInfo_m.DeviceTypeInfo.eDeviceAPI.Security;
        //        DT.Device_Type = (int)DeviceTypeInfo_m.DeviceTypeInfo.eDeviceType_Security.Zone_Interior;
        //        dv.set_DeviceType_Set(HS, DT);
        //        dv.set_Interface(HS, GetName());
        //        dv.set_InterfaceInstance(HS, "");
        //        dv.set_Last_Change(HS, DateTime.Now);
        //        dv.set_Location(HS, "Camera"); // room
        //        dv.set_Location2(HS, "HikVision"); // floor
        //
        //        VSVGPairs.VSPair Pair = new VSVGPairs.VSPair(HomeSeerAPI.ePairStatusControl.Status);
        //        Pair.PairType = VSVGPairs.VSVGPairType.SingleValue;
        //        Pair.Value = -1;
        //        Pair.Status = "Unknown";
        //        Default_VS_Pairs_AddUpdateUtil(refId, Pair);
        //
        //        Pair = new VSVGPairs.VSPair(HomeSeerAPI.ePairStatusControl.Status);
        //        Pair.PairType = VSVGPairs.VSVGPairType.SingleValue;
        //        Pair.Value = 0;
        //        Pair.Status = "No Motion";
        //        Default_VS_Pairs_AddUpdateUtil(refId, Pair);
        //
        //        Pair = new VSVGPairs.VSPair(HomeSeerAPI.ePairStatusControl.Status);
        //        Pair.PairType = VSVGPairs.VSVGPairType.SingleValue;
        //        Pair.Value = 1;
        //        Pair.Status = "Motion";
        //        Default_VS_Pairs_AddUpdateUtil(refId, Pair);
        //
        //        dv.MISC_Set(HS, Enums.dvMISC.STATUS_ONLY);
        //        dv.MISC_Set(HS, Enums.dvMISC.SHOW_VALUES);
        //        dv.set_Status_Support(HS, true);
        //    }
        //
        //    return dv;
        //}

        /// <summary>
        /// Add the protected, default VS/VG pairs WITHOUT overwriting any user added pairs unless absolutely necessary (because they conflict).
        /// </summary>
        /// <param name="refId">The device reference identifier for HomeSeer.</param>
        /// <param name="Pair">The value/status pair.</param>
        //private void Default_VS_Pairs_AddUpdateUtil(int refId, VSVGPairs.VSPair Pair)
        //{
        //    if ((Pair == null) || (refId < 1) || (!HS.DeviceExistsRef(refId)))
        //        return;
        //
        //    try
        //    {
        //        VSVGPairs.VSPair Existing = HS.DeviceVSP_Get(refId, Pair.Value, Pair.ControlStatus);
        //        if (Existing != null)
        //        {
        //            // This is unprotected, so it is a user's value/ status pair.
        //            if ((Existing.ControlStatus == HomeSeerAPI.ePairStatusControl.Both) && (Pair.ControlStatus != HomeSeerAPI.ePairStatusControl.Both))
        //            {
        //                // The existing one is for BOTH, so try changing it to the opposite of what we are adding and then add it.
        //                if (Pair.ControlStatus == HomeSeerAPI.ePairStatusControl.Status)
        //                {
        //                    if (!HS.DeviceVSP_ChangePair(refId, Existing, HomeSeerAPI.ePairStatusControl.Control))
        //                    {
        //                        HS.DeviceVSP_ClearBoth(refId, Pair.Value);
        //                        HS.DeviceVSP_AddPair(refId, Pair);
        //                    }
        //                    else
        //                        HS.DeviceVSP_AddPair(refId, Pair);
        //                }
        //                else
        //                {
        //                    if (!HS.DeviceVSP_ChangePair(refId, Existing, HomeSeerAPI.ePairStatusControl.Status))
        //                    {
        //                        HS.DeviceVSP_ClearBoth(refId, Pair.Value);
        //                        HS.DeviceVSP_AddPair(refId, Pair);
        //                    }
        //                    else
        //                        HS.DeviceVSP_AddPair(refId, Pair);
        //                }
        //            }
        //            else if (Existing.ControlStatus == HomeSeerAPI.ePairStatusControl.Control)
        //            {
        //                // There is an existing one that is STATUS or CONTROL - remove it if ours is protected.
        //                HS.DeviceVSP_ClearControl(refId, Pair.Value);
        //                HS.DeviceVSP_AddPair(refId, Pair);
        //            }
        //            else if (Existing.ControlStatus == HomeSeerAPI.ePairStatusControl.Status)
        //            {
        //                // There is an existing one that is STATUS or CONTROL - remove it if ours is protected.
        //                HS.DeviceVSP_ClearStatus(refId, Pair.Value);
        //                HS.DeviceVSP_AddPair(refId, Pair);
        //            }
        //        }
        //        else
        //        {
        //            // There is not a pair existing, so just add it.
        //            HS.DeviceVSP_AddPair(refId, Pair);
        //        }
        //    }
        //    catch (Exception)
        //    {
        //    }
        //}

        #endregion Support functions

    }
}