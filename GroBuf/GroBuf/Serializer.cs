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

        public void Serialize(Type obj, IntPtr result, ref int index)
        {
            impl.Serialize(obj, result, ref index);
        }

        public T Deserialize<T>(byte[] data)
        {
            return impl.Deserialize<T>(data);
        }

        public T Deserialize<T>(byte[] data, ref int index)
        {
            return impl.Deserialize<T>(data, ref index);
        }

        public T Deserialize<T>(byte[] data, int length)
        {
            return impl.Deserialize<T>(data, length);
        }

        public T Deserialize<T>(byte[] data, ref int index, int length)
        {
            return impl.Deserialize<T>(data, ref index, length);
        }

        public T Deserialize<T>(IntPtr data, ref int index, int length)
        {
            return impl.Deserialize<T>(data, ref index, length);
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

        public void Serialize(Type type, object obj, IntPtr result, ref int index)
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

        public object Deserialize(Type type, byte[] data, int length)
        {
            return impl.Deserialize(type, data, length);
        }

        public object Deserialize(Type type, byte[] data, ref int index, int length)
        {
            return impl.Deserialize(type, data, ref index, length);
        }

        public object Deserialize(Type type, IntPtr result, ref int index, int length)
        {
            return impl.Deserialize(type, result, ref index, length);
        }

        public object ChangeType(Type from, Type to, object obj)
        {
            return impl.ChangeType(from, to, obj);
        }

        public object Copy(Type type, object obj)
        {
            return impl.Copy(type, obj);
        }

        private readonly SerializerImpl impl;
    }
}