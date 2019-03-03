using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using HomeSeerAPI;
using HSPI_AKTemplate;

namespace HSPI_AKExample
{
    using HSDevice = Scheduler.Classes.DeviceClass;
    using DeviceDict = Dictionary<int, DeviceBase>;

    class StatusPage : PageBuilder
    {
        private static string pageName = "My Devices";

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusPage"/> class.
        /// Assume Controller.UpdateConfiguration() was called by plugin already
        /// Also RegisterConfigLink for "manage Interfaces" page
        /// </summary>
        /// <param name="plugin">The HomeSeer plugin.</param>
        public StatusPage(HSPI plugin) : base(pageName, plugin, config:true)
        {
            this.suppressDefaultFooter = true;
        }

        /// <summary>
        /// Return HTML representation of the page
        /// </summary>
        /// <param name="queryPairs">See TableBuilder.BuildHeaderLink for queryString format</param>
        /// <returns></returns>
        public override string BuildContent(NameValueCollection queryPairs = null)
        {
            var stb = new StringBuilder();

            // See TableBuilder.BuildHeaderLink for queryString format
            BuildTable_Info(stb, queryPairs.Get("sortby"), queryPairs.Get("sortorder"));

            // Stop timer
            long dur = duration;

            return stb.ToString();
        }

        private void BuildTable_Info(StringBuilder stb, string sortby = null, string sortorder = null)
        {
            StatusPage.BuildTable_Info( stb,
                                          ((HSPI)plugin).controller.UsedDevices,
                                          page_name: page_link,
                                          sortby: sortby,
                                          sortorder: sortorder,
                                          inSlider: false
                                          );
        }

        /// <summary>
        ///  Build info table for all UsedDevices
        /// </summary>
        /// <param name="stb"></param>
        /// <param name="UsedDevices"></param>
        /// <param name="title"></param>
        /// <param name="tt"></param>
        /// <param name="stateDevice">If pass stateDevice, then display only devices controlled by the stateDevice</param>
        /// <param name="triggerDevice">Same as stateDevice</param>
        /// <param name="page_name">If want to add sorting to table columns, see AddHeader/BuildHeaderLink</param>
        /// <param name="sorthdr">on/of sorting</param>
        /// <param name="sortby">As above</param>
        /// <param name="sortorder">As above</param>
        /// <param name="inSlider">Put table inside SliderTab</param>
        /// <param name="tableID">ID for table and SliderTab</param>
        /// <param name="initiallyOpen"></param>
        /// <param name="noempty">Don't show empty table</param>
        public static void BuildTable_Info(StringBuilder stb,
                                        DeviceDict UsedDevices,
                                        string title = null,
                                        string tt = null,
                                        DeviceBase stateDevice = null,
                                        DeviceBase triggerDevice = null,
                                        string page_name = null,
                                        bool sorthdr = true,
                                        string sortby = null,
                                        string sortorder = null,
                                        bool? inSlider = null,
                                        string tableID = null,
                                        bool initiallyOpen = true,
                                        bool noempty = false
                                        )
        {
            List<string> hdr = new List<string>(){ "Ref",
                PageBuilder.LocationLabels.Item1, PageBuilder.LocationLabels.Item2,
                "Name", "Plugin", "Type", "Timer", "PowerFail", "States", "Changed", "Log", "Triggers"};

            // Don't show state device if already on the state device page
            if (stateDevice == null)
            {
                hdr.Add("State Device");
                hdr.Add("Changed");
            }

            // Prepare TableData
            TableBuilder.TableData data = new TableBuilder.TableData();

            foreach (DeviceBase device in UsedDevices.Values)
            {
                // Filter 1: list of devices controlled by stateDevice
                //?if (stateDevice != null && device.StateDeviceId != stateDevice.RefId)
                //?    continue;
                //?
                //?// Filter 2: list of devices controlled by stateDevice
                //?if (triggerDevice != null && !device.TriggerDevices.Contains(triggerDevice.RefId))
                //?    continue;

                var row = data.AddRow();

                row.Add(device.RefId.ToString());

                if (PageBuilder.bLocationFirst)
                    row.Add(device.Location);
                row.Add(device.Location2);
                if (!PageBuilder.bLocationFirst)
                    row.Add(device.Location);

                row.Add(device.GetURL());
                row.Add(device.Interface);
                row.Add(device.Type);
                //?row.Add(BoolToImg(device.use_cntdwn_timer));
                //?row.Add(Utils.ToString(device.AfterDelay));
                row.Add(String.Join(", ", device.vspsListStr(ePairStatusControl.Both)));
                row.Add(device.LastChange);
                row.Add(BoolToImg(device.log_enable));
                //?row.Add(device.ListOfTriggerIds());

                // Don't show state device if already on the state device page
                //?if (stateDevice == null)
                //?{
                //?    row.Add(device.StateDeviceURL);
                //?    row.Add(device.StateDeviceId != 0 ? device.StateDevice.LastChange : "");
                //?};
            }

            string html = TableBuilder.BuildTable(data,
                                                   hdr: hdr.ToArray(),
                                                   title: title,
                                                   tooltip: tt,
                                                   page_name: page_name,
                                                   sorthdr: sorthdr,
                                                   sortby: sortby,
                                                   sortorder: sortorder,
                                                   noempty: noempty,
                                                   inSlider: inSlider,
                                                   tableID: tableID,
                                                   initiallyOpen: initiallyOpen);
            stb.Append(html);
        }

    }
}
