using System;
using System.Reflection;
using System.Reflection.Emit;

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
            ILGenerator il = context.Il;
            context.LoadField(sizeCounterField); // stack: [sizeCounterDelegate]
            context.LoadObj(); // stack: [sizeCounterDelegate, obj]
            if(Type.IsValueType)
                il.Emit(OpCodes.Box, Type); // stack: [sizeCounterDelegate, (object)obj]
            context.LoadWriteEmpty(); // stack: [sizeCounterDelegate, (object)obj, writeEmpty]
            il.Emit(OpCodes.Call, typeof(SizeCounterDelegate).GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance)); // stack: [sizeCounterDelegate.Invoke((object)obj, writeEmpty) = size]

            var countLength = il.DefineLabel();
            il.Emit(OpCodes.Dup); // stack: [size, size]
            il.Emit(OpCodes.Brtrue, countLength); // if(size != 0) goto countLength; stack: [size]
            il.Emit(OpCodes.Pop); // stack: []
            context.ReturnForNull();
            il.Emit(OpCodes.Ret);
            il.MarkLabel(countLength);
            il.Emit(OpCodes.Ldc_I4_5); // stack: [size, 5]
            il.Emit(OpCodes.Add); // stack: [size + 5]
        }

        private readonly MethodInfo sizeCounter;
    }
}