using System;

namespace GroBuf.DataMembersExtracters
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class GroboMemberAttribute: Attribute
    {
        public GroboMemberAttribute(string name)
        {
            Name = name;
        }

        public GroboMemberAttribute(ulong id)
        {
            Id = id;
        }

        public ulong? Id { get; private set; }
        public string Name { get; private set; }
    }
}