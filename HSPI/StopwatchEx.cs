using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSPI_AKTemplate
{
    public class StopwatchEx
    {
        public static Dictionary<string, long> FuncTimingDict = new Dictionary<string, long>();

        public string Name { get; private set; }
        private System.Diagnostics.Stopwatch watch;

        public StopwatchEx(string name)
        {
            this.Name = name;
            FuncTimingDict[name] = -1;
            watch = System.Diagnostics.Stopwatch.StartNew();
        }

        public long Stop()
        {
            watch.Stop();
            FuncTimingDict[this.Name] = watch.ElapsedMilliseconds;
            return watch.ElapsedMilliseconds;
        }

        public static new string ToString()
        {
            string timings = ""; // "<span style='font-size:10px; max-width: 300px; display: inline-block;'>";
            foreach (string name in FuncTimingDict.Keys)
            {
                timings += name + ": " + FuncTimingDict[name] + " ms; ";
            }
            //timings += "</span>";
            return timings;
        }
    }
}
