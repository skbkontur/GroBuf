using System;

namespace GroBuf
{
    internal class LeafTypeAttribute : Attribute
    {
        public LeafTypeAttribute(Type type)
        {
            Type = type;
        }

        public Type Type { get; private set; }
    }
}