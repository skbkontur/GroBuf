using System;

namespace GroBuf
{
    public delegate void ReaderDelegate<T>(IntPtr data, ref int index, int length, ref T result);

    public delegate void ReaderDelegate(IntPtr data, ref int index, int length, ref object result);
}