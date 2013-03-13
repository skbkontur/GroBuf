using System;
using System.Reflection;

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
            il.Dup(); // stack: [ref index, index, index]
            il.Stloc(start); // start = index; stack: [ref index, index]
            il.Ldc_I4(5); // stack: [ref index, index, 5]
            il.Add(); // stack: [ref index, index + 5]
            il.Stind(typeof(int)); // index = index + 5; stack: []

            context.LoadField(writerField); // stack: [writerDelegate]
            context.LoadObj(); // stack: [writerDelegate, obj]
            if(Type.IsValueType)
                il.Box(Type); // stack: [writerDelegate, (object)obj]
            context.LoadWriteEmpty(); // stack: [writerDelegate, (object)obj, writeEmpty]
            context.LoadResult(); // stack: [writerDelegate, (object)obj, writeEmpty, result]
            context.LoadIndexByRef(); // stack: [writerDelegate, (object)obj, writeEmpty, result, ref index]
            il.Call(typeof(WriterDelegate).GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance), typeof(WriterDelegate)); // stack: [writerDelegate.Invoke((object)obj, writeEmpty, result, ref index)]

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

        private readonly MethodInfo writer;
    }
}