using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

using HSPI_AKTemplate;

namespace HSPI_AKExample
{
    using HSDevice = Scheduler.Classes.DeviceClass;
    using DeviceDict = Dictionary<int, DeviceBase>;

    class ConfigPage : PageBuilder
    {
        private static string pageName = "Config";

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigPage"/> class.
        /// Assume Controller.UpdateConfiguration() was called by plugin already
        /// Also RegisterConfigLink for "manage Interfaces" page
        /// </summary>
        /// <param name="plugin">The HomeSeer plugin.</param>
        public ConfigPage(HSPI plugin) : base(pageName, plugin, config:true)
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
            //BuildTable_Info(stb, queryPairs.Get("sortby"), queryPairs.Get("sortorder"));

            // Stop timer
            long dur = duration;

            return stb.ToString();
        }

        //private void BuildTable_Info(StringBuilder stb, string sortby = null, string sortorder = null)
        //{
        //    ConfigDevice.BuildTable_Info( stb,
        //                                  ((HSPI)plugin).controller.UsedDevices,
        //                                  page_name: page_link,
        //                                  sortby: sortby,
        //                                  sortorder: sortorder,
        //                                  inSlider: false
        //                                  );
        //}

    }
}
