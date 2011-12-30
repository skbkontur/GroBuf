namespace SKBKontur.GroBuf.Writers
{
    internal unsafe delegate void WriterDelegate<in T>(T obj, bool writeEmpty, ref byte[] result, ref int index, ref byte* pinnedResult);
    internal unsafe delegate void WriterDelegate<in T, in TParam>(T obj, bool writeEmpty, ref byte[] result, ref int index, ref byte* pinnedResult, TParam param);
}