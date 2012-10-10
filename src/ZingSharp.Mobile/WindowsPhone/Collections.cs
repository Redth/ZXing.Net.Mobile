using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace System
{
    public class Serializable : Attribute
    {
    }
}

namespace System.Collections
{
   
    public class ArrayList : List<object>
    {
        public ArrayList() : base() { }

        public ArrayList(int capacity)
            : base(capacity)
        { }

        public static ArrayList Synchronized(ArrayList list)
        {
            return list;
        }

       
    }

    public class Hashtable : Dictionary<object, object>
    {
        public Hashtable() : base() { }
        public Hashtable(int capacity)
            : base(capacity)
        { }

        public static Hashtable Synchronized(Hashtable table)
        {
            return table;
        }

        public object this[object index]
        {
            get
            {
                if (this.ContainsKey(index))
                    return base[index];
                else
                    return null;
            }
            set
            {
                if (this.ContainsKey(index))
                    this[index] = value;
                else
                    this.Add(index, value);
            }
        }
    }
}
