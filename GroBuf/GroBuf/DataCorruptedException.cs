using System;

namespace SKBKontur.GroBuf
{
    public class DataCorruptedException: Exception
    {
        public DataCorruptedException(string message)
            : base(message)
        {
        }
    }
}