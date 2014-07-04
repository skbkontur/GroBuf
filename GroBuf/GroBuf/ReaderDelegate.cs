using System;

namespace GroBuf
{
    public delegate void ReaderDelegate<T>(IntPtr data, ref int index, ref T result, ReaderContext context);

    public delegate void ReaderDelegate(IntPtr data, ref int index, ref object result, ReaderContext context);
}