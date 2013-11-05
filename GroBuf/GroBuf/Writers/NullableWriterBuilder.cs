using System;

using GrEmit;

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

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            context.BuildConstants(Type.GetGenericArguments()[0]);
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;

            context.LoadWriter(Type.GetGenericArguments()[0]);

            context.LoadObjByRef(); // stack: [&obj]
            il.Call(Type.GetProperty("Value").GetGetMethod()); // stack: [obj.Value]
            context.LoadWriteEmpty(); // stack: [obj.Value, writeEmpty]
            context.LoadResult(); // stack: [obj.Value, writeEmpty, result]
            context.LoadIndexByRef(); // stack: [obj.Value, writeEmpty, result, ref index]
            context.CallWriter(Type.GetGenericArguments()[0]); // writer(obj.Value, writeEmpty, result, ref index)
        }

        protected override bool CheckEmpty(WriterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            context.LoadObjByRef(); // stack: [&obj]
            context.Il.Call(Type.GetProperty("HasValue").GetGetMethod()); // stack: obj.HasValue
            context.Il.Brtrue(notEmptyLabel); // if(obj.HasValue) goto notEmpty;
            return true;
        }
    }
}