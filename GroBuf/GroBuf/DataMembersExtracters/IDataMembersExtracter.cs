using System;
using System.Reflection;

namespace GroBuf.DataMembersExtracters
{
    public interface IDataMembersExtracter
    {
        MemberInfo[] GetMembers(Type type);
    }
}