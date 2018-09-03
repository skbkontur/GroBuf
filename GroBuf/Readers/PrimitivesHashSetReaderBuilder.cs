using System;
using System.Collections.Generic;
using System.Reflection;

using GrEmit;

namespace GroBuf.Readers
{
    internal class PrimitivesHashSetReaderBuilder : ReaderBuilderBase
    {
        public PrimitivesHashSetReaderBuilder(Type type)
            : base(type)
        {
            if (!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(HashSet<>)))
                throw new InvalidOperationException("HashSet expected but was '" + Type + "'");
            elementType = Type.GetGenericArguments()[0];
            if (!elementType.IsPrimitive)
                throw new NotSupportedException("HashSet of primitive type expected but was '" + Type + "'");
        }

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
            context.BuildConstants(elementType);
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            context.IncreaseIndexBy1();
            context.AssertTypeCode(GroBufTypeCodeMap.GetTypeCode(elementType.MakeArrayType()));

            var il = context.Il;
            var size = il.DeclareLocal(typeof(int));

            il.Ldc_I4(4);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Ldind(typeof(uint)); // stack: [data length]
            il.Dup(); // stack: [data length, data length]
            il.Stloc(size); // size = data length; stack: [data length]
            context.IncreaseIndexBy4(); // index = index + 4; stack: [data length]
            context.AssertLength();

            var count = context.Length;
            il.Ldloc(size); // stack: [size]
            CountArrayLength(elementType, il); // stack: [array length]
            il.Stloc(count); // count = array length

            context.LoadResultByRef(); // stack: [ref result]
            il.Newobj(Type.GetConstructor(Type.EmptyTypes)); // stack: [ref result, new HashSet() = hashSet]
            il.Dup(); // stack: [ref result, hashSet, hashSet]
            il.Ldloc(count); // stack: [ref result, hashSet, hashSet, count]
            il.Call(Type.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic)); // hashSet.Initialize(count); stack: [ref result, hashSet]
            il.Stind(Type); // result = hashSet; stack: []

            il.Ldloc(count);
            var doneLabel = il.DefineLabel("done");
            il.Brfalse(doneLabel); // if(count == 0) goto allDone; stack: []

            var i = il.DeclareLocal(typeof(int));
            il.Ldc_I4(0); // stack: [0]
            il.Stloc(i); // i = 0; stack: []
            var cycleStartLabel = il.DefineLabel("cycleStart");
            il.MarkLabel(cycleStartLabel);
            context.LoadResult(Type); // stack: [result]
            context.GoToCurrentLocation(); // stack: [result, &data[index]]
            il.Ldloc(i); // stack: [result, &data[index], i]
            MakeShift(elementType, il); // stack: [result, &data[index], i << x]
            il.Add(); // stack: [result, current]
            il.Ldind(elementType); // stack: [result, *current]
            il.Call(Type.GetMethod("Add")); // stack: [result.Add(*current)]
            il.Pop(); // stack: []

            il.Ldloc(count); // stack: [count]
            il.Ldloc(i); // stack: [current, count, i]
            il.Ldc_I4(1); // stack: [current, count, i, 1]
            il.Add(); // stack: [current, count, i + 1]
            il.Dup(); // stack: [current, count, i + 1, i + 1]
            il.Stloc(i); // i = i + 1; stack: [current, count, i]
            il.Bgt(cycleStartLabel, false); // if(count > i) goto cycleStart; stack: [current]

            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Ldloc(size); // stack: [ref index, index, size]
            il.Add(); // stack: [ref index, index + size]
            il.Stind(typeof(int)); // index = index + size

            il.MarkLabel(doneLabel); // stack: []
        }

        protected override bool IsReference { get { return true; } }

        private static void CountArrayLength(Type elementType, GroboIL il)
        {
            var typeCode = GroBufTypeCodeMap.GetTypeCode(elementType);
            switch (typeCode)
            {
            case GroBufTypeCode.Int8:
            case GroBufTypeCode.UInt8:
            case GroBufTypeCode.Boolean:
                break;
            case GroBufTypeCode.Int16:
            case GroBufTypeCode.UInt16:
                il.Ldc_I4(1);
                il.Shr(false);
                break;
            case GroBufTypeCode.Int32:
            case GroBufTypeCode.UInt32:
                il.Ldc_I4(2);
                il.Shr(false);
                break;
            case GroBufTypeCode.Int64:
            case GroBufTypeCode.UInt64:
                il.Ldc_I4(3);
                il.Shr(false);
                break;
            case GroBufTypeCode.Single:
                il.Ldc_I4(2);
                il.Shr(false);
                break;
            case GroBufTypeCode.Double:
                il.Ldc_I4(3);
                il.Shr(false);
                break;
            default:
                throw new NotSupportedException("Type '" + elementType + "' is not supported");
            }
        }

        private static void MakeShift(Type elementType, GroboIL il)
        {
            var typeCode = GroBufTypeCodeMap.GetTypeCode(elementType);
            switch (typeCode)
            {
            case GroBufTypeCode.Int8:
            case GroBufTypeCode.UInt8:
            case GroBufTypeCode.Boolean:
                break;
            case GroBufTypeCode.Int16:
            case GroBufTypeCode.UInt16:
                il.Ldc_I4(1);
                il.Shl();
                break;
            case GroBufTypeCode.Int32:
            case GroBufTypeCode.UInt32:
                il.Ldc_I4(2);
                il.Shl();
                break;
            case GroBufTypeCode.Int64:
            case GroBufTypeCode.UInt64:
                il.Ldc_I4(3);
                il.Shl();
                break;
            case GroBufTypeCode.Single:
                il.Ldc_I4(2);
                il.Shl();
                break;
            case GroBufTypeCode.Double:
                il.Ldc_I4(3);
                il.Shl();
                break;
            default:
                throw new NotSupportedException("Type '" + elementType + "' is not supported");
            }
        }

        private readonly Type elementType;
    }
}