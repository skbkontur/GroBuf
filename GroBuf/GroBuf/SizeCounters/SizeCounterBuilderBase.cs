using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;
using GrEmit.Utils;

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
            using (var il = new GroboIL(method))
            {
                var context = new SizeCounterMethodBuilderContext(sizeCounterBuilderContext, il);

                var notEmptyLabel = il.DefineLabel("notEmpty");
                if(CheckEmpty(context, notEmptyLabel)) // Check if obj is empty
                    context.ReturnForNull(); // return for null
                il.MarkLabel(notEmptyLabel); // Now we know that obj is not empty

                if(!Type.IsValueType && IsReference && sizeCounterBuilderContext.GroBufWriter.Options.HasFlag(GroBufOptions.PackReferences))
                {
                    // Pack reference
                    var index = il.DeclareLocal(typeof(int));
                    context.LoadContext(); // stack: [context]
                    il.Dup(); // stack: [context, context]
                    il.Ldfld(WriterContext.IndexField); // stack: [context, context.index]
                    il.Stloc(index); // index = context.index; stack: [context]
                    il.Ldfld(WriterContext.ObjectsField); // stack: [context.objects]
                    context.LoadObj(); // stack: [context.objects, obj]
                    il.Call(HackHelpers.GetMethodDefinition<Dictionary<object, int>>(dict => dict.ContainsKey(null))); // stack: [context.object.ContainsKey(obj)]
                    var storeLocationLabel = il.DefineLabel("storeLocation");
                    il.Brfalse(storeLocationLabel); // if(!context.objects.ContainsKey(obj)) goto storeLocation; stack: []
                    context.LoadContext(); // stack: [context]
                    il.Dup(); // stack: [context, context]
                    il.Ldfld(WriterContext.ReferencesField); // stack: [context, context.references]
                    il.Ldc_I4(1); // stack: [context, context.references, 1]
                    il.Add(); // stack: [context, context.references + 1]
                    il.Stfld(WriterContext.ReferencesField); // context.references += 1; stack: []
                    il.Ldc_I4(5); // stack: [5]
                    il.Ret(); // return 5
                    il.MarkLabel(storeLocationLabel);
                    context.LoadContext(); // stack: [context]
                    il.Ldfld(typeof(WriterContext).GetField("objects", BindingFlags.Public | BindingFlags.Instance)); // stack: [context.objects]
                    context.LoadObj(); // stack: [context.objects, obj]
                    il.Ldloc(index); // stack: [context.objects, obj, index]
                    il.Call(HackHelpers.GetMethodDefinition<Dictionary<object, int>>(dict => dict.Add(null, 0))); // context.objects.Add(obj, index);
                }

                CountSizeNotEmpty(context); // Count size
                il.Ret();
            }
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

        protected abstract bool IsReference { get; }

        protected Type Type { get; private set; }
    }
}