using System;

namespace GroBuf
{
    public class GroBufLazy<T> where T: class
    {
        public GroBufLazy()
            : this(null)
        {
        }

        public GroBufLazy(T value)
        {
            this.value = value;
            raw = false;
        }

        public GroBufLazy(byte[] data, Func<byte[], T> reader)
        {
            this.data = data;
            this.reader = reader;
            raw = true;
        }

        public T Value
        {
            get
            {
                if(!raw)
                    return value;
                value = reader(data);
                raw = false;
                return value;
            }
            set
            {
                raw = false;
                this.value = value;
            }
        }

        private bool raw;
        private T value;
        private readonly Func<byte[], T> reader;
        private readonly byte[] data;
    }
}