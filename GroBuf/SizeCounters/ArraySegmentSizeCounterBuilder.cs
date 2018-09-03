using System;
using System.Reflection;

using GrEmit;

namespace GroBuf.SizeCounters
{
    internal class ArraySegmentSizeCounterBuilder : SizeCounterBuilderBase
    {
        public ArraySegmentSizeCounterBuilder(Type type)
            : base(type)
        {
            if (!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(ArraySegment<>)))
                throw new InvalidOperationException("An array segment expected but was '" + Type + "'");
            elementType = Type.GetGenericArguments()[0];
            arrayField = Type.GetField("_array", BindingFlags.Instance | BindingFlags.NonPublic);
            offsetField = Type.GetField("_offset", BindingFlags.Instance | BindingFlags.NonPublic);
            countField = Type.GetField("_count", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
            context.BuildConstants(elementType);
        }

        protected override bool CheckEmpty(SizeCounterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            var il = context.Il;
            context.LoadObjByRef(); // stack: [ref obj]
            il.Ldfld(arrayField); // stack: [obj._array]
            if (context.Context.GroBufWriter.Options.HasFlag(GroBufOptions.WriteEmptyObjects))
                il.Brtrue(notEmptyLabel); // if(obj._array != null) goto notEmpty;
            else
            {
                var emptyLabel = il.DefineLabel("empty");
                il.Brfalse(emptyLabel); // if(obj._array == null) goto empty;
                context.LoadObjByRef(); // stack: [ref obj]
                il.Ldfld(countField); // stack: [obj._count]
                il.Brtrue(notEmptyLabel); // if(obj._count != 0) goto notEmpty;
                il.MarkLabel(emptyLabel);
            }
            return true;
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;
            il.Ldc_I4(9); // stack: [9 = size] 9 = type code + data length + array length

            var doneLabel = il.DefineLabel("done");
            context.LoadObjByRef(); // stack: [size, ref obj]
            il.Ldfld(countField); // stack: [size, obj._count]
            il.Brfalse(doneLabel); // if(obj._count == 0) goto done; stack: [size]
            var i = il.DeclareLocal(typeof(int));
            var end = il.DeclareLocal(typeof(int));
            context.LoadObjByRef(); // stack: [size, ref obj]
            il.Dup(); // stack: [size, ref obj, ref obj]
            il.Ldfld(offsetField); // stack: [size, ref obj, obj._offset]
            il.Stloc(i); // i = obj._offset; stack: [size, ref obj]
            il.Ldfld(countField); // stack: [size, obj._count]
            il.Ldloc(i); // stack: [size, obj._count, obj._offset]
            il.Add(); // stack: [size, obj._count + obj._offset]
            il.Stloc(end); // end = obj._count + obj._offset; stack: [size]
            var array = il.DeclareLocal(elementType.MakeArrayType());
            context.LoadObjByRef(); // stack: [size, ref obj]
            il.Ldfld(arrayField); // stack: [size, obj._array]
            il.Stloc(array); // array = obj._array; stack: [size]
            var cycleStartLabel = il.DefineLabel("cycleStart");
            il.MarkLabel(cycleStartLabel);
            il.Ldloc(array); // stack: [size, array]
            il.Ldloc(i); // stack: [size, array, i]
            il.Ldelem(elementType); // stack: [size, array[i]]
            il.Ldc_I4(1); // stack: [size, array[i], true]
            context.LoadContext(); // stack: [size, array[i], true, context]
            context.CallSizeCounter(elementType); // stack: [size, writer(array[i], true, context) = itemSize]
            il.Add(); // stack: [size + itemSize]
            il.Ldloc(end); // stack: [size, end]
            il.Ldloc(i); // stack: [size, end, i]
            il.Ldc_I4(1); // stack: [size, end, i, 1]
            il.Add(); // stack: [size, end, i + 1]
            il.Dup(); // stack: [size, end, i + 1, i + 1]
            il.Stloc(i); // i = i + 1; stack: [size, end, i]
            il.Bgt(cycleStartLabel, false); // if(end > i) goto cycleStart; stack: [size]
            il.MarkLabel(doneLabel);
        }

        protected override bool IsReference => false;

        private readonly Type elementType;
        private readonly FieldInfo arrayField;
        private readonly FieldInfo offsetField;
        private readonly FieldInfo countField;
    }
}