using System;

using GroBuf.DataMembersExtracters;

namespace GroBuf
{
    public class Serializer : ISerializer
    {
        public Serializer()
        {
            impl = new SerializerImpl(new PropertiesExtractor());
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

        public int GetSize(Type type, object obj)
        {
            return impl.GetSize(type, obj);
        }

        public void Serialize(Type type, object obj, byte[] result, ref int index)
        {
            impl.Serialize(type, obj, result, ref index);
        }

        public byte[] Serialize(Type type, object obj)
        {
            return impl.Serialize(type, obj);
        }

        public object Deserialize(Type type, byte[] data)
        {
            return impl.Deserialize(type, data);
        }

        public object Deserialize(Type type, byte[] data, ref int index)
        {
            return impl.Deserialize(type, data, ref index);
        }

        private readonly SerializerImpl impl;
    }
}