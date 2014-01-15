using System;
using System.Linq;
using System.Reflection;

namespace GroBuf.DataMembersExtracters
{
    public class PropertiesExtractor : IDataMembersExtractor
    {
        public IDataMember[] GetMembers(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.CanRead && property.GetGetMethod(true).GetParameters().Length == 0 && property.CanWrite && property.GetSetMethod(true).GetParameters().Length == 1).Select(info => new DataMember(info.Name, info)).ToArray();
        }
    }
}