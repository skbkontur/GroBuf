using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GroBuf.DataMembersExtracters
{
    public class AllFieldsExtractor : IDataMembersExtractor
    {
        public Tuple<string, MemberInfo>[] GetMembers(Type type)
        {
            var result = new List<Tuple<string, MemberInfo>>();
            GetMembers(type, result);
            return result.ToArray();
        }

        private static void GetMembers(Type type, List<Tuple<string, MemberInfo>> members)
        {
            members.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly).Select(info => new Tuple<string, MemberInfo>(info.Name, info)));
            if(type.BaseType != typeof(object))
                GetMembers(type.BaseType, members);
        }
    }
}