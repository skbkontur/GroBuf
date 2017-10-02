using System;
using System.Linq;
using System.Reflection;

namespace GroBuf.DataMembersExtracters
{
    public class DataMembersByAttributeExtractor : IDataMembersExtractor
    {
        public DataMembersByAttributeExtractor(bool extractOnlyWithAttribute)
        {
            this.extractOnlyWithAttribute = extractOnlyWithAttribute;
        }

        public IDataMember[] GetMembers(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead && property.GetGetMethod(true).GetParameters().Length == 0 && property.TryGetWritableMemberInfo().TryGetWritableMemberInfo() != null)
                .Select(x =>
                            {
                                var result = DataMember.TryCreateByDataMemberAttribute(x);
                                return extractOnlyWithAttribute ? result : (result ?? DataMember.CreateByName(x));
                            }).Where(x => x != null).ToArray();
        }

        private readonly bool extractOnlyWithAttribute;
    }
}