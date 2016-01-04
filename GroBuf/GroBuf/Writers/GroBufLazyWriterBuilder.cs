using System;
using System.Reflection;

using GrEmit;

namespace GroBuf.Writers
{
    internal class GroBufLazyWriterBuilder : WriterBuilderBase
    {
        public GroBufLazyWriterBuilder(Type type)
            : base(type)
        {
            if (!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(GroBufLazy<>)))
                throw new InvalidOperationException("Expected GroBufLazy but was " + Type);
        }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            context.BuildConstants(Type.GetGenericArguments()[0]);
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;

            context.LoadObj(); // stack: [obj]
            il.Ldfld(Type.GetField("raw", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.raw]
            var writeRawLabel = il.DefineLabel("writeRaw");
            il.Brtrue(writeRawLabel);

            context.LoadObj(); // stack: [obj]
            il.Ldfld(Type.GetField("value", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.value]
            context.LoadWriteEmpty(); // stack: [obj.value, writeEmpty]
            context.LoadResult(); // stack: [obj.value, writeEmpty, result]
            context.LoadIndexByRef(); // stack: [obj.value, writeEmpty, result, ref index]
            context.LoadContext(); // stack: [obj.value, writeEmpty, result, ref index, context]
            context.CallWriter(Type.GetGenericArguments()[0]); // writer(obj.value, writeEmpty, result, ref index, context)
            il.Ret();

            il.MarkLabel(writeRawLabel);
            var data = il.DeclareLocal(typeof(byte).MakeByRefType(), true);
            var length = il.DeclareLocal(typeof(int));
            context.LoadObj(); // stack: [obj]
            il.Ldfld(Type.GetField("data", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.data]
            il.Dup(); // stack: [obj.data, obj.data]
            il.Ldlen(); // stack: [obj.data, obj.data.Length]
            il.Stloc(length); // length = obj.data.Length; stack: [obj.data]
            il.Ldc_I4(0); // stack: [obj.data, 0]
            il.Ldelema(typeof(byte)); // stack: [&obj.data[0]]
            il.Stloc(data); // data = &obj.data; stack: []
            il.Ldloc(length);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&result[index]]
            il.Ldloc(data); // stack: [&result[index], data]
            il.Ldloc(length); // stack: [&result[index], data, data.Length]
            il.Cpblk(); // result[index] = data; stack: []
            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Ldloc(length); // stack: [ref index, index, data.Length]
            il.Add(); // stack: [ref index, index + data.Length]
            il.Stind(typeof(int)); // index = index + data.Length; stack: []
            il.Ldnull();
            il.Stloc(data);
        }

        protected override bool CheckEmpty(WriterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            var il = context.Il;
            var emptyLabel = il.DefineLabel("empty");
            context.LoadObj(); // stack: [obj]
            il.Brfalse(emptyLabel); // if(obj == null) goto empty; stack: []
            context.LoadObj(); // stack: [obj]
            il.Ldfld(Type.GetField("raw", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.raw]
            il.Brtrue(notEmptyLabel); // if(obj.raw) goto notEmpty; stack: []
            context.LoadObj(); // stack: [obj]
            il.Ldfld(Type.GetField("value", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.value]
            il.Brtrue(notEmptyLabel); // if(obj.value != null) goto notEmpty; stack: []
            il.MarkLabel(emptyLabel);
            return true;
        }

        protected override bool IsReference { get { return false; } }
    }
}