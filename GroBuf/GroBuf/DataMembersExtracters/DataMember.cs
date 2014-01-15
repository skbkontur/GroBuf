using System.Reflection;

namespace GroBuf.DataMembersExtracters
{
    public class DataMember : IDataMember
    {
        public DataMember(MemberInfo member)
            : this(member.Name, member)
        {
        }

        public DataMember(string name, MemberInfo member)
        {
            Name = name;
            Member = member;
        }

        public string Name { get; private set; }

        public MemberInfo Member { get; private set; }
    }
}