using System;
using HSPI_AKTemplate;

namespace HSPI_AKExample
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // create an instance of our plugin.
            HSPI plugin = new HSPI();
            int ret = plugin.ConnectMain(args);
            System.Environment.Exit(ret);
        }
    }
}