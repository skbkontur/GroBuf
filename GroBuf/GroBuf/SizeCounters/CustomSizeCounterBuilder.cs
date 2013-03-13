using System;
using System.Reflection;

namespace GroBuf.SizeCounters
{
    internal class CustomSizeCounterBuilder : SizeCounterBuilderBase
    {
        public CustomSizeCounterBuilder(Type type, MethodInfo sizeCounter)
            : base(type)
        {
            this.sizeCounter = sizeCounter;
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var groBufWriter = context.Context.GroBufWriter;
            Func<Type, SizeCounterDelegate> sizeCountersFactory = type => (obj, writeEmpty) => groBufWriter.GetSize(type, obj, writeEmpty);
            var sizeCounterDelegate = (SizeCounterDelegate)sizeCounter.Invoke(null, new[] {sizeCountersFactory});
            var sizeCounterField = context.Context.BuildConstField("sizeCounter_" + Type.Name + "_" + Guid.NewGuid(), sizeCounterDelegate);
            var il = context.Il;
            context.LoadField(sizeCounterField); // stack: [sizeCounterDelegate]
            context.LoadObj(); // stack: [sizeCounterDelegate, obj]
            if(Type.IsValueType)
                il.Box(Type); // stack: [sizeCounterDelegate, (object)obj]
            context.LoadWriteEmpty(); // stack: [sizeCounterDelegate, (object)obj, writeEmpty]
            il.Call(typeof(SizeCounterDelegate).GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance), typeof(SizeCounterDelegate)); // stack: [sizeCounterDelegate.Invoke((object)obj, writeEmpty) = size]

            var countLengthLabel = il.DefineLabel("countLength");
            il.Dup(); // stack: [size, size]
            il.Brtrue(countLengthLabel); // if(size != 0) goto countLength; stack: [size]
            il.Pop(); // stack: []
            context.ReturnForNull();
            il.Ret();
            il.MarkLabel(countLengthLabel);
            il.Ldc_I4(5); // stack: [size, 5]
            il.Add(); // stack: [size + 5]
        }

        private readonly MethodInfo sizeCounter;
    }
}