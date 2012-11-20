using System;
using System.Reflection;

namespace GroBuf.DataMembersExtracters
{
    public interface IDataMembersExtractor
    {
        MemberInfo[] GetMembers(Type type);
    }
}