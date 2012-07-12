using System;

namespace GroBuf
{
    public interface ISerializer
    {
        object Deserialize(Type type, byte[] data);
        byte[] Serialize(Type type, object o);

        int GetSize<T>(T obj);
        void Serialize<T>(T obj, byte[] result, ref int index);
        byte[] Serialize<T>(T obj);
        T Deserialize<T>(byte[] data);
        T Deserialize<T>(byte[] data, ref int index);
        void Merge<T>(T from, ref T to);
        TTo ChangeType<TFrom, TTo>(TFrom obj);
        T Copy<T>(T obj);
    }
}