using System;

namespace GroBuf.Readers
{
    internal interface IReaderCollection
    {
        IReaderBuilder GetReaderBuilder(Type type, bool ignoreCustomSerialization);
    }
}