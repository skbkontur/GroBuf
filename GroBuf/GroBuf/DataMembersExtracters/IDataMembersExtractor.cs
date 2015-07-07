using System;

namespace GroBuf.DataMembersExtracters
{
    public interface IDataMembersExtractor
    {
        IDataMember[] GetMembers(Type type);
    }
}