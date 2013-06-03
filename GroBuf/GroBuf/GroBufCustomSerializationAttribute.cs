using System;

namespace GroBuf
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class GroBufCustomSerializationAttribute : Attribute
    {
        public GroBufCustomSerializationAttribute()
        {
        }

        public GroBufCustomSerializationAttribute(Type customSerializerType)
        {
            CustomSerializerType = customSerializerType;
        }

        public Type CustomSerializerType { get; private set; }
    }

    public interface IGroBufCustomSerializerCollection
    {
        IGroBufCustomSerializer Get(Type declaredType, Func<Type, IGroBufCustomSerializer> factory);
    }

    public class EmptyGroBufCustomSerializerCollection : IGroBufCustomSerializerCollection
    {
        public IGroBufCustomSerializer Get(Type declaredType, Func<Type, IGroBufCustomSerializer> factory)
        {
            return null;
        }
    }
}