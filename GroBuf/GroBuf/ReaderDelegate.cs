using System;
using System.Collections.Generic;

namespace GroBuf
{
    public delegate void ReaderDelegate<T>(IntPtr data, ref int index, ref T result, ReaderContext context);

    public delegate void ReaderDelegate(IntPtr data, ref int index, ref object result, ReaderContext context);

    public class ReaderContext
    {
        public int length;
        public int count;
        public readonly List<object> objects = new List<object>();
    }
}