using System.Reflection;

namespace GroBuf.DataMembersExtractors
{
    public interface IDataMember
    {
        ulong? Id { get; }
        string Name { get; }
        MemberInfo Member { get; }
    }
}