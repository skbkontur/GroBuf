using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;
using GrEmit.Utils;

namespace GroBuf.Writers
{
    internal abstract class WriterBuilderBase : IWriterBuilder
    {
        protected WriterBuilderBase(Type type)
        {
            Type = type;
        }

        public void BuildWriter(WriterTypeBuilderContext writerTypeBuilderContext)
        {
            var method = new DynamicMethod("Write_" + Type.Name + "_" + Guid.NewGuid(), typeof(void),
                                           new[]
                                               {
                                                   Type, typeof(bool), typeof(IntPtr), typeof(int).MakeByRefType(), typeof(WriterContext)
                                               }, writerTypeBuilderContext.Module, true);
            writerTypeBuilderContext.SetWriterMethod(Type, method);
            using (var il = new GroboIL(method))
            {
                var context = new WriterMethodBuilderContext(writerTypeBuilderContext, il);

                var notEmptyLabel = il.DefineLabel("notEmpty");
                if (CheckEmpty(context, notEmptyLabel)) // Check if obj is empty
                    context.WriteNull(); // Write null & return
                il.MarkLabel(notEmptyLabel); // Now we know that obj is not empty

                if (!Type.IsValueType && IsReference && writerTypeBuilderContext.GroBufWriter.Options.HasFlag(GroBufOptions.PackReferences))
                {
                    // Pack reference
                    var index = il.DeclareLocal(typeof(int));
                    context.LoadIndex(); // stack: [external index]
                    context.LoadContext(); // stack: [external index, context]
                    il.Ldfld(WriterContext.StartField); // stack: [external index, context.start]
                    il.Sub(); // stack: [external index - context.start]
                    il.Stloc(index); // index = external index - context.start; stack: []
                    context.LoadContext(); // stack: [context]
                    il.Ldfld(typeof(WriterContext).GetField("objects", BindingFlags.Public | BindingFlags.Instance)); // stack: [context.objects]
                    context.LoadObj(); // stack: [context.objects, obj]
                    var reference = il.DeclareLocal(typeof(int));
                    il.Ldloca(reference); // stack: [context.objects, obj, ref reference]
                    int dummy;
                    il.Call(HackHelpers.GetMethodDefinition<Dictionary<object, int>>(dict => dict.TryGetValue(null, out dummy))); // stack: [context.object.TryGetValue(obj, out reference)]
                    var storeLocationLabel = il.DefineLabel("storeLocation");
                    il.Brfalse(storeLocationLabel);
                    // Current object is in dict
                    il.Ldloc(index);
                    il.Ldloc(reference); // stack: [index, reference]
                    var skipSelfLabel = il.DefineLabel("skipSelf");
                    il.Beq(skipSelfLabel); // if(index == reference) goto skipSelf; stack: []
                    il.Ldloc(index); // stack: [index]
                    il.Ldloc(reference); // stack: [index, reference]
                    var badReferenceLabel = il.DefineLabel("badReference");
                    il.Blt(badReferenceLabel, false); // if(index < reference) goto badReference; stack: []
                    context.WriteTypeCode(GroBufTypeCode.Reference); // result[index++] = GroBufTypeCode.Reference
                    context.GoToCurrentLocation(); // stack: [&result[index]]
                    il.Ldloc(reference); // stack: [&result[index], reference]
                    il.Stind(typeof(int)); // *(int *)&result[index] = reference
                    context.IncreaseIndexBy4(); // index += 4
                    il.Ret();
                    il.MarkLabel(badReferenceLabel);
                    il.Ldstr("Bad reference");
                    il.Newobj(typeof(DataCorruptedException).GetConstructor(new[] {typeof(string)}));
                    il.Throw();
                    il.MarkLabel(storeLocationLabel);
                    context.LoadContext(); // stack: [context]
                    il.Ldfld(typeof(WriterContext).GetField("objects", BindingFlags.Public | BindingFlags.Instance)); // stack: [context.objects]
                    context.LoadObj(); // stack: [context.objects, obj]
                    il.Ldloc(index); // stack: [context.objects, obj, index]
                    il.Call(HackHelpers.GetMethodDefinition<Dictionary<object, int>>(dict => dict.Add(null, 0))); // context.objects.Add(obj, index);
                    il.MarkLabel(skipSelfLabel);
                }

                WriteNotEmpty(context); // Write obj
                il.Ret();
            }
            var @delegate = method.CreateDelegate(typeof(WriterDelegate<>).MakeGenericType(Type));
            var pointer = GroBufHelpers.ExtractDynamicMethodPointer(method);
            writerTypeBuilderContext.SetWriterPointer(Type, pointer, @delegate);
        }

        public void BuildConstants(WriterConstantsBuilderContext context)
        {
            context.SetFields(Type, new KeyValuePair<string, Type>[0]);
            BuildConstantsInternal(context);
        }

        protected abstract void BuildConstantsInternal(WriterConstantsBuilderContext context);
        protected abstract void WriteNotEmpty(WriterMethodBuilderContext context);

        /// <summary>
        ///     Checks whether <c>obj</c> is empty
        /// </summary>
        /// <param name="context">Current context</param>
        /// <param name="notEmptyLabel">Label where to go if <c>obj</c> is not empty</param>
        /// <returns>true if <c>obj</c> can be empty</returns>
        protected virtual bool CheckEmpty(WriterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            if (Type.IsValueType) return false;
            context.LoadObj(); // stack: [obj]
            context.Il.Brtrue(notEmptyLabel); // if(obj != null) goto notEmpty;
            return true;
        }

        protected abstract bool IsReference { get; }

        protected Type Type { get; private set; }
    }
}