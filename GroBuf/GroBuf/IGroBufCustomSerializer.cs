using System;

namespace GroBuf
{
    public interface IGroBufCustomSerializer
    {
        int CountSize(object obj, bool writeEmpty, WriterContext context);
        void Write(object obj, bool writeEmpty, IntPtr result, ref int index, WriterContext context);
        void Read(IntPtr data, ref int index, ref object result, ReaderContext context);
    }
}