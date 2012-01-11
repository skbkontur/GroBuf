using System;

namespace GroBuf
{
    public class DataCorruptedException: Exception
    {
        public DataCorruptedException(string message)
            : base(message)
        {
        }
    }
}