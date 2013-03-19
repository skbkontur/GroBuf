using System;
using System.Linq.Expressions;
using System.Reflection;

namespace GroBuf.SizeCounters
{
    internal class StringSizeCounterBuilder : SizeCounterBuilderBase
    {
        public StringSizeCounterBuilder()
            : base(typeof(string))
        {
        }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;
            context.LoadObj(); // stack: [obj]
            il.Call(lengthPropertyGetter); // stack: [obj.Length]
            il.Ldc_I4(1); // stack: [obj.Length, 1]
            il.Shl(); // stack: [obj.Length << 1]
            il.Ldc_I4(5); // stack: [obj.Length << 1, 5]
            il.Add(); // stack: [obj.Length << 1 + 5]
        }

        private static readonly MethodInfo lengthPropertyGetter = ((PropertyInfo)((MemberExpression)((Expression<Func<string, int>>)(s => s.Length)).Body).Member).GetGetMethod();
    }
}