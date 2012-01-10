using System;
using System.Reflection;

namespace SKBKontur.GroBuf.DataMembersExtracters
{
    public interface IDataMembersExtracter
    {
        MemberInfo[] GetMembers(Type type);
    }
}