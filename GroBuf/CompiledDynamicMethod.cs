using System;
using System.Reflection.Emit;

namespace GroBuf
{
    internal class CompiledDynamicMethod
    {
        public DynamicMethod Method { get; set; }
        public int Index { get; set; }
        public IntPtr Pointer { get; set; }
        public Delegate Delegate { get; set; }
    }
}