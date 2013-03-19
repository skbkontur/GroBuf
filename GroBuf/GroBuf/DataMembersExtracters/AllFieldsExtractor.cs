using System;
using System.Reflection;

namespace GroBuf.DataMembersExtracters
{
    public class AllFieldsExtractor: IDataMembersExtractor
    {
        public MemberInfo[] GetMembers(Type type)
        {
            return type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }
    }
}