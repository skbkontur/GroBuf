using System;

namespace GroBuf
{
    public interface ISerializer
    {
        int GetSize<T>(T obj);
        void Serialize<T>(T obj, byte[] result, ref int index);
        byte[] Serialize<T>(T obj);
        T Deserialize<T>(byte[] data);
        T Deserialize<T>(byte[] data, ref int index);
        void Merge<T>(T from, ref T to);
        TTo ChangeType<TFrom, TTo>(TFrom obj);
        T Copy<T>(T obj);

        int GetSize(Type type, object obj);
        void Serialize(Type type, object obj, byte[] result, ref int index);
        byte[] Serialize(Type type, object obj);
        object Deserialize(Type type, byte[] data);
        object Deserialize(Type type, byte[] data, ref int index);
        object ChangeType(Type from, Type to, object obj);
        object Copy(Type type, object obj);
    }
}