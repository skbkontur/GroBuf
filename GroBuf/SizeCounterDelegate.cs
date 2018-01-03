namespace GroBuf
{
    public delegate int SizeCounterDelegate<in T>(T obj, bool writeEmpty, WriterContext context);

    public delegate int SizeCounterDelegate(object obj, bool writeEmpty, WriterContext context);
}