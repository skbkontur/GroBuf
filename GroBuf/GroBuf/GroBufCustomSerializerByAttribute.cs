using System;

namespace GroBuf
{
    internal class GroBufCustomSerializerByAttribute : IGroBufCustomSerializer
    {
        public GroBufCustomSerializerByAttribute(SizeCounterDelegate sizeCounter, WriterDelegate writerDelegate, ReaderDelegate readerDelegate)
        {
            this.sizeCounter = sizeCounter;
            this.writerDelegate = writerDelegate;
            this.readerDelegate = readerDelegate;
        }

        public int CountSize(object obj, bool writeEmpty)
        {
            return sizeCounter(obj, writeEmpty);
        }

        public void Write(object obj, bool writeEmpty, IntPtr result, ref int index, int resultLength)
        {
            writerDelegate(obj, writeEmpty, result, ref index, resultLength);
        }

        public void Read(IntPtr data, ref int index, ref object result, ReaderContext context)
        {
            readerDelegate(data, ref index, ref result, context);
        }

        private readonly SizeCounterDelegate sizeCounter;
        private readonly WriterDelegate writerDelegate;
        private readonly ReaderDelegate readerDelegate;
    }
}