using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace GroBuf.Writers
{
    internal class StringWriterBuilder : WriterBuilderBase<string>
    {
        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var length = context.LocalInt;
            context.LoadObj(); // stack: [obj]
            context.Il.Emit(OpCodes.Call, GetLengthPropertyGetter()); // stack: [obj.Length]
            context.Il.Emit(OpCodes.Ldc_I4_1); // stack: [obj.Length, 1]
            context.Il.Emit(OpCodes.Shl); // stack: [obj.Length << 1]
            context.Il.Emit(OpCodes.Dup); // stack: [obj.Length << 1, obj.Length << 1]
            context.Il.Emit(OpCodes.Ldc_I4_5); // stack: [obj.Length << 1, obj.Length << 1, 5]
            context.Il.Emit(OpCodes.Add); // stack: [obj.Length << 1, obj.Length << 1 + 5]
            context.EnsureSize();
            context.Il.Emit(OpCodes.Stloc, length); // length = obj.Length << 1
            context.WriteTypeCode(GroBufTypeCode.String);
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.Il.Emit(OpCodes.Ldloc, length); // stack: [&result[index], length]
            context.Il.Emit(OpCodes.Stind_I4); // result[index] = length
            context.IncreaseIndexBy4(); // index = index + 4

            context.GoToCurrentLocation(); // stack: [&result[index]]
            var str = context.Il.DeclareLocal(typeof(string), true);
            context.LoadObj(); // stack: [&result[index], obj]
            context.Il.Emit(OpCodes.Stloc, str); // str = obj
            context.Il.Emit(OpCodes.Ldloc, str); // stack: [&result[index], str]
            context.Il.Emit(OpCodes.Conv_I); // stack: [&result[index], (int)str]
            context.Il.Emit(OpCodes.Ldc_I4, RuntimeHelpers.OffsetToStringData); // stack: [&result[index], (IntPtr)str, offset]
            context.Il.Emit(OpCodes.Add); // stack: [&result[index], (IntPtr)str + offset]
            context.Il.Emit(OpCodes.Ldloc, length); // stack: [&result[index], (IntPtr)str + offset, length]
            context.Il.Emit(OpCodes.Unaligned, 1L);
            context.Il.Emit(OpCodes.Cpblk); // &result[index] = str
            context.Il.Emit(OpCodes.Ldc_I4_0); // stack: [0]
            context.Il.Emit(OpCodes.Conv_U); // stack: [(uint)0]
            context.Il.Emit(OpCodes.Stloc, str); // str = (uint)0;

            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            context.Il.Emit(OpCodes.Ldloc, length); // stack: [ref index, index, length]
            context.Il.Emit(OpCodes.Add); // stack: [ref index, index + length]
            context.Il.Emit(OpCodes.Stind_I4); // index = index + length
        }

        protected override bool CheckEmpty(WriterMethodBuilderContext context, Label notEmptyLabel)
        {
            context.LoadObj(); // stack: [obj]
            context.Il.Emit(OpCodes.Call, GetIsNullOrEmptyMethod()); // stack: [string.isNullOrEmpty(obj)]
            context.Il.Emit(OpCodes.Brfalse, notEmptyLabel); // if(!string.isNullOrEmpty(obj)) goto notEmpty;
            return true;
        }

        private static MethodInfo GetIsNullOrEmptyMethod()
        {
            return isNullOrEmptyMethod ?? (isNullOrEmptyMethod = ((MethodCallExpression)((Expression<Func<string, bool>>)(s => string.IsNullOrEmpty(s))).Body).Method);
        }

        private static MethodInfo GetLengthPropertyGetter()
        {
            return lengthPropertyGetter ?? (lengthPropertyGetter = ((PropertyInfo)((MemberExpression)((Expression<Func<string, int>>)(s => s.Length)).Body).Member).GetGetMethod());
        }

        private static MethodInfo isNullOrEmptyMethod;
        private static MethodInfo lengthPropertyGetter;
    }
}