using System;

namespace GroBuf
{
    public delegate void ReaderDelegate<T>(IntPtr data, ref int index, ref T result, ReaderContext context);

    public delegate void ReaderDelegate(IntPtr data, ref int index, ref object result, ReaderContext context);

    public class ReaderContext
    {
        public ReaderContext(int length, int numberOfObjects)
        {
            this.length = length;
            objects = numberOfObjects == 0 ? null : new object[numberOfObjects];
        }

        public readonly int length;
        public int count;
        public readonly object[] objects;
    }
}