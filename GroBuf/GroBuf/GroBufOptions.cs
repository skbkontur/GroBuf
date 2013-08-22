using System;

namespace GroBuf
{
    [Flags]
    public enum GroBufOptions
    {
        None = 0,
        WriteEmptyObjects = 1,
        MergeOnRead = 2
    }
}