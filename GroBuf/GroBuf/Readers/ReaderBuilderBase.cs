using System;
using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.Readers
{
    internal abstract class ReaderBuilderBase<T> : IReaderBuilder<T>
    {
        protected ReaderBuilderBase()
        {
            Type = typeof(T);
        }

        public MethodInfo BuildReader(ReaderTypeBuilderContext readerTypeBuilderContext)
        {
            var typeBuilder = readerTypeBuilderContext.TypeBuilder;

            var method = typeBuilder.DefineMethod("Read_" + Type.Name + "_" + Guid.NewGuid(), MethodAttributes.Public | MethodAttributes.Static, typeof(void),
                                                  new[]
                                                      {
                                                          typeof(byte*), typeof(int).MakeByRefType(), typeof(int), Type.MakeByRefType()
                                                      });
            readerTypeBuilderContext.SetReader(Type, method);
            var il = method.GetILGenerator();
            var context = new ReaderMethodBuilderContext<T>(readerTypeBuilderContext, il);

            ReadTypeCodeAndCheck(context); // Read TypeCode and check
            ReadNotEmpty(context); // Read obj
            il.Emit(OpCodes.Ret);
            return method;
        }

        protected abstract void ReadNotEmpty(ReaderMethodBuilderContext<T> context);

        protected Type Type { get; private set; }

        /// <summary>
        /// Reads TypeCode at <c>data</c>[<c>index</c>] and checks it
        /// <para></para>
        /// Returns default(<typeparamref name="T"/>) if TypeCode = Empty
        /// </summary>
        /// <param name="context">Current context</param>
        private static void ReadTypeCodeAndCheck(ReaderMethodBuilderContext<T> context)
        {
            var il = context.Il;
            var notEmptyLabel = il.DefineLabel();
            il.Emit(OpCodes.Ldc_I4_1);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Emit(OpCodes.Ldind_U1); // stack: [data[index]]
            il.Emit(OpCodes.Dup); // stack: [data[index], data[index]]
            il.Emit(OpCodes.Stloc, context.TypeCode); // typeCode = data[index]; stack: [typeCode]

            il.Emit(OpCodes.Brtrue, notEmptyLabel); // if(typeCode != 0) goto notNull;

            context.IncreaseIndexBy1(); // index = index + 1
            context.Il.Emit(OpCodes.Ret);

            il.MarkLabel(notEmptyLabel);

            context.CheckTypeCode();
        }
    }
}