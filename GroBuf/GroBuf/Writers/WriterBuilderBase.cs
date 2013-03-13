using System;
using System.Reflection;

using GrEmit;

namespace GroBuf.Writers
{
    internal abstract class WriterBuilderBase : IWriterBuilder
    {
        protected WriterBuilderBase(Type type)
        {
            Type = type;
        }

        public MethodInfo BuildWriter(WriterTypeBuilderContext writerTypeBuilderContext)
        {
            var typeBuilder = writerTypeBuilderContext.TypeBuilder;

            var method = typeBuilder.DefineMethod("Write_" + Type.Name + "_" + Guid.NewGuid(), MethodAttributes.Public | MethodAttributes.Static, typeof(void),
                                                  new[]
                                                      {
                                                          Type, typeof(bool), typeof(byte*), typeof(int).MakeByRefType(),
                                                      });
            writerTypeBuilderContext.SetWriter(Type, method);
            var il = new GroboIL(method);
            var context = new WriterMethodBuilderContext(writerTypeBuilderContext, il);

            var notEmptyLabel = il.DefineLabel("notEmpty");
            if(CheckEmpty(context, notEmptyLabel)) // Check if obj is empty
                context.WriteNull(); // Write null & return
            il.MarkLabel(notEmptyLabel); // Now we know that obj is not empty
            WriteNotEmpty(context); // Write obj
            il.Ret();
            return method;
        }

        protected abstract void WriteNotEmpty(WriterMethodBuilderContext context);

        /// <summary>
        /// Checks whether <c>obj</c> is empty
        /// </summary>
        /// <param name="context">Current context</param>
        /// <param name="notEmptyLabel">Label where to go if <c>obj</c> is not empty</param>
        /// <returns>true if <c>obj</c> can be empty</returns>
        protected virtual bool CheckEmpty(WriterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            if(!Type.IsClass) return false;
            context.LoadObj(); // stack: [obj]
            context.Il.Brtrue(notEmptyLabel); // if(obj != null) goto notEmpty;
            return true;
        }

        protected Type Type { get; private set; }
    }
}