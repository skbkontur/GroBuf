using System;

namespace GroBuf
{
    public interface IGroBufCustomSerializerCollection
    {
        IGroBufCustomSerializer Get(Type declaredType, Func<Type, IGroBufCustomSerializer> factory);
    }
}