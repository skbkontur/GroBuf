using System;
using System.Reflection;

namespace SKBKontur.GroBuf.DataMembersExtracters
{
    public class FieldsExtracter : IDataMembersExtracter
    {
        public MemberInfo[] GetMembers(Type type)
        {
            return type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        }
    }
}