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
}