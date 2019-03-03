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

    }
}
