namespace SKBKontur.GroBuf.Readers
{
    internal unsafe delegate T ReaderDelegate<out T>(byte* pinnedData, ref int index, int dataLength);
    internal unsafe delegate T ReaderDelegate<out T, in TParam>(byte* pinnedData, ref int index, int dataLength, TParam param);
    internal unsafe delegate T ReaderDelegate<out T, in TParam1, in TParam2>(byte* pinnedData, ref int index, int dataLength, TParam1 param1, TParam2 param2);
}