using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Readers
{
    internal abstract class ReaderBuilderBase<T> : IReaderBuilder<T>
    {
        protected ReaderBuilderBase(IReaderCollection readerCollection)
        {
            this.readerCollection = readerCollection;
            Type = typeof(T);
        }

        public abstract ReaderDelegate<T> BuildReader();

        /// <summary>
        /// Reads TypeCode at <c>data</c>[<c>index</c>] and checks it
        /// <para></para>
        /// Returns default(<typeparamref name="T"/>) if TypeCode = Empty
        /// </summary>
        /// <param name="context">Current context</param>
        protected void ReadTypeCodeAndCheck(ReaderBuilderContext<T> context)
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
            context.ReturnDefaultValue();

            il.MarkLabel(notEmptyLabel);

            context.CheckTypeCode();
        }

        protected unsafe Delegate GetReader(Type type)
        {
            if(getWriterMethod == null)
                getWriterMethod = ((MethodCallExpression)((Expression<Action<IReaderCollection>>)(collection => collection.GetReader<int>())).Body).Method.GetGenericMethodDefinition();
            return ((Delegate)getWriterMethod.MakeGenericMethod(new[] {type}).Invoke(readerCollection, new object[0]));
        }

        protected Type Type { get; private set; }
        private readonly IReaderCollection readerCollection;
        private MethodInfo getWriterMethod;
    }
}