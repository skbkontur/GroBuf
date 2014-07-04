using System;
using System.Collections.Generic;
using System.Reflection;
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
            var il = new GroboIL(method);
            var context = new ReaderMethodBuilderContext(readerTypeBuilderContext, il, Type.IsValueType);

            ReadTypeCodeAndCheck(context); // Read TypeCode and check
            ReadNotEmpty(context); // Read obj

            if(context.Index != null)
            {
                context.LoadContext();
                il.Ldfld(typeof(ReaderContext).GetField("objects", BindingFlags.Public | BindingFlags.Instance));
                var objectsIsNullLabel = il.DefineLabel("objectsIsNull");
                il.Brfalse(objectsIsNullLabel);
                context.LoadContext();
                il.Ldfld(typeof(ReaderContext).GetField("objects", BindingFlags.Public | BindingFlags.Instance));
                il.Ldloc(context.Index);
                context.LoadResult(Type);
                if(Type.IsValueType)
                    il.Box(Type);
                il.Stelem(typeof(object));
                il.MarkLabel(objectsIsNullLabel);
            }

            il.Ret();
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
            if (context.Index != null)
            {
                context.LoadContext();
                il.Ldfld(typeof(ReaderContext).GetField("count", BindingFlags.Public | BindingFlags.Instance));
                il.Stloc(context.Index);
            }
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

            if(context.Index != null)
            {
                context.LoadContext();
                il.Ldloc(context.Index);
                il.Ldc_I4(1);
                il.Add();
                il.Stfld(typeof(ReaderContext).GetField("count", BindingFlags.Public | BindingFlags.Instance));
            }
        }
    }
}