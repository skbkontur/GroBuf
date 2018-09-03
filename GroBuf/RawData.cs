using System;

namespace GroBuf
{
    internal class RawData<T>
    {
        public RawData(long serializerId, byte[] data, Func<byte[], T> reader)
        {
            this.serializerId = serializerId;
            this.data = data;
            this.reader = reader;
        }

        public T GetValue()
        {
            return reader(data);
        }

        private readonly long serializerId;
        private readonly byte[] data;
        private readonly Func<byte[], T> reader;
    }
}