using GroBuf.DataMembersExtracters;

namespace GroBuf
{
    public class Serializer : ISerializer
    {
        public Serializer()
        {
            impl = new SerializerImpl(new PropertiesExtracter());
        }

        public int GetSize<T>(T obj)
        {
            return impl.GetSize(obj);
        }

        public void Serialize<T>(T obj, byte[] result, ref int index)
        {
            impl.Serialize(obj, result, ref index);
        }

        public byte[] Serialize<T>(T obj)
        {
            return impl.Serialize(obj);
        }

        public T Deserialize<T>(byte[] data)
        {
            return impl.Deserialize<T>(data);
        }

        public T Deserialize<T>(byte[] data, ref int index)
        {
            return impl.Deserialize<T>(data, ref index);
        }

        public void Merge<T>(T from, ref T to)
        {
            impl.Merge(from, ref to);
        }

        public TTo ChangeType<TFrom, TTo>(TFrom obj)
        {
            return impl.ChangeType<TFrom, TTo>(obj);
        }

        public T Copy<T>(T obj)
        {
            return impl.Copy(obj);
        }

        private readonly SerializerImpl impl;
    }
}