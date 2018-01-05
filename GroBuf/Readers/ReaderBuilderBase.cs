using System;
using System.Collections.Generic;
using System.Reflection.Emit;

using GrEmit;
using GrEmit.Utils;

namespace GroBuf.Readers
{
    internal abstract class ReaderBuilderBase : IReaderBuilder
    {
        protected ReaderBuilderBase(Type type)
        {
            Type = type;
        }

        public void BuildReader(ReaderTypeBuilderContext readerTypeBuilderContext)
        {
            var method = new DynamicMethod("Read_" + Type.Name + "_" + Guid.NewGuid(), typeof(void),
                                           new[]
                                               {
                                                   typeof(IntPtr), typeof(int).MakeByRefType(), Type.MakeByRefType(), typeof(ReaderContext)
                                               }, readerTypeBuilderContext.Module, true);
            readerTypeBuilderContext.SetReaderMethod(Type, method);
            using(var il = new GroboIL(method))
            {
                var context = new ReaderMethodBuilderContext(readerTypeBuilderContext, il, !Type.IsValueType && IsReference);

                ReadTypeCodeAndCheck(context); // Read TypeCode and check

                if(!Type.IsValueType && IsReference)
                {
                    // Read reference
                    context.LoadContext(); // stack: [context]
                    il.Ldfld(ReaderContext.ObjectsField); // stack: [context.objects]
                    var notReadLabel = il.DefineLabel("notRead");
                    il.Brfalse(notReadLabel);
                    context.LoadIndex(); // stack: [external index]
                    context.LoadContext(); // stack: [external index, context]
                    il.Ldfld(ReaderContext.StartField); // stack: [external index, context.start]
                    il.Sub(); // stack: [external index - context.start]
                    il.Stloc(context.Index); // index = external index - context.start; stack: []

                    context.LoadContext(); // stack: [context]
                    il.Ldfld(ReaderContext.ObjectsField); // stack: [context.objects]
                    il.Ldloc(context.Index); // stack: [context.objects, index]
                    var obj = il.DeclareLocal(typeof(object));
                    il.Ldloca(obj);
                    object dummy;
                    il.Call(HackHelpers.GetMethodDefinition<Dictionary<int, object>>(dict => dict.TryGetValue(0, out dummy))); // stack: [context.objects.TryGetValue(index, out obj)]
                    il.Brfalse(notReadLabel); // if(!context.objects.TryGetValue(index, out obj)) goto notRead;
                    context.LoadResultByRef(); // stack: [ref result]
                    il.Ldloc(obj); // stack: [ref result, obj]
                    il.Castclass(Type); // stack: [ref result, (Type)obj]
                    il.Stind(Type); // result = (Type)obj; stack: []
                    context.IncreaseIndexBy1(); // Skip type code
                    context.SkipValue(); // Skip value - it has already been read
                    il.Ret();
                    il.MarkLabel(notReadLabel);
                    il.Ldloc(context.TypeCode); // stack: [typeCode]
                    il.Ldc_I4((int)GroBufTypeCode.Reference); // stack: [typeCode, GroBufTypeCode.Reference]
                    var readUsualLabel = il.DefineLabel("readUsual");
                    il.Bne_Un(readUsualLabel); // if(typeCode != GroBufTypeCode.Reference) goto readUsual; stack: []

                    context.LoadContext(); // stack: [context]
                    il.Ldfld(ReaderContext.ObjectsField); // stack: [context.objects]
                    var objectsIsNotNullLabel = il.DefineLabel("objectsIsNotNull");
                    il.Brtrue(objectsIsNotNullLabel); // if(context.objects != null) goto objectsIsNotNull; stack: [context.objects]
                    il.Ldstr("Reference is not valid at this point");
                    il.Newobj(typeof(DataCorruptedException).GetConstructor(new[] {typeof(string)}));
                    il.Throw();
                    il.MarkLabel(objectsIsNotNullLabel);

                    context.IncreaseIndexBy1(); // index = index + 1; stack: []
                    il.Ldc_I4(4);
                    context.AssertLength();
                    context.GoToCurrentLocation();
                    var reference = il.DeclareLocal(typeof(int));
                    il.Ldind(typeof(int)); // stack: [*(int*)data[index]]
                    il.Stloc(reference); // reference = *(int*)data[index]; stack: []
                    context.IncreaseIndexBy4(); // index = index + 4; stack: []
                    il.Ldloc(context.Index); // stack: [index]
                    il.Ldloc(reference); // stack: [index, reference]
                    var goodReferenceLabel = il.DefineLabel("goodReference");
                    il.Bgt(goodReferenceLabel, false); // if(index > reference) goto goodReference; stack: []
                    il.Ldstr("Bad reference");
                    il.Newobj(typeof(DataCorruptedException).GetConstructor(new[] {typeof(string)}));
                    il.Throw();
                    il.MarkLabel(goodReferenceLabel);
                    context.LoadContext(); // stack: [context]
                    il.Ldfld(ReaderContext.ObjectsField); // stack: [context.objects]
                    il.Ldloc(reference); // stack: [context.objects, reference]
                    il.Ldloca(obj); // stack: [context.objects, reference, ref obj]
                    il.Call(HackHelpers.GetMethodDefinition<Dictionary<int, object>>(dict => dict.TryGetValue(0, out dummy))); // stack: [context.objects.TryGetValue(reference, out obj)]
                    var readObjectLabel = il.DefineLabel("readObject");
                    il.Brfalse(readObjectLabel); // if(!context.objects.TryGetValue(reference, out obj)) goto readObjects; stack: []
                    context.LoadResultByRef(); // stack: [ref result]
                    il.Ldloc(obj); // stack: [ref result, obj]
                    il.Castclass(Type); // stack: [ref result, (Type)obj]
                    il.Stind(Type); // result = (Type)obj; stack: []
                    il.Ret();
                    il.MarkLabel(readObjectLabel);

                    // Referenced object has not been read - this means that the object reference belongs to is a property that had been deleted
                    context.LoadData(); // stack: [data]
                    il.Ldloc(reference); // stack: [data, reference]
                    context.LoadContext(); // stack: [data, reference, context]
                    il.Ldfld(ReaderContext.StartField); // stack: [data, reference, context.start]
                    il.Add(); // stack: [data, reference + context.start]
                    il.Stloc(reference); // reference += context.start; stack: [data]
                    il.Ldloca(reference); // stack: [data, ref reference]
                    context.LoadResultByRef(); // stack: [data, ref reference, ref result]
                    context.LoadContext(); // stack: [data, ref reference, ref result, context]
                    context.CallReader(Type);
                    il.Ret();
                    il.MarkLabel(readUsualLabel);
                }

                ReadNotEmpty(context); // Read obj
                il.Ret();
            }
            var @delegate = method.CreateDelegate(typeof(ReaderDelegate<>).MakeGenericType(Type));
            var pointer = GroBufHelpers.ExtractDynamicMethodPointer(method);
            readerTypeBuilderContext.SetReaderPointer(Type, pointer, @delegate);
        }

        public void BuildConstants(ReaderConstantsBuilderContext context)
        {
            context.SetFields(Type, new KeyValuePair<string, Type>[0]);
            BuildConstantsInternal(context);
        }

        protected abstract void BuildConstantsInternal(ReaderConstantsBuilderContext context);
        protected abstract void ReadNotEmpty(ReaderMethodBuilderContext context);

        protected abstract bool IsReference { get; }

        protected Type Type { get; private set; }

        /// <summary>
        ///     Reads TypeCode at <c>data</c>[<c>index</c>] and checks it
        ///     <para></para>
        ///     Returns default(<typeparamref name="T" />) if TypeCode = Empty
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