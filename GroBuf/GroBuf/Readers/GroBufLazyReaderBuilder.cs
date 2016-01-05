using System;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;

namespace GroBuf.Readers
{
    internal class GroBufLazyReaderBuilder : ReaderBuilderBase
    {
        public GroBufLazyReaderBuilder(Type type, ModuleBuilder module)
            : base(type)
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(GroBufLazy<>)))
                throw new InvalidOperationException("Expected GroBufLazy but was '" + Type + "'");
            this.module = module;
            readerInvoker = BuildReaderInvoker();
        }

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
            context.BuildConstants(Type.GetGenericArguments()[0]);
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            var il = context.Il;

            il.Ldc_I4(1); // stack: [1]
            context.AssertLength(); // assert can read 1 byte; stack: []
            var source = il.DeclareLocal(typeof(IntPtr));
            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Stloc(source); // source = &data[index]; stack: []
            context.IncreaseIndexBy1(); // index = index + 1; stack: []
            il.Ldc_I4(4); // stack: [4]
            context.AssertLength(); // assert can read 4 bytes; stack: []
            var length = il.DeclareLocal(typeof(int));
            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Ldind(typeof(int)); // stack: [*(int*)&data[index]]
            il.Stloc(length); // length = *(int*)&data[index]; stack: []
            context.IncreaseIndexBy4(); // index += 4; stack: []
            il.Ldloc(length); // stack: [length]
            context.AssertLength(); // assert can read length bytes; stack: []
            il.Ldloc(length); // stack: [length]
            il.Ldc_I4(5); // stack: [length, 5]
            il.Add(); // stack: [length + 5]
            il.Stloc(length); // length = length + 5; stack: []
            var array = il.DeclareLocal(typeof(byte[]));
            il.Ldloc(length); // stack: [length]
            il.Newarr(typeof(byte)); // stack: [new byte[length]]
            il.Stloc(array); // array = new byte[length]; stack: []
            var dest = il.DeclareLocal(typeof(byte).MakeByRefType(), true);
            il.Ldloc(array); // stack: [array]
            il.Ldc_I4(0); // stack: [array, 0]
            il.Ldelema(typeof(byte)); // stack: [&array[0]]
            il.Stloc(dest); // dest = &array[0]; stack: []
            il.Ldloc(dest); // stack: [dest]
            il.Ldloc(source); // stack: [dest, source]
            il.Ldloc(length); // stack: [dest, source, length]
            il.Cpblk(); // dest = source; stack: []
            il.Ldnull();
            il.Stloc(dest);
            
            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Ldloc(length); // stack: [ref index, index, length]
            il.Ldc_I4(5); // stack: [ref index, index, length, 5]
            il.Sub(); // stack: [ref index, index, length - 5]
            il.Add(); // stack: [ref index, index + length - 5]
            il.Stind(typeof(int)); // index = index + length - 5; stack: []

            var argumentType = Type.GetGenericArguments()[0];
            context.LoadResultByRef(); // stack: [ref result]
            il.Ldloc(array); // stack: [ref result, array]
            context.LoadReader(argumentType); // stack: [ref result, array, reader<argument>]
            il.Newobj(readerInvoker.GetConstructor(new[] {typeof(IntPtr)})); // stack: [ref result, array, new ReaderInvoker(reader<argument>)]
            il.Ldftn(readerInvoker.GetMethod("Read", BindingFlags.Instance | BindingFlags.Public));
            var funcType = typeof(Func<,>).MakeGenericType(typeof(byte[]), argumentType);
            il.Newobj(funcType.GetConstructor(new[] {typeof(object), typeof(IntPtr)})); // stack: [ref result, array, new Func<byte[], argument>(..)]
            il.Newobj(Type.GetConstructor(new[] {typeof(byte[]), funcType})); // stack: [ref result, new Lazy<argument>(array, func)]
            il.Stind(Type); // result = new Lazy<argument>(array, func); stack: []
        }

        protected override bool IsReference { get { return true; } }

        private Type BuildReaderInvoker()
        {
            var argument = Type.GetGenericArguments()[0];
            var typeBuilder = module.DefineType("ReaderInvoker_" + Type, TypeAttributes.Public | TypeAttributes.Class);
            var reader = typeBuilder.DefineField("reader", typeof(IntPtr), FieldAttributes.Private);
            var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] {typeof(IntPtr)});
            using(var il = new GroboIL(constructor))
            {
                il.Ldarg(0); // stack: [this]
                il.Ldarg(1); // stack: [this, reader]
                il.Stfld(reader); // this.reader = reader; stack: []
                il.Ret();
            }
            var method = typeBuilder.DefineMethod("Read", MethodAttributes.Public, argument, new[] {typeof(byte[])});
            using(var il = new GroboIL(method))
            {
                var pinnedData = il.DeclareLocal(typeof(byte).MakeByRefType(), "pinnedData", true);
                il.Ldarg(1); // stack: [data]
                il.Ldc_I4(0); // stack: [data, 0]
                il.Ldelema(typeof(byte)); // stack: [&data[0]]
                il.Stloc(pinnedData); // pinnedData = &data[0]; stack: []
                var index = il.DeclareLocal(typeof(int), "index");
                il.Ldc_I4(0); // stack: [0]
                il.Stloc(index); // index = 0; stack: []
                var result = il.DeclareLocal(argument, "result");
                if(argument.IsValueType)
                {
                    il.Ldloca(result); // stack: [ref result]
                    il.Initobj(argument); // result = default(T); stack: []
                }
                else
                {
                    if(!argument.IsArray)
                        ObjectConstructionHelper.EmitConstructionOfType(argument, il);
                    else
                    {
                        il.Ldc_I4(0);
                        il.Newarr(argument.GetElementType());
                    }
                    il.Stloc(result);
                }
                var context = il.DeclareLocal(typeof(ReaderContext), "context");
                il.Ldarg(1); // stack: [data]
                il.Ldlen(); // stack: [data.Length]
                il.Ldc_I4(0); // stack: [data.Length, 0]
                il.Ldc_I4(0); // stack: [data.Length, 0, 0]
                il.Newobj(typeof(ReaderContext).GetConstructor(new[] {typeof(int), typeof(int), typeof(int)}));
                il.Stloc(context);

                il.Ldloc(pinnedData);
                il.Conv<IntPtr>();
                il.Ldloca(index);
                il.Ldloca(result);
                il.Ldloc(context);
                il.Ldarg(0);
                il.Ldfld(reader);
                il.Calli(CallingConventions.Standard, typeof(void), new[] {typeof(IntPtr), typeof(int).MakeByRefType(), argument.MakeByRefType(), typeof(ReaderContext)});
                il.Ldnull();
                il.Stloc(pinnedData);
                il.Ldloc(result);
                il.Ret();
            }
            return typeBuilder.CreateType();
        }

        private readonly ModuleBuilder module;
        private readonly Type readerInvoker;
    }
}