namespace GroBuf.SizeCounters
{
    internal interface ISizeCounterCollection
    {
        ISizeCounterBuilder<T> GetSizeCounterBuilder<T>();
    }
}