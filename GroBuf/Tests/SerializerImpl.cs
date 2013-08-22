using GroBuf.DataMembersExtracters;

namespace GroBuf.Tests
{
    public class SerializerImpl : Serializer
    {
        public SerializerImpl()
            : base(new PropertiesExtractor())
        {
        }

        public SerializerImpl(IDataMembersExtractor dataMembersExtractor, IGroBufCustomSerializerCollection customSerializerCollection = null, GroBufOptions options = GroBufOptions.None)
            : base(dataMembersExtractor, customSerializerCollection, options)
        {
        }
    }
}