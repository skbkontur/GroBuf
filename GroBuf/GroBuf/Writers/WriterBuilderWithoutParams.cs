using System;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Writers
{
    internal abstract class WriterBuilderWithoutParams<T> : WriterBuilderBase<T>
    {
        protected WriterBuilderWithoutParams(IWriterCollection writerCollection)
            : base(writerCollection)
        {
        }

        public override WriterDelegate<T> BuildWriter()
        {
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void),
                                                  new[]
                                                      {
                                                          Type, typeof(bool), typeof(byte[]).MakeByRefType(),
                                                          typeof(int).MakeByRefType(), typeof(byte).MakePointerType().MakeByRefType()
                                                      }, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            var context = new WriterBuilderContext(il);

            var notEmptyLabel = il.DefineLabel();
            if(CheckEmpty(context, notEmptyLabel)) // Check if obj is empty
                context.WriteNull(); // Write null & return
            il.MarkLabel(notEmptyLabel); // Now we know that obj is not empty
            WriteNotEmpty(context); // Write obj
            il.Emit(OpCodes.Ret);
            return (WriterDelegate<T>)dynamicMethod.CreateDelegate(typeof(WriterDelegate<T>));
        }

        protected abstract void WriteNotEmpty(WriterBuilderContext context);
    }
}