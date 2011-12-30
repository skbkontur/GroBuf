using System;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Readers
{
    internal abstract class ReaderBuilderWithoutParams<T> : ReaderBuilderBase<T>
    {
        protected ReaderBuilderWithoutParams(IReaderCollection readerCollection)
            : base(readerCollection)
        {
        }

        public override ReaderDelegate<T> BuildReader()
        {
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), Type,
                                                  new[]
                                                      {
                                                          typeof(byte).MakePointerType(), typeof(int).MakeByRefType(), typeof(int)
                                                      }, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            var context = new ReaderBuilderContext<T>(il);

            ReadTypeCodeAndCheck(context); // Read TypeCode and check
            ReadNotEmpty(context); // Read obj
            il.Emit(OpCodes.Ret);
            return (ReaderDelegate<T>)dynamicMethod.CreateDelegate(typeof(ReaderDelegate<T>));
        }

        protected abstract void ReadNotEmpty(ReaderBuilderContext<T> context);
    }
}