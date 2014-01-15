using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GroBuf.DataMembersExtracters
{
    public class FieldsExtractor : IDataMembersExtractor
    {
        public IDataMember[] GetMembers(Type type)
        {
            var result = new List<IDataMember>();
            GetMembers(type, result);
            return result.ToArray();
        }

        private static void GetMembers(Type type, List<IDataMember> members)
        {
            members.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Select(info => new DataMember(info.Name, info)));
            if (type.BaseType != typeof(object))
                GetMembers(type.BaseType, members);
        }
    }
}