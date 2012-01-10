using System;
using System.Reflection;
using System.Linq;

namespace SKBKontur.GroBuf.DataMembersExtracters
{
    public class PropertiesExtracter : IDataMembersExtracter
    {
        public MemberInfo[] GetMembers(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.CanRead && property.CanWrite).ToArray();
        }
    }
}