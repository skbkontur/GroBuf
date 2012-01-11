using System;
using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.SizeCounters
{
    internal abstract class SizeCounterBuilderBase<T> : ISizeCounterBuilder<T>
    {
        protected SizeCounterBuilderBase()
        {
            Type = typeof(T);
        }

        public MethodInfo BuildCounter(SizeCounterTypeBuilderContext sizeCounterTypeBuilderContext)
        {
            var typeBuilder = sizeCounterTypeBuilderContext.TypeBuilder;

            var method = typeBuilder.DefineMethod("Count_" + Type.Name + "_" + Guid.NewGuid(), MethodAttributes.Public | MethodAttributes.Static, typeof(int),
                                                  new[]
                                                      {
                                                          Type, typeof(bool)
                                                      });
            sizeCounterTypeBuilderContext.SetCounter(Type, method);
            var il = method.GetILGenerator();
            var context = new SizeCounterMethodBuilderContext(sizeCounterTypeBuilderContext, il);

            var notEmptyLabel = il.DefineLabel();
            if(CheckEmpty(context, notEmptyLabel)) // Check if obj is empty
                context.ReturnForNull(); // return for null
            il.MarkLabel(notEmptyLabel); // Now we know that obj is not empty
            CountSizeNotEmpty(context); // Count size
            il.Emit(OpCodes.Ret);
            return method;
        }

        protected abstract void CountSizeNotEmpty(SizeCounterMethodBuilderContext context);

        /// <summary>
        /// Checks whether <c>obj</c> is empty
        /// </summary>
        /// <param name="context">Current context</param>
        /// <param name="notEmptyLabel">Label where to go if <c>obj</c> is not empty</param>
        /// <returns>true if <c>obj</c> can be empty</returns>
        protected virtual bool CheckEmpty(SizeCounterMethodBuilderContext context, Label notEmptyLabel)
        {
            if(!Type.IsClass) return false;
            context.LoadObj(); // stack: [obj]
            context.Il.Emit(OpCodes.Brtrue, notEmptyLabel); // if(obj != null) goto notEmpty;
            return true;
        }

        protected Type Type { get; private set; }
    }
}