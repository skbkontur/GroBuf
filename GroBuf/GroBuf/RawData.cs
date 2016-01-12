using System;

namespace GroBuf
{
    public class RawData<T>
    {
        private readonly long serializerId;
        private readonly byte[] data;
        private readonly Func<byte[], T> reader;

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
    }
}