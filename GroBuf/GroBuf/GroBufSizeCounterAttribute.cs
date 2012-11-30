using System;

namespace GroBuf
{
    [AttributeUsage(AttributeTargets.Method)]
    public class GroBufSizeCounterAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class GroBufCustomSerializationAttribute : Attribute
    {
    }
}