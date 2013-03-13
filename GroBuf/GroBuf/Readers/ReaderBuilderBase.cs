using System;
using System.Reflection;

using GrEmit;

namespace GroBuf.Readers
{
    internal abstract class ReaderBuilderBase : IReaderBuilder
    {
        protected ReaderBuilderBase(Type type)
        {
            Type = type;
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
            var il = new GroboIL(method);
            var context = new ReaderMethodBuilderContext(readerTypeBuilderContext, il);

            ReadTypeCodeAndCheck(context); // Read TypeCode and check
            ReadNotEmpty(context); // Read obj
            il.Ret();
            return method;
        }

        protected abstract void ReadNotEmpty(ReaderMethodBuilderContext context);

        protected Type Type { get; private set; }

        /// <summary>
        /// Reads TypeCode at <c>data</c>[<c>index</c>] and checks it
        /// <para></para>
        /// Returns default(<typeparamref name="T"/>) if TypeCode = Empty
        /// </summary>
        /// <param name="context">Current context</param>
        private static void ReadTypeCodeAndCheck(ReaderMethodBuilderContext context)
        {
            var il = context.Il;
            var notEmptyLabel = il.DefineLabel("notEmpty");
            il.Ldc_I4(1);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Ldind(typeof(byte)); // stack: [data[index]]
            il.Dup(); // stack: [data[index], data[index]]
            il.Stloc(context.TypeCode); // typeCode = data[index]; stack: [typeCode]

            il.Brtrue(notEmptyLabel); // if(typeCode != 0) goto notNull;

            context.IncreaseIndexBy1(); // index = index + 1
            il.Ret();

            il.MarkLabel(notEmptyLabel);

            context.CheckTypeCode();
        }
    }
}