using System;

namespace GroBuf
{
    public class EmptyGroBufCustomSerializerCollection : IGroBufCustomSerializerCollection
    {
        public IGroBufCustomSerializer Get(Type declaredType, Func<Type, IGroBufCustomSerializer> factory)
        {
            return null;
        }
    }
}