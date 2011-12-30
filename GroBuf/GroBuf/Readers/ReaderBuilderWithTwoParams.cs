using System;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Readers
{
    internal abstract class ReaderBuilderWithTwoParams<T, TParam1, TParam2> : ReaderBuilderBase<T>
    {
        protected ReaderBuilderWithTwoParams(IReaderCollection readerCollection)
            : base(readerCollection)
        {
        }

        public override unsafe ReaderDelegate<T> BuildReader()
        {
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), Type,
                                                  new[]
                                                      {
                                                          typeof(byte).MakePointerType(), typeof(int).MakeByRefType(), typeof(int), typeof(TParam1), typeof(TParam2)
                                                      }, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            var context = new ReaderBuilderContext<T>(il);
            ReadTypeCodeAndCheck(context); // Read TypeCode and check
            var parms = ReadNotEmpty(context); // Read obj
            il.Emit(OpCodes.Ret);
            var writer = (ReaderDelegate<T, TParam1, TParam2>)dynamicMethod.CreateDelegate(typeof(ReaderDelegate<T, TParam1, TParam2>));
            return (byte* data, ref int index, int length) => writer(data, ref index, length, parms.Item1, parms.Item2);
        }

        protected abstract Tuple<TParam1, TParam2> ReadNotEmpty(ReaderBuilderContext<T> context);
    }
}