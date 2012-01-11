namespace GroBuf.Counterz
{
    internal interface ISizeCounterCollection
    {
        ISizeCounterBuilder<T> GetSizeCounter<T>();
    }
}