using System;
using System.Collections;
using System.Collections.Generic;
using Java.Util;
using ZXing;

namespace BigIntegerLibrary
{
    /// <summary>
    /// BigInteger-related exception class.
    /// </summary>
    //[System.Serializable]
    public sealed class BigIntegerException : Exception
    {
        /// <summary>
        /// BigIntegerException constructor.
        /// </summary>
        /// <param name="message">The exception message</param>
        /// <param name="innerException">The inner exception</param>
        public BigIntegerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public sealed class DynamicList<T> : IList<T>
    {
        private readonly List<T> innerList = new List<T>();
        public IEnumerator<T> GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)innerList).GetEnumerator();
        }

        public void Add(T item)
        {
            innerList.Add(item);
        }

        public void Clear()
        {
            innerList.Clear();
        }

        public bool Contains(T item)
        {
            return innerList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            innerList.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return innerList.Remove(item);
        }

        public int Count
        {
            get { return innerList.Count; }
        }

        public bool IsReadOnly { get { return false; } }
        public int IndexOf(T item)
        {
            return innerList.IndexOf(item);
        }

        private void ResizeToFitIfNeeded(int index)
        {
            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            while (index >= innerList.Count)
            {
                innerList.Add(default(T));
            }
        }
        public void Insert(int index, T item)
        {
            ResizeToFitIfNeeded(index);
            innerList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            if (index >= innerList.Count)
                return;
            innerList.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                return index >= innerList.Count ? default(T) : innerList[index];
            }
            set
            {
                ResizeToFitIfNeeded(index);
                innerList[index] = value;
                CompactList();
            }
        }

        private void CompactList()
        {
            while (innerList.Count > 0 && Equals(innerList[innerList.Count - 1], default(T)))
                innerList.RemoveAt(innerList.Count - 1);
        }
    }
}