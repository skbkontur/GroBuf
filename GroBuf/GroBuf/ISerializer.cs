﻿namespace GroBuf
{
    public interface ISerializer
    {
        int GetSize<T>(T obj);
        void Serialize<T>(T obj, byte[] result, int index);
        byte[] Serialize<T>(T obj);
        T Deserialize<T>(byte[] data);
        TTo ChangeType<TFrom, TTo>(TFrom obj);
    }
}