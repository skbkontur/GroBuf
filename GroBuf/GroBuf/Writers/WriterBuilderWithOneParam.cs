using System;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Writers
{
    internal abstract class WriterBuilderWithOneParam<T, TParam> : WriterBuilderBase<T>
    {
        protected WriterBuilderWithOneParam(IWriterCollection writerCollection)
            : base(writerCollection)
        {
        }

        public override unsafe WriterDelegate<T> BuildWriter()
        {
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void),
                                                  new[]
                                                      {
                                                          Type, typeof(bool), typeof(byte[]).MakeByRefType(),
                                                          typeof(int).MakeByRefType(), typeof(byte).MakePointerType().MakeByRefType(), typeof(TParam)
                                                      }, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            var context = new WriterBuilderContext(il);
            var notEmptyLabel = il.DefineLabel();
            if(CheckEmpty(context, notEmptyLabel)) // Check if obj is empty
                context.WriteNull(); // Write null & return
            il.MarkLabel(notEmptyLabel); // Now we know that obj is not empty
            var param = WriteNotEmpty(context); // Write obj
            il.Emit(OpCodes.Ret);
            var writer = (WriterDelegate<T, TParam>)dynamicMethod.CreateDelegate(typeof(WriterDelegate<T, TParam>));
            return (T obj, bool writeEmpty, ref byte[] result, ref int index, ref byte* pinnedResult) => writer(obj, writeEmpty, ref result, ref index, ref pinnedResult, param);
        }

        protected abstract TParam WriteNotEmpty(WriterBuilderContext context);
    }
}