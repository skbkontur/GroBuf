using GroBuf.DataMembersExtracters;

namespace GroBuf.Tests
{
    public class Serializer : SerializerBase
    {
        public Serializer()
            : base(new PropertiesExtractor())
        {
        }

        public Serializer(IDataMembersExtractor dataMembersExtractor, IGroBufCustomSerializerCollection customSerializerCollection = null, GroBufOptions options = GroBufOptions.None)
            : base(dataMembersExtractor, customSerializerCollection, options)
        {
        }
    }
}