using System.Reflection;

namespace GroBuf.DataMembersExtracters
{
    public interface IDataMember
    {
        ulong? Id { get; }
        string Name { get; }
        MemberInfo Member { get; }
    }
}