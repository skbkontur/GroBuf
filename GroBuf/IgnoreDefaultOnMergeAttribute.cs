using System;

namespace GroBuf
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class IgnoreDefaultOnMergeAttribute : Attribute
    {
    }
}