using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GroBuf.Writers
{
    internal class StringWriterBuilder : WriterBuilderBase
    {
        public StringWriterBuilder()
            : base(typeof(string))
        {
        }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
        }

        protected override unsafe void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var length = context.LocalInt;
            context.LoadObj(); // stack: [obj]
            var il = context.Il;
            il.Call(lengthPropertyGetter); // stack: [obj.Length]
            il.Ldc_I4(1); // stack: [obj.Length, 1]
            il.Shl(); // stack: [obj.Length << 1]
            il.Stloc(length); // length = obj.Length << 1
            context.WriteTypeCode(GroBufTypeCode.String);
            il.Ldc_I4(4);
            context.AssertLength();
            context.GoToCurrentLocation(); // stack: [&result[index]]
            il.Ldloc(length); // stack: [&result[index], length]
            il.Stind(typeof(int)); // result[index] = length
            context.IncreaseIndexBy4(); // index = index + 4

            var doneLabel = il.DefineLabel("done");
            il.Ldloc(length); // stack: [length]
            il.Brfalse(doneLabel);

            il.Ldloc(length);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&result[index]]
            var str = il.DeclareLocal(typeof(string), true);
            context.LoadObj(); // stack: [&result[index], obj]
            il.Stloc(str); // str = obj
            il.Ldloc(str); // stack: [&result[index], str]
            il.Conv<IntPtr>(); // stack: [&result[index], (IntPtr)str]
            il.Ldc_I4(RuntimeHelpers.OffsetToStringData); // stack: [&result[index], (IntPtr)str, offset]
            il.Add(); // stack: [&result[index], (IntPtr)str + offset]
            il.Ldloc(length); // stack: [&result[index], (IntPtr)str + offset, length]
            il.Cpblk(); // &result[index] = str
            il.FreePinnedLocal(str); // str = null; stack: []

            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Ldloc(length); // stack: [ref index, index, length]
            il.Add(); // stack: [ref index, index + length]
            il.Stind(typeof(int)); // index = index + length

            il.MarkLabel(doneLabel);
        }

        protected override bool IsReference { get { return true; } }

        private static readonly MethodInfo lengthPropertyGetter = ((PropertyInfo)((MemberExpression)((Expression<Func<string, int>>)(s => s.Length)).Body).Member).GetGetMethod();
    }
}