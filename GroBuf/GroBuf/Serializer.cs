using GroBuf.DataMembersExtracters;

namespace GroBuf
{
    public class Serializer : ISerializer
    {
        private readonly SerializerImpl impl;

        public Serializer()
        {
            impl = new SerializerImpl(new PropertiesExtracter());
        }

        public int GetSize<T>(T obj)
        {
            return impl.GetSize(obj);
        }

        public void Serialize<T>(T obj, byte[] result, int index)
        {
            impl.Serialize(obj, result, index);
        }

        public byte[] Serialize<T>(T obj)
        {
            return impl.Serialize(obj);
        }

        public T Deserialize<T>(byte[] data)
        {
            return impl.Deserialize<T>(data);
        }

        public TTo ChangeType<TFrom, TTo>(TFrom obj)
        {
            return impl.ChangeType<TFrom, TTo>(obj);
        }

        public T Copy<T>(T obj)
        {
            return impl.Copy(obj);
        }
    }
}