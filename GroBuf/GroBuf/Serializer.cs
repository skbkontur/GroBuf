using GroBuf.DataMembersExtracters;

namespace GroBuf
{
    public class Serializer : ISerializer
    {
        public Serializer()
            : this(new PropertiesExtracter())
        {
        }

        public Serializer(IDataMembersExtracter dataMembersExtracter)
        {
            writer = new GroBufWriter(dataMembersExtracter);
            reader = new GroBufReader(dataMembersExtracter);
        }

        public int GetSize<T>(T obj)
        {
            return writer.GetSize(obj);
        }

        public void Serialize<T>(T obj, byte[] result, int index)
        {
            writer.Write(obj, result, index);
        }

        public byte[] Serialize<T>(T obj)
        {
            return writer.Write(obj);
        }

        public T Deserialize<T>(byte[] data)
        {
            return reader.Read<T>(data);
        }

        private readonly GroBufWriter writer;
        private readonly GroBufReader reader;
    }
}