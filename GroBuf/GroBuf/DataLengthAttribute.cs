using System;

namespace GroBuf
{
    internal class DataLengthAttribute : Attribute
    {
        public DataLengthAttribute(int length)
        {
            Length = length;
        }

        public int Length { get; private set; }
    }
}