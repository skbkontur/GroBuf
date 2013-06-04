namespace GroBuf
{
    public delegate int SizeCounterDelegate<in T>(T obj, bool writeEmpty);

    public delegate int SizeCounterDelegate(object obj, bool writeEmpty);
}