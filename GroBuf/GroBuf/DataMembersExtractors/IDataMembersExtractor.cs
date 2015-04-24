using System;

namespace GroBuf.DataMembersExtractors
{
    public interface IDataMembersExtractor
    {
        IDataMember[] GetMembers(Type type);
    }
}