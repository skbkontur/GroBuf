using System.Reflection;

namespace GroBuf.DataMembersExtracters
{
    public interface IDataMember
    {
        string Name { get; }
        MemberInfo Member { get; }
    }
}