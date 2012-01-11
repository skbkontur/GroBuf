using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.Counterz
{
    internal class StringSizeCounterBuilder : SizeCounterBuilderBase<string>
    {
        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            context.LoadObj(); // stack: [obj]
            context.Il.Emit(OpCodes.Call, GetLengthPropertyGetter()); // stack: [obj.Length]
            context.Il.Emit(OpCodes.Ldc_I4_1); // stack: [obj.Length, 1]
            context.Il.Emit(OpCodes.Shl); // stack: [obj.Length << 1]
            context.Il.Emit(OpCodes.Ldc_I4_5); // stack: [obj.Length << 1, 5]
            context.Il.Emit(OpCodes.Add); // stack: [obj.Length << 1 + 5]
        }

        protected override bool CheckEmpty(SizeCounterMethodBuilderContext context, Label notEmptyLabel)
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