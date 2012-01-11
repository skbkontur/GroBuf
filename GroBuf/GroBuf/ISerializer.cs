using System;

namespace GroBuf
{
    public interface ISerializer
    {
        int GetSize<T>(T obj);
        void Serialize<T>(T obj, byte[] result, int index);
        void Serialize<T>(T obj, IntPtr result);
        byte[] Serialize<T>(T obj);
        T Deserialize<T>(byte[] data);
    }
}