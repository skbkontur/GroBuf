using System;
using System.Reflection.Emit;

namespace GroBuf.Writers
{
    internal class NullableWriterBuilder : WriterBuilderBase
    {
        public NullableWriterBuilder(Type type)
            : base(type)
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
            context.LoadResult(); // stack: [obj.Value, writeEmpty, result]
            context.LoadIndexByRef(); // stack: [obj.Value, writeEmpty, result, ref index]
            il.Emit(OpCodes.Call, context.Context.GetWriter(Type.GetGenericArguments()[0])); // writer(obj.Value, writeEmpty, result, ref index)
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