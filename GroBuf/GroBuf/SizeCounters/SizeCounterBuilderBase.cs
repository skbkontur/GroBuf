using System;
using System.Collections.Generic;
using System.Reflection.Emit;

using GrEmit;

namespace GroBuf.SizeCounters
{
    internal abstract class SizeCounterBuilderBase : ISizeCounterBuilder
    {
        protected SizeCounterBuilderBase(Type type)
        {
            Type = type;
        }

        public void BuildSizeCounter(SizeCounterBuilderContext sizeCounterBuilderContext)
        {
            var method = new DynamicMethod("Count_" + Type.Name + "_" + Guid.NewGuid(), typeof(int), new[] {Type, typeof(bool), typeof(WriterContext)}, sizeCounterBuilderContext.Module, true);
            sizeCounterBuilderContext.SetSizeCounterMethod(Type, method);
            var il = new GroboIL(method);
            var context = new SizeCounterMethodBuilderContext(sizeCounterBuilderContext, il);

            var notEmptyLabel = il.DefineLabel("notEmpty");
            if(CheckEmpty(context, notEmptyLabel)) // Check if obj is empty
                context.ReturnForNull(); // return for null
            il.MarkLabel(notEmptyLabel); // Now we know that obj is not empty
            CountSizeNotEmpty(context); // Count size
            il.Ret();
            var @delegate = method.CreateDelegate(typeof(SizeCounterDelegate<>).MakeGenericType(Type));
            var pointer = GroBufHelpers.ExtractDynamicMethodPointer(method);
            sizeCounterBuilderContext.SetSizeCounterPointer(Type, pointer, @delegate);
        }

        public void BuildConstants(SizeCounterConstantsBuilderContext context)
        {
            context.SetFields(Type, new KeyValuePair<string, Type>[0]);
            BuildConstantsInternal(context);
        }

        protected abstract void BuildConstantsInternal(SizeCounterConstantsBuilderContext context);

        protected abstract void CountSizeNotEmpty(SizeCounterMethodBuilderContext context);

        /// <summary>
        /// Checks whether <c>obj</c> is empty
        /// </summary>
        /// <param name="context">Current context</param>
        /// <param name="notEmptyLabel">Label where to go if <c>obj</c> is not empty</param>
        /// <returns>true if <c>obj</c> can be empty</returns>
        protected virtual bool CheckEmpty(SizeCounterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            if(Type.IsValueType) return false;
            context.LoadObj(); // stack: [obj]
            context.Il.Brtrue(notEmptyLabel); // if(obj != null) goto notEmpty;
            return true;
        }

        protected Type Type { get; private set; }
    }
}