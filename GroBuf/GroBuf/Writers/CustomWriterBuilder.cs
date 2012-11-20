using System;
using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.Writers
{
    internal class CustomWriterBuilder : WriterBuilderBase
    {
        public CustomWriterBuilder(Type type, MethodInfo writer)
            : base(type)
        {
            this.writer = writer;
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var groBufWriter = context.Context.GroBufWriter;
            Func<Type, WriterDelegate> writersFactory = type => ((object o, bool empty, IntPtr result, ref int index) => groBufWriter.Write(type, o, empty, result, ref index));
            var writerDelegate = (WriterDelegate)writer.Invoke(null, new[] {writersFactory});
            var writerField = context.Context.BuildConstField("writer_" + Type.Name + "_" + Guid.NewGuid(), writerDelegate);
            var il = context.Il;

            var length = context.LocalInt;
            var start = il.DeclareLocal(typeof(int));
            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Emit(OpCodes.Dup); // stack: [ref index, index, index]
            il.Emit(OpCodes.Stloc, start); // start = index; stack: [ref index, index]
            il.Emit(OpCodes.Ldc_I4_5); // stack: [ref index, index, 5]
            il.Emit(OpCodes.Add); // stack: [ref index, index + 5]
            il.Emit(OpCodes.Stind_I4); // index = index + 5; stack: []

            context.LoadField(writerField); // stack: [writerDelegate]
            context.LoadObj(); // stack: [writerDelegate, obj]
            if(Type.IsValueType)
                il.Emit(OpCodes.Box, Type); // stack: [writerDelegate, (object)obj]
            context.LoadWriteEmpty(); // stack: [writerDelegate, (object)obj, writeEmpty]
            context.LoadResult(); // stack: [writerDelegate, (object)obj, writeEmpty, result]
            context.LoadIndexByRef(); // stack: [writerDelegate, (object)obj, writeEmpty, result, ref index]
            il.Emit(OpCodes.Call, typeof(WriterDelegate).GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance)); // stack: [writerDelegate.Invoke((object)obj, writeEmpty, result, ref index)]

            context.LoadIndex(); // stack: [index]
            il.Emit(OpCodes.Ldloc, start); // stack: [index, start]
            il.Emit(OpCodes.Sub); // stack: [index - start]
            il.Emit(OpCodes.Ldc_I4_5); // stack: [index - start, 5]
            il.Emit(OpCodes.Sub); // stack: [index - start - 5]

            var writeLengthLabel = il.DefineLabel();
            var allDoneLabel = il.DefineLabel();
            il.Emit(OpCodes.Dup); // stack: [index - start - 5, index - start - 5]
            il.Emit(OpCodes.Stloc, length); // length = index - start - 5; stack: [length]
            il.Emit(OpCodes.Brtrue, writeLengthLabel); // if(length != 0) goto writeLength;

            context.LoadIndexByRef(); // stack: [ref index]
            il.Emit(OpCodes.Ldloc, start); // stack: [ref index, start]
            il.Emit(OpCodes.Stind_I4); // index = start
            context.WriteNull();

            il.MarkLabel(writeLengthLabel);
            context.LoadResult(); // stack: [result]
            il.Emit(OpCodes.Ldloc, start); // stack: [result, start]
            il.Emit(OpCodes.Add); // stack: [result + start]
            il.Emit(OpCodes.Dup); // stack: [result + start, result + start]
            il.Emit(OpCodes.Ldc_I4, (int)GroBufTypeCode.CustomData); // stack: [result + start, result + start, TypeCode.Object]
            il.Emit(OpCodes.Stind_I1); // *(result + start) = TypeCode.Object; stack: [result + start]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [result + start, 1]
            il.Emit(OpCodes.Add); // stack: [result + start + 1]
            il.Emit(OpCodes.Ldloc, length); // stack: [result + start + 1, length]
            il.Emit(OpCodes.Stind_I4); // *(int*)(result + start + 1) = length
            il.MarkLabel(allDoneLabel);
        }

        private readonly MethodInfo writer;
    }
}