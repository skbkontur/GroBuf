using System;
using System.Collections.Generic;

using GrEmit.Utils;

namespace GroBuf.Writers
{
    internal class CustomWriterBuilder : WriterBuilderBase
    {
        public CustomWriterBuilder(Type type, IGroBufCustomSerializer customSerializer)
            : base(type)
        {
            this.customSerializer = customSerializer;
        }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            context.SetFields(Type, new[] {new KeyValuePair<string, Type>("customSerializer_" + Type.Name + "_" + Guid.NewGuid(), typeof(IGroBufCustomSerializer))});
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var customSerializerField = context.Context.InitConstField(Type, 0, customSerializer);
            var il = context.Il;

            var length = context.LocalInt;
            var start = il.DeclareLocal(typeof(int));
            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Dup(); // stack: [ref index, index, index]
            il.Stloc(start); // start = index; stack: [ref index, index]
            il.Ldc_I4(5); // stack: [ref index, index, 5]
            il.Add(); // stack: [ref index, index + 5]
            il.Stind(typeof(int)); // index = index + 5; stack: []

            context.LoadField(customSerializerField); // stack: [customSerializer]
            context.LoadObj(); // stack: [customSerializer, obj]
            if(Type.IsValueType)
                il.Box(Type); // stack: [customSerializer, (object)obj]
            context.LoadWriteEmpty(); // stack: [customSerializer, (object)obj, writeEmpty]
            context.LoadResult(); // stack: [customSerializer, (object)obj, writeEmpty, result]
            context.LoadIndexByRef(); // stack: [customSerializer, (object)obj, writeEmpty, result, ref index]
            context.LoadContext(); // stack: [customSerializer, (object)obj, writeEmpty, result, ref index, context]
            int dummy = 0;
            var writeMethod = HackHelpers.GetMethodDefinition<IGroBufCustomSerializer>(serializer => serializer.Write(null, false, IntPtr.Zero, ref dummy, null));
            il.Call(writeMethod); // customSerializer.Write((object)obj, writeEmpty, result, ref index, context); stack: []

            context.LoadIndex(); // stack: [index]
            il.Ldloc(start); // stack: [index, start]
            il.Sub(); // stack: [index - start]
            il.Ldc_I4(5); // stack: [index - start, 5]
            il.Sub(); // stack: [index - start - 5]

            var writeLengthLabel = il.DefineLabel("writeLength");
            var doneLabel = il.DefineLabel("done");
            il.Dup(); // stack: [index - start - 5, index - start - 5]
            il.Stloc(length); // length = index - start - 5; stack: [length]
            il.Brtrue(writeLengthLabel); // if(length != 0) goto writeLength;

            context.LoadIndexByRef(); // stack: [ref index]
            il.Ldloc(start); // stack: [ref index, start]
            il.Stind(typeof(int)); // index = start
            context.WriteNull();

            il.MarkLabel(writeLengthLabel);
            context.LoadResult(); // stack: [result]
            il.Ldloc(start); // stack: [result, start]
            il.Add(); // stack: [result + start]
            il.Dup(); // stack: [result + start, result + start]
            il.Ldc_I4((int)GroBufTypeCode.CustomData); // stack: [result + start, result + start, TypeCode.Object]
            il.Stind(typeof(byte)); // *(result + start) = TypeCode.Object; stack: [result + start]
            il.Ldc_I4(1); // stack: [result + start, 1]
            il.Add(); // stack: [result + start + 1]
            il.Ldloc(length); // stack: [result + start + 1, length]
            il.Stind(typeof(int)); // *(int*)(result + start + 1) = length
            il.MarkLabel(doneLabel);
        }

        protected override bool IsReference { get { return false; } }

        private readonly IGroBufCustomSerializer customSerializer;
    }
}