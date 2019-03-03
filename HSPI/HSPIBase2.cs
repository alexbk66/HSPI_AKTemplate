﻿// Copyright (C) 2016 SRG Technology, LLC
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using HomeSeerAPI;

namespace HSPI_AKTemplate
{
    using HSDevice = Scheduler.Classes.DeviceClass;
    using DeviceDictHS = Dictionary<int, Scheduler.Classes.DeviceClass>;

    /// <summary>
    ///     Base class for HSPI plugin.
    ///     <para />
    ///     A new class with the name HSPI should be derived from this base class.
    ///     <list type="number">
    ///         <item>
    ///             <description>The namespace of the new class must be the same as the EXE filename (without extension).</description>
    ///         </item>
    ///         <item>
    ///             <description>This new class must be public, named HSPI, and be in the root of the namespace.</description>
    ///         </item>
    ///     </list>
    ///     <para />
    ///     Adapted from C# sample generated by Marcus Szolkowski.
    ///     See thread "Really simple C# sample plugin available here!" http://board.HomeSeer.com/showthread.php?t=178122.
    /// </summary>
    /// <seealso cref="IPlugInAPI" />
    public abstract class HspiBase2 : HspiBase
    {
        public ControllerBase controller { get; protected set; }

        public DeviceDictHS Devices { get { return controller.devices; } }



        public override IPlugInAPI.strInterfaceStatus InterfaceStatus()
        {
            var s = new IPlugInAPI.strInterfaceStatus {intStatus = IPlugInAPI.enumInterfaceStatus.OK};
            return s;
        }

        public override string InstanceFriendlyName()
        {
            return string.Empty;
        }

        public override int Capabilities()
        {
            return (int) Enums.eCapabilities.CA_IO;
        }

        public override int AccessLevel()
        {
            return 1;
        }

        protected override bool GetHscomPort()
        {
            return false; // true
        }

        public override bool SupportsAddDevice()
        {
            return false;
        }

        public override bool SupportsConfigDevice()
        {
            return false;
        }

        public override bool SupportsConfigDeviceAll()
        {
            return false;
        }

        public override bool SupportsMultipleInstances()
        {
            return false;
        }

        public override bool SupportsMultipleInstancesSingleEXE()
        {
            return false;
        }

        public override bool RaisesGenericCallbacks()
        {
            return false;
        }

        public override void HSEvent(Enums.HSEvent eventType, object[] parameters)
        {
        }

        public override string InitIO(string port)
        {
            // let our console wrapper know we recived InitIO request from HS
            InitIOEvent.Set();
            return "";
        }

        public override IPlugInAPI.PollResultInfo PollDevice(int deviceId)
        {
            var pollResult = new IPlugInAPI.PollResultInfo
            {
                Result = IPlugInAPI.enumPollResult.Device_Not_Found,
                Value = 0
            };

            return pollResult;
        }

        protected override bool GetHasTriggers()
        {
            return false;
        }

        protected override int GetTriggerCount()
        {
            return 0;
        }

        public override void SetIOMulti(List<CAPI.CAPIControl> colSend)
        {
            // HomeSeer will inform us when the one of our devices has changed.  Push that change through to the field.
        }

        public override void ShutdownIO()
        {
            // let our console wrapper know we are finished
            Shutdown = true;
            ShutdownEvent.Set();
        }

        public override SearchReturn[] Search(string searchString, bool regEx)
        {
            return null;
        }

        public override string ActionBuildUI(string uniqueControlId, IPlugInAPI.strTrigActInfo actionInfo)
        {
            return "";
        }

        public override bool ActionConfigured(IPlugInAPI.strTrigActInfo actionInfo)
        {
            return true;
        }

        public override int ActionCount()
        {
            return 0;
        }

        public override string ActionFormatUI(IPlugInAPI.strTrigActInfo actionInfo)
        {
            return "";
        }

        public override IPlugInAPI.strMultiReturn ActionProcessPostUI(NameValueCollection postData,
            IPlugInAPI.strTrigActInfo actionInfo)
        {
            return new IPlugInAPI.strMultiReturn();
        }

        public override bool ActionReferencesDevice(IPlugInAPI.strTrigActInfo actionInfo, int deviceId)
        {
            return false;
        }

        public override string get_ActionName(int actionNumber)
        {
            return "";
        }

        public override bool get_Condition(IPlugInAPI.strTrigActInfo actionInfo)
        {
            return false;
        }

        public override void set_Condition(IPlugInAPI.strTrigActInfo actionInfo, bool value)
        {
        }

        public override bool get_HasConditions(int triggerNumber)
        {
            return false;
        }

        public override string TriggerBuildUI(string uniqueControlId, IPlugInAPI.strTrigActInfo triggerInfo)
        {
            return "";
        }

        public override string TriggerFormatUI(IPlugInAPI.strTrigActInfo actionInfo)
        {
            return "";
        }

        public override IPlugInAPI.strMultiReturn TriggerProcessPostUI(NameValueCollection postData,
            IPlugInAPI.strTrigActInfo actionInfo)
        {
            return new IPlugInAPI.strMultiReturn();
        }

        public override bool TriggerReferencesDevice(IPlugInAPI.strTrigActInfo actionInfo, int deviceId)
        {
            return false;
        }

        public override bool TriggerTrue(IPlugInAPI.strTrigActInfo actionInfo)
        {
            return false;
        }

        public override int get_SubTriggerCount(int triggerNumber)
        {
            return 0;
        }

        public override string get_SubTriggerName(int triggerNumber, int subTriggerNumber)
        {
            return "";
        }

        public override bool get_TriggerConfigured(IPlugInAPI.strTrigActInfo actionInfo)
        {
            return true;
        }

        public override string get_TriggerName(int triggerNumber)
        {
            return "";
        }

        public override bool HandleAction(IPlugInAPI.strTrigActInfo actionInfo)
        {
            return false;
        }

        public override void SpeakIn(int deviceId, string text, bool wait, string host)
        {
        }

        public override string GenPage(string link)
        {
            return "";
        }

        public override string PagePut(string data)
        {
            return "";
        }

        public override string GetPagePlugin(string page, string user, int userRights, string queryString)
        {
            return "";
        }

        public override string PostBackProc(string page, string data, string user, int userRights)
        {
            Console.WriteLine("PostBackProc: {0} ~ {1}", page, data);

            return "PostBackProc";
        }

        public override string ConfigDevice(int deviceId, string user, int userRights, bool newDevice)
        {
            return "";
        }

        public override Enums.ConfigDevicePostReturn ConfigDevicePost(int deviceId,
            string data,
            string user,
            int userRights)
        {
            return Enums.ConfigDevicePostReturn.DoneAndCancel;
        }

        /// <summary>
        /// There may be times when you need to offer a custom function that is not part of the plugin API. 
        /// The API functions allow users to call your plugin from scripts and web pages by calling the functions by name.
        /// </summary>
        /// <returns>The property get.</returns>
        /// <param name="procedureName">Procedure name.</param>
        /// <param name="parameters">parameters.</param>
        public override object PluginFunction(string functionName, object[] parameters)
        {
            return null;
        }

        /// <summary>
        /// There may be times when you need to offer a custom function that is not part of the plugin API. 
        /// The API functions allow users to call your plugin from scripts and web pages by calling the functions by name.
        /// </summary>
        /// <returns>The property get.</returns>
        /// <param name="procedureName">Procedure name.</param>
        /// <param name="parameters">parameters.</param>
        public override object PluginPropertyGet(string propertyName, object[] parameters)
        {
            return null;
        }

        /// <summary>
        /// There may be times when you need to offer a custom function that is not part of the plugin API. 
        /// The API functions allow users to call your plugin from scripts and web pages by calling the functions by name.
        /// </summary>
        /// <returns>The property get.</returns>
        /// <param name="procedureName">Procedure name.</param>
        /// <param name="parameters">parameters.</param>
        public override void PluginPropertySet(string propertyName, object value)
        {
        }

        //public override void SetDeviceValue(int deviceId, double value, bool trigger = true)
        //{
        //    HS.SetDeviceValueByRef(deviceId, value, trigger);
        //}

        #region RegisterWebPage

        ///<summary>
        /// Registers the web page in HomeSeer
        ///</summary>
        ///<param name="linktext">The text to be shown</param>
        ///<param name="link">A short link to the page</param>
        ///<param name="page_title">The title of the page when loaded</param>
        /// <param name="config">RegisterConfigLink for "manage Interfaces" page</param>
        public string RegisterWebPage(string linktext = "", string link = "", string page_title = "", bool config=false)
        {
            if (linktext == "")
            {
                // Make text same as link, but replace '_' with space
                linktext = link.Replace("_", " ");
            }
            else if (link == "")
            {
                link = linktext.Replace(" ", "_");
            }

            if (page_title == "")
                page_title = linktext;

            // Make link unique
            if(!link.Contains(PluginNameNoSpace))
                link = PluginNameNoSpace + "_" + link;

            RegisterWebPageDesc(linktext, link, page_title, config);

            // This link HS will pass to GetPagePlugin
            return link;
        }

        ///<summary>
        /// Registers the web page in HomeSeer
        ///</summary>
        ///<param name="linktext">The text to be shown</param>
        ///<param name="link">A short link to the page</param>
        ///<param name="page_title">The title of the page when loaded</param>
        /// <param name="config">RegisterConfigLink for "manage Interfaces" page</param>
        public void RegisterWebPageDesc(string linktext, string link, string page_title, bool config = false, bool helplink = false)
        {
            // Register page
            if(!helplink)
            {
                string err = HS.RegisterPage(link, this.Name, PluginInstance: "");
                if(!String.IsNullOrEmpty(err))
                    Console.WriteLine($"RegisterPage: {link} - {err}");
            }

            // Register callback
            //try
            {
                WebPageDesc wpd = new WebPageDesc
                {
                    plugInName = (config||helplink) ? this.Name : this.PluginNameCleaned,
                    link = link,
                    linktext = linktext,
                    page_title = page_title
                };

                if(helplink)
                {
                    // Link for Help Menu
                    HS.RegisterHelpLink(wpd);
                }
                else if(config)
                {
                    // Link for "Manage Plugins" page
                    Callback.RegisterConfigLink(wpd);
                }
                else
                {
                    // Link for Plugins Menu
                    Callback.RegisterLink(wpd);
                }
            }
            //catch (Exception ex)
            //{
            //    Console.WriteLine("Registering Web Links (RegisterWebPage): " + ex.Message);
            //}
        }

        public string IniFile
        {
            get
            {
                return PluginNameNoSpace + ".ini";
            }
        }

        public string PluginNameNoSpace
        {
            get
            {
                return PluginNameCleaned.Replace(" ", "");
            }
        }

        public string PluginNameCleaned
        {
            get
            {
                // RegisterLink doesn't like these in page link title for some reason
                string bad = "(){}[]@#$%&*";
                string plug_name = Name;
                for (int i = 0; i < bad.Length; i++)
                    plug_name = plug_name.Replace(bad[i].ToString(), "");
                return plug_name;
            }
        }

        #endregion RegisterWebPage

    }
}