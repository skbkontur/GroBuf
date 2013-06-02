using System;
using System.Collections.Generic;
using System.Reflection.Emit;

using GrEmit;

namespace GroBuf.Writers
{
    internal abstract class WriterBuilderBase : IWriterBuilder
    {
        protected WriterBuilderBase(Type type)
        {
            Type = type;
        }

        public void BuildWriter(WriterTypeBuilderContext writerTypeBuilderContext)
        {
            var method = new DynamicMethod("Write_" + Type.Name + "_" + Guid.NewGuid(), typeof(void),
                                           new[]
                                               {
                                                   Type, typeof(bool), typeof(IntPtr), typeof(int).MakeByRefType(),
                                               }, writerTypeBuilderContext.Module, true);
            writerTypeBuilderContext.SetWriterMethod(Type, method);
            var il = new GroboIL(method);
            var context = new WriterMethodBuilderContext(writerTypeBuilderContext, il);

            var notEmptyLabel = il.DefineLabel("notEmpty");
            if(CheckEmpty(context, notEmptyLabel)) // Check if obj is empty
                context.WriteNull(); // Write null & return
            il.MarkLabel(notEmptyLabel); // Now we know that obj is not empty
            WriteNotEmpty(context); // Write obj
            il.Ret();
            var @delegate = method.CreateDelegate(typeof(WriterDelegate<>).MakeGenericType(Type));
            var pointer = GroBufHelpers.ExtractDynamicMethodPointer(method);
            writerTypeBuilderContext.SetWriterPointer(Type, pointer, @delegate);
        }

        public void BuildConstants(WriterConstantsBuilderContext context)
        {
            context.SetFields(Type, new KeyValuePair<string, Type>[0]);
            BuildConstantsInternal(context);
        }

        protected abstract void BuildConstantsInternal(WriterConstantsBuilderContext context);
        protected abstract void WriteNotEmpty(WriterMethodBuilderContext context);

        /// <summary>
        /// Checks whether <c>obj</c> is empty
        /// </summary>
        /// <param name="context">Current context</param>
        /// <param name="notEmptyLabel">Label where to go if <c>obj</c> is not empty</param>
        /// <returns>true if <c>obj</c> can be empty</returns>
        protected virtual bool CheckEmpty(WriterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            if(Type.IsValueType) return false;
            context.LoadObj(); // stack: [obj]
            context.Il.Brtrue(notEmptyLabel); // if(obj != null) goto notEmpty;
            return true;
        }

        protected Type Type { get; private set; }
    }
}