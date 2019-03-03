using System;
using System.Collections.Generic;

using HomeSeerAPI;
using static HomeSeerAPI.VSVGPairs;


namespace HSPI_AKTemplate
{
    using PairStatusControlDict = Dictionary<ePairStatusControl, MyPairList>;
    using ValueCAPIDict = Dictionary<double, CAPI.CAPIControl>;


    public enum SplitRangeType
    {
        NotSplit,   // Add range as single entry
        Split,      // Split range
        SplitAndNot // Add range itself, then add split entries
    }

    /// <summary>
    /// Keep VSPairs for each type (control/status/both) in a dictionary for performance
    /// bool = splitrange - ranges split into individual states
    /// </summary>
    public class StatusPairsDict : Dictionary<SplitRangeType, PairStatusControlDict>
    {
        // Just for readability shorten ePairStatusControl.Control to 'Control', etc.
        private static readonly ePairStatusControl Control = ePairStatusControl.Control;
        private static readonly ePairStatusControl Status = ePairStatusControl.Status;
        private static readonly ePairStatusControl Both = ePairStatusControl.Both;

        private IHSApplication hs;
        private int deviceId;
        private ValueCAPIDict valueCAPIDict = null;

        public StatusPairsDict(IHSApplication hs, int deviceId)
        {
            this.hs = hs;
            this.deviceId = deviceId;
        }

        #region Main

        /// <summary>
        /// Main Function.
        /// Get all VSPairs, for performance keep them in dict with the key Status/Control/Both
        /// Convert VSVGPairs.VSPair[] to simple PairList
        /// Note: ranges *split* into individual states
        /// </summary>
        /// <param name="StatusControl">ePairStatusControl - Status, Control, or Both</param>
        /// <param name="splitrange">ranges *split* into individual states</param>
        /// <returns></returns>
        public MyPairList vspsList(ePairStatusControl StatusControl, SplitRangeType splitrange)
        {
            if (!this.ContainsKey(splitrange))
                this[splitrange] = new PairStatusControlDict();

            // If this particular list is in the Dictionary already - just return it
            if (!this[splitrange].ContainsKey(StatusControl))
            {
                this[splitrange][Control] = MakeVSpsListControl(splitrange);
                this[splitrange][Status] = MakeVSpsListStatus(splitrange);

                // For ePairStatusControl.Both merge two lists (Union)
                this[splitrange][Both] = Merge(this[splitrange][Control], this[splitrange][Status]);
            }
            return this[splitrange][StatusControl];
        }

        /// <summary>
        /// Get Union ot two lists, Linq Union didn't work...
        /// </summary>
        /// <param name="a">List 1</param>
        /// <param name="b">List 2</param>
        /// <returns></returns>
        public static MyPairList Merge(MyPairList a, MyPairList b)
        {
            MyPairList res = new MyPairList(a);
            foreach (MyPair x in b)
            {
                bool found = false;
                foreach (MyPair y in a)
                {
                    if (y.Equals(x))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    res.Add(x);
            }
            return res;
        }


        /// <summary>
        /// Find VSPair matching MyPair min-max range
        /// </summary>
        /// <param name="myPair"></param>
        /// <returns></returns>
        public VSPair FindVSPair(MyPair myPair)
        {
            if (this.vsps != null)
            {
                foreach (VSPair pair in this.vsps)
                {
                    if (MakePair(pair, null).ValueStr == myPair.ValueStr)
                        return pair;
                }
            }
            return null;
        }


        /// <summary>
        /// Find VSPair matching min-max range
        /// NOT USED
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        //public VSPair FindVSPair(double min, double max)
        //{
        //    if (this.vsps != null)
        //    {
        //        foreach (VSPair pair in this.vsps)
        //        {
        //            // May need precision check
        //            if (pair.PairType == VSVGPairType.Range && min >= pair.RangeStart && max <= pair.RangeEnd)
        //                return pair;
        //        }
        //    }
        //    return null;
        //}
        //

        /// <summary>
        /// If value is range "min-max" - split it and return min and max,
        /// See MakePair for value range format
        /// NOT USED
        /// </summary>
        /// <param name="value">range or single val</param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns>True if value is range</returns>
        //public bool SplitRangeValue(string value, out double min, out double max)
        //{
        //    min = 0;
        //    max = 0;
        //    GroupCollection match = Regex.Match(value, "(.+)-(.+)").Groups;
        //    if (match.Count <= 1)
        //        return false;
        //
        //    //Console.WriteLine($"{match[1]}-{match[2]}");
        //    return double.TryParse(match[1].Value, out min) && double.TryParse(match[2].Value, out max);
        //}

        #endregion Main

        #region CAPIControl

        /// <summary>
        /// bool = splitrange
        /// </summary>
        private Dictionary<bool, CAPI.CAPIControl[]> capisDict = new Dictionary<bool, CAPI.CAPIControl[]>();

        /// <summary>
        /// Process CAPIControl[] and convert to list of MyPair
        /// </summary>
        /// <param name="splitrange">ranges *split* into individual states</param>
        /// <returns></returns>
        public MyPairList MakeVSpsListControl(SplitRangeType splitrange)
        {
            MyPairList pairs = new MyPairList();

            // Doesn't make sense for Control?
            if (splitrange == SplitRangeType.SplitAndNot)
                return pairs;

            bool b_splitrange = (splitrange==SplitRangeType.Split);

            // CAPIControl[] - Note: ranges *split* into individual states
            if (!capisDict.ContainsKey(b_splitrange))
                capisDict[b_splitrange] = hs.CAPIGetControlEx(deviceId, SingleRangeEntry: !b_splitrange);

            if(b_splitrange)
                valueCAPIDict = new ValueCAPIDict();

            // Get Control pairs
            foreach (CAPI.CAPIControl capi in capisDict[b_splitrange])
            {
                if (capi != null)
                {
                    if(capi.Label != null)
                        pairs.Add(MakePair(capi));
                    // Keep CAPIControl in ValueCAPIDict for SmartDevice.Value
                    if (b_splitrange)
                        valueCAPIDict[capi.ControlValue] = capi;
                }
            }

            return pairs;
        }

        /// <summary>
        /// Get CAPIControl for state value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public CAPI.CAPIControl GetCAPIControl(double value)
        {
            if (valueCAPIDict == null)
                MakeVSpsListControl(SplitRangeType.Split);

            if (!valueCAPIDict.ContainsKey(value))
                return null;

            return valueCAPIDict[value];
        }

        /// <summary>
        /// Helper for MakeVSpsListControl
        /// If pair.PairType is SingleRangeEntry - make a range string, i.e. "Dim 30-100%"
        /// </summary>
        /// <param name="capi">CAPI.CAPIControl</param>
        /// <returns></returns>
        private static MyPair MakePair(CAPI.CAPIControl capi)
        {
            string name;
            object value;
            if (capi.Range == null)
            {
                name = capi.Label;
                value = capi.ControlValue;
            }
            else
            {
                value = $"{capi.Range.RangeStart}-{capi.Range.RangeEnd}";
                // make a range string, i.e. "Dim 30-100%"
                name = $"{capi.Range.RangeStatusPrefix}({value}){capi.Range.RangeStatusSuffix}";
            }

            return new MyPair(name, value);
        }

        #endregion CAPIControl

        #region VSPair


        /// <summary>
        /// VSPair[] for vspsListSplit
        /// </summary>
        private VSPair[] vsps = null;

        /// <summary>
        /// Process VSPair[] and convert to list of MyPair
        /// Note: ranges *split* into individual states
        /// </summary>
        /// <param name="splitrange">ranges *split* into individual states</param>
        private MyPairList MakeVSpsListStatus(SplitRangeType splitrange)
        {
            // VSPair[]
            if (this.vsps == null)
                this.vsps = hs.DeviceVSP_GetAllStatus(this.deviceId);

            MyPairList pairs = new MyPairList();

            if (this.vsps != null)
            {
                foreach (VSPair pair in this.vsps)
                {
                    if (pair.PairType == VSVGPairType.SingleValue || splitrange==SplitRangeType.NotSplit)
                    {
                        pairs.Add(MakePair(pair, null));
                    }
                    else
                    {
                        // Add the range itself first (only for SplitAndNot)
                        if(splitrange == SplitRangeType.SplitAndNot)
                            pairs.Add(MakePair(pair, null));

                        // May need floor/ceil?
                        // Since we want ranges *split* - make new pair for each value in range
                        for (int v = (int)pair.RangeStart; v <= pair.RangeEnd; v++)
                        {
                            pairs.Add(MakePair(pair, v));

                            // For huge range just add first pair (temporary, need better solution)
                            if (pair.RangeEnd - pair.RangeStart > 100)
                                break;
                        }
                    }
                }
            }

            return pairs;
        }


        /// <summary>
        /// Helper for MakeVSpsListStatus
        /// If pair.PairType is VSVGPairType.Range - make a range string, i.e. "Dim 30-100%"
        /// </summary>
        /// <param name="pair">VSPair</param>
        /// <param name="value">state value, when we want to expand single range state (splitrange=true)</param>
        /// <returns></returns>
        private static MyPair MakePair(VSPair pair, double? value)
        {
            string name;
            object newvalue;
            if (pair.PairType == VSVGPairType.SingleValue || value != null)
            {
                if (value == null)
                    value = pair.Value;
                name = pair.GetPairString((double)value, null, null);
                newvalue = value;
            }
            else
            {
                newvalue = $"{pair.RangeStart}-{pair.RangeEnd}";
                // make a range string, i.e. "Dim 30-100%"
                name = $"{pair.RangeStatusPrefix}({newvalue}){pair.RangeStatusSuffix}";
            }

            return new MyPair(name, newvalue);
        }

        #endregion VSPair
    }
}
