namespace GroBuf
{
    public interface ISerializer
    {
        int GetSize<T>(T obj);
        void Serialize<T>(T obj, byte[] result, int index);
        byte[] Serialize<T>(T obj);
        T Deserialize<T>(byte[] data);
        void Merge<T>(byte[] data, ref T result);
        TTo ChangeType<TFrom, TTo>(TFrom obj);
        T Copy<T>(T obj);
    }
}