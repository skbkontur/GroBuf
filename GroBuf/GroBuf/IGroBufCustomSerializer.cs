using System;

namespace GroBuf
{
    public interface IGroBufCustomSerializer
    {
        int CountSize(object obj, bool writeEmpty);
        void Write(object obj, bool writeEmpty, IntPtr result, ref int index, int resultLength);
        void Read(IntPtr data, ref int index, ref object result, ReaderContext context);
    }
}