using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;

namespace HSPI_AKTemplate
{
    /// <summary>
    /// COMMENT, UTILS
    /// </summary>
    [Serializable]
    public class MyIntList : List<int>, IComparable<MyIntList>
    {
        public MyIntList() : base()
        {
        }

        public MyIntList(List<int> copy) : base(copy)
        {
        }

        public override string ToString()
        {
            return String.Join(", ", this.ToArray());
        }

        public int CompareTo(MyIntList other)
        {
            if (other == null || this.Count != other.Count)
                return -1;

            for (int i = 0; i < this.Count; i++)
            {
                if (this[i] != other[i])
                    return 1;
            }
            return 0;
        }

        public override bool Equals(Object other)
        {
            return CompareTo((MyIntList)other) == 0;
        }

    }

    [Serializable]
    public class DeviceIdList : MyIntList
    {
        public DeviceIdList() : base()
        {
        }

        public DeviceIdList(List<int> copy) : base(copy)
        {
        }

        /// <summary>
        /// Add Device to group ("adddev" command)
        /// </summary>
        /// <param name="devID"></param>
        /// <returns></returns>
        public bool AddDevice(int devID = 0)
        {
            if (!this.Contains(devID))
            {
                this.Add(devID);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove device from group at dev_num index
        /// </summary>
        /// <param name="dev_num"></param>
        /// <param name="devID"></param>
        /// <returns></returns>
        public bool RemoveDevice(int dev_num, int devID)
        {
            if (dev_num >= 0 && dev_num < this.Count &&
                this[dev_num] == devID)
            {
                this.Remove(devID);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Set device ID at dev_num
        /// </summary>
        /// <param name="dev_num">Device index in group to update</param>
        /// <param name="dev_id">Device ID to set</param>
        /// <returns></returns>
        public bool SetDevice(int dev_num, int devID)
        {
            if (dev_num >= 0 && dev_num < this.Count &&
                this[dev_num] != devID)
            {
                this[dev_num] = devID;
                return true;
            }
            return false;
        }
    }
}
