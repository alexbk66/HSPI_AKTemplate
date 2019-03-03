using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HSPI_AKTemplate;

namespace HSPI_AKExample
{
    public class Controller : ControllerBase
    {
        public Controller(HSPI plugin) : base(plugin)
        {
        }

        public override void CheckAndCreateDevices()
        {
            DeviceBase device = new DeviceScreen(this, devID: 0);
        }

        /// <summary>
        /// HSEvent callback informs us that one of used devices was deleted
        /// </summary>
        /// <param name="devID"></param>
        public void DeviceDeleted(int devID)
        {
            DeviceBase device = UsedDevices[devID];
            Log($"Device used by plugin was deleted! {device}", true);

            // TEMP - TODO: do something about it!
            device.Error = "Device was deleted in HomeSeer!";
        }

    }
}
