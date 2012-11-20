using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.SizeCounters
{
    internal class StringSizeCounterBuilder : SizeCounterBuilderBase
    {
        public StringSizeCounterBuilder()
            : base(typeof(string))
        {
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            context.LoadObj(); // stack: [obj]
            context.Il.Emit(OpCodes.Call, lengthPropertyGetter); // stack: [obj.Length]
            context.Il.Emit(OpCodes.Ldc_I4_1); // stack: [obj.Length, 1]
            context.Il.Emit(OpCodes.Shl); // stack: [obj.Length << 1]
            context.Il.Emit(OpCodes.Ldc_I4_5); // stack: [obj.Length << 1, 5]
            context.Il.Emit(OpCodes.Add); // stack: [obj.Length << 1 + 5]
        }

        private static readonly MethodInfo lengthPropertyGetter = ((PropertyInfo)((MemberExpression)((Expression<Func<string, int>>)(s => s.Length)).Body).Member).GetGetMethod();
    }
}