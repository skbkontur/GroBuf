using System;
using System.Linq;

namespace GroBuf.DataMembersExtracters
{
    public class CompositeExtractor : IDataMembersExtractor
    {
        public CompositeExtractor(params IDataMembersExtractor[] dataMembersExtractors)
        {
            this.dataMembersExtractors = dataMembersExtractors;
        }

        public IDataMember[] GetMembers(Type type)
        {
            return dataMembersExtractors.SelectMany(x => x.GetMembers(type))
                                        .GroupBy(x => Tuple.Create(x.Id, x.Name))
                                        .Select(x => x.First())
                                        .ToArray();
        }

        private readonly IDataMembersExtractor[] dataMembersExtractors;
    }
}