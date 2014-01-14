using System;
using System.Reflection;

namespace GroBuf.DataMembersExtracters
{
    public interface IDataMembersExtractor
    {
        Tuple<string, MemberInfo>[] GetMembers(Type type);
    }
}