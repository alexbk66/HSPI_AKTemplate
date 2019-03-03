using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using HomeSeerAPI;
using HSPI_AKTemplate;

namespace HSPI_AKExample
{
    class DeviceScreen : DeviceBase
    {
        public DeviceScreen(Controller controller, int devID = 0) : base(controller, devID)
        {
            Create();
        }

        public override void Create()
        {
            Create("Screen", force: false);
            AddVSPair(0, "Off", ePairControlUse._Off, "/images/HomeSeer/status/off.gif");
            AddVSPair(100, "On", ePairControlUse._On, "/images/HomeSeer/status/on.gif");
        }

        public override void NotifyValueChange(double value, string cause)
        {
            switch(value)
            {
                case 0:
                    MonitorOff();
                    break;
                case 100:
                    MonitorOn();
                    break;
            }
        }


        #region ScreenOnOff

        private static int WM_SYSCOMMAND = 0x0112;
        private static uint SC_MONITORPOWER = 0xF170;

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern void mouse_event(Int32 dwFlags, Int32 dx, Int32 dy, Int32 dwData, UIntPtr dwExtraInfo);

        private const int WmSyscommand = 0x0112;
        private const int ScMonitorpower = 0xF170;
        private const int MonitorShutoff = 2;
        private const int MouseeventfMove = 0x0001;

        public static void MonitorOff()
        {
            SendMessage(GetConsoleWindow(), WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)2);
        }

        public static void MonitorOn()
        {
            mouse_event(MouseeventfMove, 0, 1, 0, UIntPtr.Zero);
            Thread.Sleep(40);
            mouse_event(MouseeventfMove, 0, -1, 0, UIntPtr.Zero);
        }

        #endregion ScreenOnOff
    }
}
