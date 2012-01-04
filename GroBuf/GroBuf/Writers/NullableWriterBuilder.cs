using System;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Writers
{
    internal class NullableWriterBuilder<T> : WriterBuilderBase<T>
    {
        public NullableWriterBuilder()
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Nullable<>)))
                throw new InvalidOperationException("Expected nullable but was " + Type);
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            context.LoadObjByRef(); // stack: [&obj]
            il.Emit(OpCodes.Call, Type.GetProperty("Value").GetGetMethod()); // stack: [obj.Value]
            context.LoadWriteEmpty(); // stack: [obj.Value, writeEmpty]
            context.LoadResultByRef(); // stack: [obj.Value, writeEmpty, ref result]
            context.LoadIndexByRef(); // stack: [obj.Value, writeEmpty, ref result, ref index]
            context.LoadPinnedResultByRef(); // stack: [obj.Value, writeEmpty, ref result, ref index, ref pinnedResult]
            il.Emit(OpCodes.Call, context.Context.GetWriter(Type.GetGenericArguments()[0])); // writer(obj.Value, writeEmpty, ref result, ref index, ref pinnedResult)
        }

        protected override bool CheckEmpty(WriterMethodBuilderContext context, Label notEmptyLabel)
        {
            context.LoadObjByRef(); // stack: [&obj]
            context.Il.Emit(OpCodes.Call, Type.GetProperty("HasValue").GetGetMethod()); // stack: obj.HasValue
            context.Il.Emit(OpCodes.Brtrue, notEmptyLabel); // if(obj.HasValue) goto notEmpty;
            return true;
        }
    }
}