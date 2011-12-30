using System;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Writers
{
    internal class NullableWriterBuilder<T> : WriterBuilderWithOneParam<T, Delegate>
    {
        public NullableWriterBuilder(IWriterCollection writerCollection)
            : base(writerCollection)
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Nullable<>)))
                throw new InvalidOperationException("Expected nullable but was " + Type);
        }

        protected override Delegate WriteNotEmpty(WriterBuilderContext context)
        {
            var il = context.Il;
            context.LoadAdditionalParam(0); // stack: [writer]
            context.LoadObjByRef(); // stack: [writer, &obj]
            il.Emit(OpCodes.Call, Type.GetProperty("Value").GetGetMethod()); // stack: [writer, obj.Value]
            context.LoadWriteEmpty(); // stack: [writer, obj.Value, writeEmpty]
            context.LoadResultByRef(); // stack: [writer, obj.Value, writeEmpty, ref result]
            context.LoadIndexByRef(); // stack: [writer, obj.Value, writeEmpty, ref result, ref index]
            context.LoadPinnedResultByRef(); // stack: [writer, obj.Value, writeEmpty, ref result, ref index, ref pinnedResult]
            var writer = GetWriter(Type.GetGenericArguments()[0]);
            il.Emit(OpCodes.Call, writer.GetType().GetMethod("Invoke")); // writer(obj.Value, writeEmpty, ref result, ref index, ref pinnedResult)
            return writer;
        }

        protected override bool CheckEmpty(WriterBuilderContext context, Label notEmptyLabel)
        {
            context.LoadObjByRef(); // stack: [&obj]
            context.Il.Emit(OpCodes.Call, Type.GetProperty("HasValue").GetGetMethod()); // stack: obj.HasValue
            context.Il.Emit(OpCodes.Brtrue, notEmptyLabel); // if(obj.HasValue) goto notEmpty;
            return true;
        }
    }
}