using System;
using System.Collections.Generic;
using System.Reflection;

using GrEmit;

namespace GroBuf.Writers
{
    internal class PrimitivesListWriterBuilder : WriterBuilderBase
    {
        public PrimitivesListWriterBuilder(Type type)
            : base(type)
        {
            if (!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(List<>)))
                throw new InvalidOperationException("Expected list but was '" + Type + "'");
            elementType = Type.GetGenericArguments()[0];
            if (!elementType.IsPrimitive)
                throw new NotSupportedException("List of primitive type expected but was '" + Type + "'");
        }

        protected override bool CheckEmpty(WriterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            context.LoadObj(); // stack: [obj]
            if (context.Context.GroBufWriter.Options.HasFlag(GroBufOptions.WriteEmptyObjects))
                context.Il.Brtrue(notEmptyLabel); // if(obj != null) goto notEmpty;
            else
            {
                var emptyLabel = context.Il.DefineLabel("empty");
                context.Il.Brfalse(emptyLabel); // if(obj == null) goto empty;
                context.LoadObj(); // stack: [obj]
                context.Il.Ldfld(Type.GetField("_size", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.Count]
                context.Il.Brtrue(notEmptyLabel); // if(obj.Count != 0) goto notEmpty;
                context.Il.MarkLabel(emptyLabel);
            }
            return true;
        }

        protected override bool IsReference => true;

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            context.BuildConstants(elementType);
        }

        protected override unsafe void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            context.WriteTypeCode(GroBufTypeCodeMap.GetTypeCode(elementType.MakeArrayType()));
            il.Ldc_I4(4);
            context.AssertLength();
            var size = il.DeclareLocal(typeof(int));
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObj(); // stack: [&result[index], obj]
            il.Ldfld(Type.GetField("_size", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [&result[index], obj.Count]
            CountArraySize(elementType, il); // stack: [&result[index], obj size]
            il.Dup(); // stack: [&result[index], obj size, obj size]
            il.Stloc(size); // size = obj size; stack: [&result[index], obj size]
            il.Stind(typeof(int)); // result[index] = size; stack: []
            context.IncreaseIndexBy4(); // index = index + 4; stack: []

            il.Ldloc(size); // stack: [size]
            var doneLabel = il.DefineLabel("done");
            il.Brfalse(doneLabel); // if(size == 0) goto done; stack: []

            il.Ldloc(size);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObj(); // stack: [&result[index], obj]
            il.Ldfld(Type.GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [&result[index], obj._items]
            il.Ldc_I4(0); // stack: [&result[index], obj._items, 0]
            il.Ldelema(elementType); // stack: [&result[index], &obj._items[0]]
            var arr = il.DeclareLocal(elementType.MakeByRefType(), true);
            il.Stloc(arr); // arr = &obj._items[0]; stack: [&result[index]]
            il.Ldloc(arr); // stack: [&result[index], arr]
            il.Ldloc(size); // stack: [&result[index], arr, size]
            il.Cpblk(); // &result[index] = arr
            il.FreePinnedLocal(arr); // arr = null; stack: []
            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Ldloc(size); // stack: [ref index, index, size]
            il.Add(); // stack: [ref index, index + size]
            il.Stind(typeof(int)); // index = index + size

            il.MarkLabel(doneLabel);
        }

        private static void CountArraySize(Type elementType, GroboIL il)
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