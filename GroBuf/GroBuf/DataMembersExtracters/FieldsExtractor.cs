using System;
using System.Reflection;

namespace GroBuf.DataMembersExtracters
{
    public class FieldsExtractor : IDataMembersExtractor
    {
        public MemberInfo[] GetMembers(Type type)
        {
            return type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        }
    }
}