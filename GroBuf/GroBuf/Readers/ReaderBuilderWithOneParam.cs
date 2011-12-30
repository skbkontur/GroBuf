using System;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Readers
{
    internal abstract class ReaderBuilderWithOneParam<T, TParam> : ReaderBuilderBase<T>
    {
        protected ReaderBuilderWithOneParam(IReaderCollection readerCollection)
            : base(readerCollection)
        {
        }

        public override unsafe ReaderDelegate<T> BuildReader()
        {
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), Type,
                                                  new[]
                                                      {
                                                          typeof(byte).MakePointerType(), typeof(int).MakeByRefType(), typeof(int), typeof(TParam)
                                                      }, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            var context = new ReaderBuilderContext<T>(il);
            ReadTypeCodeAndCheck(context); // Read TypeCode and check
            var param = ReadNotEmpty(context); // Read obj
            il.Emit(OpCodes.Ret);
            var writer = (ReaderDelegate<T, TParam>)dynamicMethod.CreateDelegate(typeof(ReaderDelegate<T, TParam>));
            return (byte* data, ref int index, int length) => writer(data, ref index, length, param);
        }

        protected abstract TParam ReadNotEmpty(ReaderBuilderContext<T> context);
    }
}