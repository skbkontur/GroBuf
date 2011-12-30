namespace SKBKontur.GroBuf
{
    public class Serializer : ISerializer
    {
        public byte[] Serialize<T>(T obj)
        {
            return writer.Write(obj);
        }

        public T Deserialize<T>(byte[] data)
        {
            return reader.Read<T>(data);
        }

        private readonly GroBufWriter writer = new GroBufWriter();
        private readonly GroBufReader reader = new GroBufReader();
    }
}