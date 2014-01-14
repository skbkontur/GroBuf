using System;
using System.Collections.Generic;
using System.Reflection;

namespace GroBuf.DataMembersExtracters
{
    public class AllFieldsExtractor : IDataMembersExtractor
    {
        public MemberInfo[] GetMembers(Type type)
        {
            var result = new List<MemberInfo>();
            GetMembers(type, result);
            return result.ToArray();
        }

        private static void GetMembers(Type type, List<MemberInfo> members)
        {
            members.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly));
            if(type.BaseType != typeof(object))
                GetMembers(type.BaseType, members);
        }
    }
}