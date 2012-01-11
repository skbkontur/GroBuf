using System;
using System.Reflection;
using System.Linq;

namespace GroBuf.DataMembersExtracters
{
    public class PropertiesExtracter : IDataMembersExtracter
    {
        public MemberInfo[] GetMembers(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.GetGetMethod() != null && property.GetSetMethod() != null).ToArray();
        }
    }
}