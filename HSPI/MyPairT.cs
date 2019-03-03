using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSPI_AKTemplate
{

    /// <summary>
    /// List of pairs (string, T)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MyPairT<T> : ICloneable
    {
        public string Name { set; get; }
        public T Value { set; get; }

        public MyPairT(string Name, T Value)
        {
            this.Name = Name;
            this.Value = Value;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Returns Value converted to String
        /// </summary>
        public string ValueStr
        {
            get
            {
                return Value != null ? Value.ToString() : default(T).ToString();
            }
        }
    }

    /// <summary>
    /// List of pairs (string, T)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    //public class MyPairListT<T> : List<MyPairT<T>>
    //{
    //}

    /// <summary>
    /// Specialization MyPairT<object>
    /// </summary>
    public class MyPair : MyPairT<object>
    {
        public MyPair(string Name = "", object Value = null) : base(Name, Value)
        {
        }

        public bool Equals(MyPair y)
        {
            if (this == null && y == null)
                return true;
            if (this == null || y == null)
                return false;

            // Compare value string, not value
            return (this.Name == y.Name && this.ValueStr == y.ValueStr);
        }
    }


    public class MyPairList : List<MyPair>
    {
        public MyPairList() : base()
        {
        }
        public MyPairList(MyPairList other) : base(other)
        {
        }

        /// <summary>
        /// Insert a pair if doesn't exist at specified index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        /// <returns>specified index if pair inserted, otherwice index-1</returns>
        public new int Insert(int index, MyPair item)
        {
            if(!Contains(item))
            {
                base.Insert(index, item);
                return index;
            }
            return index-1;
        }

        public new bool Contains(MyPair item)
        {
            foreach(var pair in this)
            {
                if(pair.Equals(item))
                    return true;
            }
            return false;
        }
    }

}
