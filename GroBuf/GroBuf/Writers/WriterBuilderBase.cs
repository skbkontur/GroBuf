using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Writers
{
    internal abstract class WriterBuilderBase<T> : IWriterBuilder<T>
    {
        protected WriterBuilderBase(IWriterCollection writerCollection)
        {
            this.writerCollection = writerCollection;
            Type = typeof(T);
        }

        public abstract WriterDelegate<T> BuildWriter();

        /// <summary>
        /// Checks whether <c>obj</c> is empty
        /// </summary>
        /// <param name="context">Current context</param>
        /// <param name="notEmptyLabel">Label where to go if <c>obj</c> is not empty</param>
        /// <returns>true if <c>obj</c> can be empty</returns>
        protected virtual bool CheckEmpty(WriterBuilderContext context, Label notEmptyLabel)
        {
            if(!Type.IsClass) return false;
            context.LoadObj(); // stack: [obj]
            context.Il.Emit(OpCodes.Brtrue, notEmptyLabel); // if(obj != null) goto notEmpty;
            return true;
        }

        protected unsafe Delegate GetWriter(Type type)
        {
            if(getWriterMethod == null)
                getWriterMethod = ((MethodCallExpression)((Expression<Action<IWriterCollection>>)(collection => collection.GetWriter<int>())).Body).Method.GetGenericMethodDefinition();
            return ((Delegate)getWriterMethod.MakeGenericMethod(new[] {type}).Invoke(writerCollection, new object[0]));
        }

        protected Type Type { get; private set; }
        private readonly IWriterCollection writerCollection;
        private MethodInfo getWriterMethod;
    }
}