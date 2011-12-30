using System;

namespace SKBKontur.GroBuf
{
    public class DataLengthAttribute : Attribute
    {
        public DataLengthAttribute(int length)
        {
            Length = length;
        }

        public int Length { get; private set; }
    }
}