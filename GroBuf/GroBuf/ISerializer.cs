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
        T Deserialize<T>(byte[] data, int length);
        T Deserialize<T>(byte[] data, ref int index, int length);
        T Deserialize<T>(IntPtr data, ref int index, int length);
        void Merge<T>(T from, ref T to);
        TTo ChangeType<TFrom, TTo>(TFrom obj);
        T Copy<T>(T obj);

        int GetSize(Type type, object obj);
        void Serialize(Type type, object obj, byte[] result, ref int index);
        void Serialize(Type type, object obj, IntPtr result, ref int index, int length);
        byte[] Serialize(Type type, object obj);
        object Deserialize(Type type, byte[] data);
        object Deserialize(Type type, byte[] data, ref int index);
        object Deserialize(Type type, byte[] data, int length);
        object Deserialize(Type type, byte[] data, ref int index, int length);
        object Deserialize(Type type, IntPtr result, ref int index, int length);
        object ChangeType(Type from, Type to, object obj);
        object Copy(Type type, object obj);

        string DebugView(byte[] data);
    }
}