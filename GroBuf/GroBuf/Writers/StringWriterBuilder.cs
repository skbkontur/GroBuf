using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace GroBuf.Writers
{
    internal class StringWriterBuilder : WriterBuilderBase
    {
        public StringWriterBuilder()
            : base(typeof(string))
        {
        }

        protected override unsafe void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var length = context.LocalInt;
            context.LoadObj(); // stack: [obj]
            ILGenerator il = context.Il;
            il.Emit(OpCodes.Call, lengthPropertyGetter); // stack: [obj.Length]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [obj.Length, 1]
            il.Emit(OpCodes.Shl); // stack: [obj.Length << 1]
            il.Emit(OpCodes.Stloc, length); // length = obj.Length << 1
            context.WriteTypeCode(GroBufTypeCode.String);
            context.GoToCurrentLocation(); // stack: [&result[index]]
            il.Emit(OpCodes.Ldloc, length); // stack: [&result[index], length]
            il.Emit(OpCodes.Stind_I4); // result[index] = length
            context.IncreaseIndexBy4(); // index = index + 4

            var allDoneLabel = il.DefineLabel();
            il.Emit(OpCodes.Ldloc, length); // stack: [length]
            il.Emit(OpCodes.Brfalse, allDoneLabel);

            context.GoToCurrentLocation(); // stack: [&result[index]]
            var str = il.DeclareLocal(typeof(string), true);
            context.LoadObj(); // stack: [&result[index], obj]
            il.Emit(OpCodes.Stloc, str); // str = obj
            il.Emit(OpCodes.Ldloc, str); // stack: [&result[index], str]
            il.Emit(OpCodes.Conv_I); // stack: [&result[index], (int)str]
            il.Emit(OpCodes.Ldc_I4, RuntimeHelpers.OffsetToStringData); // stack: [&result[index], (IntPtr)str, offset]
            il.Emit(OpCodes.Add); // stack: [&result[index], (IntPtr)str + offset]
            il.Emit(OpCodes.Ldloc, length); // stack: [&result[index], (IntPtr)str + offset, length]
            if (sizeof(IntPtr) == 8)
                il.Emit(OpCodes.Unaligned, 1L);
            il.Emit(OpCodes.Cpblk); // &result[index] = str
            il.Emit(OpCodes.Ldc_I4_0); // stack: [0]
            il.Emit(OpCodes.Conv_U); // stack: [(uint)0]
            il.Emit(OpCodes.Stloc, str); // str = (uint)0;

            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Emit(OpCodes.Ldloc, length); // stack: [ref index, index, length]
            il.Emit(OpCodes.Add); // stack: [ref index, index + length]
            il.Emit(OpCodes.Stind_I4); // index = index + length

            il.MarkLabel(allDoneLabel);
        }

        private static readonly MethodInfo lengthPropertyGetter = ((PropertyInfo)((MemberExpression)((Expression<Func<string, int>>)(s => s.Length)).Body).Member).GetGetMethod();
    }
}