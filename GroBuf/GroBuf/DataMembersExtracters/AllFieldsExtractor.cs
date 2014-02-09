using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GroBuf.DataMembersExtracters
{
    public class AllFieldsExtractor : IDataMembersExtractor
    {
        public IDataMember[] GetMembers(Type type)
        {
            var result = new List<IDataMember>();
            GetMembers(type, result);
            return result.ToArray();
        }

        private static void GetMembers(Type type, List<IDataMember> members)
        {
            if(type == null || type == typeof(object))
                return;
            members.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly).Select(info => new DataMember(info.Name, info)));
            GetMembers(type.BaseType, members);
        }
    }
}