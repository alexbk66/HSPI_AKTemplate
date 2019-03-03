using CommandLine;

namespace HSPI_AKTemplate
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Options
    {
        [Option('p', "port", HelpText = "HomeSeer admin port")]
        public int Port { get; set; } = 10400;

        [Option('s', "server", HelpText = "HomeSeer IP address")]
        public string Server { get; set; } = "127.0.0.1";

        [Option('s', "~server", HelpText = "testing")]
        public string Server1 { get; set; } = "127.0.0.1";
    }
}