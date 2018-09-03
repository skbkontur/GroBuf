using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace GroBuf.DataMembersExtracters
{
    public class DataMember : IDataMember
    {
        private DataMember(ulong? id, MemberInfo member)
        {
            Id = id;
            Member = member;
        }

        private DataMember(string name, MemberInfo member)
        {
            Name = name;
            Member = member;
        }

        public ulong? Id { get; private set; }

        public string Name { get; private set; }

        public MemberInfo Member { get; private set; }

        public override string ToString()
        {
            return string.Format("Id: {0}, Name: {1}, Member.Name: {2}", Id, Name, Member.Name);
        }

        public static DataMember Create(MemberInfo member)
        {
            return TryCreateByGroboAttribute(member) ?? TryCreateByDataMemberAttribute(member) ?? CreateByName(member);
        }

        public static DataMember CreateByName(MemberInfo member)
        {
            return new DataMember(member.Name, member);
        }

        public static DataMember TryCreateByDataMemberAttribute(MemberInfo member)
        {
            var dataMemberAttribute = (DataMemberAttribute)member.GetCustomAttributes(typeof(DataMemberAttribute), false).SingleOrDefault();
            if (dataMemberAttribute == null)
                return null;
            var name = string.IsNullOrWhiteSpace(dataMemberAttribute.Name) ? member.Name : dataMemberAttribute.Name;
            return new DataMember(name, member);
        }

        private static DataMember TryCreateByGroboAttribute(MemberInfo member)
        {
            var groboAttribute = (GroboMemberAttribute)member.GetCustomAttributes(typeof(GroboMemberAttribute), false).SingleOrDefault();
            if (groboAttribute == null)
                return null;
            if (groboAttribute.Id.HasValue)
                return new DataMember(groboAttribute.Id.Value, member);
            if (!string.IsNullOrWhiteSpace(groboAttribute.Name))
                return new DataMember(groboAttribute.Name, member);
            throw new InvalidOperationException("Empty grobo name of member '" + member.DeclaringType.Name + "." + member.Name + "'");
        }
    }
}