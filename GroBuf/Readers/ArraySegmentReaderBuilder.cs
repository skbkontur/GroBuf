using System;
using System.Reflection;

namespace GroBuf.Readers
{
    internal class ArraySegmentReaderBuilder : ReaderBuilderBase
    {
        public ArraySegmentReaderBuilder(Type type)
            : base(type)
        {
            if (!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(ArraySegment<>)))
                throw new InvalidOperationException("An array segment expected but was '" + Type + "'");
            elementType = Type.GetGenericArguments()[0];
            arrayField = Type.GetField("_array", BindingFlags.Instance | BindingFlags.NonPublic);
            offsetField = Type.GetField("_offset", BindingFlags.Instance | BindingFlags.NonPublic);
            countField = Type.GetField("_count", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
            context.BuildConstants(elementType);
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            var il = context.Il;

            il.Ldloc(context.TypeCode); // stack: [type code]
            il.Ldc_I4((int)GroBufTypeCode.Array); // stack: [type code, GroBufTypeCode.Array]
            var tryReadArrayElementLabel = il.DefineLabel("tryReadArrayElement");
            il.Bne_Un(tryReadArrayElementLabel); // if(type code != GroBufTypeCode.Array) goto tryReadArrayElement; stack: []

            context.IncreaseIndexBy1();
            var length = context.Length;

            il.Ldc_I4(4);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Ldind(typeof(uint)); // stack: [data length]
            context.IncreaseIndexBy4(); // index = index + 4; stack: [data length]

            context.AssertLength();
            il.Ldc_I4(4);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Ldind(typeof(uint)); // stack: [array length]
            context.IncreaseIndexBy4(); // index = index + 4; stack: [array length]
            il.Stloc(length); // length = array length; stack: []

            var array = il.DeclareLocal(elementType.MakeArrayType());
            context.LoadResultByRef(); // stack: [ref result]
            il.Dup(); // stack: [ref result, ref result]
            il.Ldc_I4(0); // stack: [ref result, ref result, 0]
            il.Stfld(offsetField); // result._offset = 0; stack: [ref result]
            il.Dup(); // stack: [ref result, ref result]
            il.Ldloc(length); // stack: [ref result, ref result, length]
            il.Stfld(countField); // result._count = length; stack: [ref result]
            il.Ldloc(length); // stack: [ref result, length]
            il.Newarr(elementType); // stack: [ref result, new type[length]]
            il.Dup();
            il.Stloc(array); // array = new type[length]; stack: [ref result]
            il.Stfld(arrayField); // result._array = array; stack: []

            il.Ldloc(length); // stack: [length]
            var doneLabel = il.DefineLabel("done");
            il.Brfalse(doneLabel); // if(length == 0) goto allDone; stack: []
            var i = il.DeclareLocal(typeof(uint));
            il.Ldc_I4(0); // stack: [0]
            il.Stloc(i); // i = 0; stack: []
            var cycleStartLabel = il.DefineLabel("cycleStart");
            il.MarkLabel(cycleStartLabel);

            context.LoadData(); // stack: [pinnedData]
            context.LoadIndexByRef(); // stack: [pinnedData, ref index]
            il.Ldloc(array); // stack: [pinnedData, ref index, array]
            il.Ldloc(i); // stack: [pinnedData, ref index, array, i]

            il.Ldelema(elementType); // stack: [pinnedData, ref index, ref array[i]]
            context.LoadContext(); // stack: [pinnedData, ref index, ref array[i], context]

            context.CallReader(elementType); // reader(pinnedData, ref index, ref array[i], context); stack: []
            il.Ldloc(i); // stack: [i]
            il.Ldc_I4(1); // stack: [i, 1]
            il.Add(); // stack: [i + 1]
            il.Dup(); // stack: [i + 1, i + 1]
            il.Stloc(i); // i = i + 1; stack: [i]
            il.Ldloc(length); // stack: [i, length]
            il.Blt(cycleStartLabel, true); // if(i < length) goto cycleStart
            il.Br(doneLabel);

            il.MarkLabel(tryReadArrayElementLabel);
            context.LoadResultByRef(); // stack: [ref result]
            il.Dup(); // stack: [ref result, ref result]
            il.Ldc_I4(0); // stack: [ref result, ref result, 0]
            il.Stfld(offsetField); // result._offset = 0; stack: [ref result]
            il.Dup(); // stack: [ref result, ref result]
            il.Ldc_I4(1); // stack: [ref result, ref result, 1]
            il.Stfld(countField); // result._count = 1; stack: [ref result]
            il.Ldc_I4(1); // stack: [ref result, 1]
            il.Newarr(elementType); // stack: [ref result, new type[1]]
            il.Dup();
            il.Stloc(array); // array = new type[1]; stack: [ref result]
            il.Stfld(arrayField); // result._array = array; stack: []

            context.LoadData(); // stack: [pinnedData]
            context.LoadIndexByRef(); // stack: [pinnedData, ref index]
            il.Ldloc(array); // stack: [pinnedData, ref index, array]
            il.Ldc_I4(0); // stack: [pinnedData, ref index, array, 0]

            il.Ldelema(elementType); // stack: [pinnedData, ref index, ref array[0]]
            context.LoadContext(); // stack: [pinnedData, ref index, ref array[0], context]

            context.CallReader(elementType); // reader(pinnedData, ref index, ref array[0], context); stack: []

            il.MarkLabel(doneLabel); // stack: []
        }

        protected override bool IsReference { get { return false; } }

        private readonly Type elementType;
        private readonly FieldInfo arrayField;
        private readonly FieldInfo offsetField;
        private readonly FieldInfo countField;
    }
}