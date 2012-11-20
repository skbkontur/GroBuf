using System;

namespace GroBuf.Writers
{
    internal interface IWriterCollection
    {
        IWriterBuilder GetWriterBuilder(Type type);
    }
}