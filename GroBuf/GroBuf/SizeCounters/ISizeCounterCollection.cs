namespace GroBuf.SizeCounters
{
    internal interface ISizeCounterCollection
    {
        ISizeCounterBuilder<T> GetSizeCounter<T>();
    }
}