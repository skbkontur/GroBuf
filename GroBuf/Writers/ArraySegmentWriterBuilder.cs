using System;
using System.Reflection;

using GrEmit;

namespace GroBuf.Writers
{
    internal class ArraySegmentWriterBuilder : WriterBuilderBase
    {
        public ArraySegmentWriterBuilder(Type type)
            : base(type)
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(ArraySegment<>)))
                throw new InvalidOperationException("An array segment expected but was '" + Type + "'");
            elementType = Type.GetGenericArguments()[0];
            arrayField = Type.GetField("_array", BindingFlags.Instance | BindingFlags.NonPublic);
            offsetField = Type.GetField("_offset", BindingFlags.Instance | BindingFlags.NonPublic);
            countField = Type.GetField("_count", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        protected override bool CheckEmpty(WriterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            var il = context.Il;
            context.LoadObjByRef(); // stack: [ref obj]
            il.Ldfld(arrayField); // stack: [obj._array]
            if(context.Context.GroBufWriter.Options.HasFlag(GroBufOptions.WriteEmptyObjects))
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

        protected override bool IsReference { get { return false; } }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            context.BuildConstants(elementType);
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            context.WriteTypeCode(GroBufTypeCode.Array);
            var length = il.DeclareLocal(typeof(int));
            context.LoadObjByRef(); // stack: [ref obj]
            il.Ldfld(countField); // stack: [obj._count]
            il.Stloc(length); // length = obj._count
            context.LoadIndex(); // stack: [index]
            var start = context.LocalInt;
            il.Stloc(start); // start = index
            il.Ldc_I4(8);
            context.AssertLength(); // 8 = data size + array length
            context.IncreaseIndexBy4(); // index = index + 4
            context.GoToCurrentLocation(); // stack: [&result[index]]
            il.Ldloc(length); // stack: [&result[index], length]
            il.Stind(typeof(int)); // *(int*)&result[index] = length; stack: []
            context.IncreaseIndexBy4(); // index = index + 4

            var writeDataLengthLabel = il.DefineLabel("writeDataLength");
            il.Ldloc(length); // stack: [length]
            il.Brfalse(writeDataLengthLabel); // if(length == 0) goto writeDataLength; stack: []

            var i = il.DeclareLocal(typeof(int));
            context.LoadObjByRef(); // stack: [ref obj]
            il.Ldfld(offsetField); // stack: [obj._offset]
            il.Dup(); // stack: [obj._offset, obj._offset]
            il.Stloc(i); // i = obj._offset; stack: [obj._offset]
            il.Ldloc(length); // stack: [obj._offset, length]
            il.Add(); // stack: [obj._offset + length]
            il.Stloc(length); // length = obj._offset + length; stack: []
            var array = il.DeclareLocal(elementType.MakeArrayType());
            context.LoadObjByRef(); // stack: [ref obj]
            il.Ldfld(arrayField); // stack: [obj._array]
            il.Stloc(array); // array = obj._array; stack: []
            var cycleStart = il.DefineLabel("cycleStart");
            il.MarkLabel(cycleStart);

            il.Ldloc(array); // stack: [array]
            il.Ldloc(i); // stack: [array, i]
            il.Ldelem(elementType);
            il.Ldc_I4(1); // stack: [array[i], true]
            context.LoadResult(); // stack: [array[i], true, result]
            context.LoadIndexByRef(); // stack: [array[i], true, result, ref index]
            context.LoadContext(); // stack: [array[i], true, result, ref index, context]
            context.CallWriter(elementType); // writer(array[i], true, result, ref index, context); stack: []
            il.Ldloc(length); // stack: [length]
            il.Ldloc(i); // stack: [length, i]
            il.Ldc_I4(1); // stack: [length, i, 1]
            il.Add(); // stack: [length, i + 1]
            il.Dup(); // stack: [length, i + 1, i + 1]
            il.Stloc(i); // i = i + 1; stack: [length, i]
            il.Bgt(cycleStart, false); // if(length > i) goto cycleStart; stack: []

            il.MarkLabel(writeDataLengthLabel);
            context.LoadResult(); // stack: [result]
            il.Ldloc(start); // stack: [result, start]
            il.Add(); // stack: [result + start]
            context.LoadIndex(); // stack: [result + start, index]
            il.Ldloc(start); // stack: [result + start, index, start]
            il.Sub(); // stack: [result + start, index - start]
            il.Ldc_I4(4); // stack: [result + start, index - start, 4]
            il.Sub(); // stack: [result + start, index - start - 4]
            il.Stind(typeof(int)); // *(int*)(result + start) = index - start - 4
        }

        private readonly Type elementType;
        private readonly FieldInfo arrayField;
        private readonly FieldInfo offsetField;
        private readonly FieldInfo countField;
    }
}