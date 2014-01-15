using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace GroBuf.DataMembersExtracters
{
    public class DataMembersByAttributeExtractor : IDataMembersExtractor
    {
        private readonly bool extractOnlyWithAttribute;

        public DataMembersByAttributeExtractor(bool extractOnlyWithAttribute)
        {
            this.extractOnlyWithAttribute = extractOnlyWithAttribute;
        }

        public IDataMember[] GetMembers(Type type)
        {
            return (from property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    where property.CanRead && property.GetGetMethod(true).GetParameters().Length == 0 && property.CanWrite && property.GetSetMethod(true).GetParameters().Length == 1
                    let dataMemberAttribute = property.GetCustomAttributes(typeof(DataMemberAttribute), false).FirstOrDefault() as DataMemberAttribute
                    where !extractOnlyWithAttribute || dataMemberAttribute != null
                    select new DataMember(dataMemberAttribute == null || string.IsNullOrEmpty(dataMemberAttribute.Name) ? property.Name : dataMemberAttribute.Name, property)).ToArray();
        }
    }
}