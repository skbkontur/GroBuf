using System;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;

namespace GroBuf.Readers
{
    internal class LazyReaderBuilder : ReaderBuilderBase
    {
        public LazyReaderBuilder(Type type, ModuleBuilder module)
            : base(type)
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Lazy<>)))
                throw new InvalidOperationException("Expected Lazy but was '" + Type + "'");
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

            var source = il.DeclareLocal(typeof(IntPtr));
            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Stloc(source); // source = &data[index]; stack: []
            context.IncreaseIndexBy1(); // skip type code
            context.SkipValue();
            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Ldloc(source); // stack: [&data[index], source]
            il.Sub(); // stack: [&data[index] - source = data length]
            var length = il.DeclareLocal(typeof(int));
            il.Stloc(length); // length = &data[index] - source; stack: []
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
            il.FreePinnedLocal(dest); // dest = null; stack: []

            var argumentType = Type.GetGenericArguments()[0];
            context.LoadResultByRef(); // stack: [ref result]
            context.LoadSerializerId(); // stack: [ref result, serializerId]
            il.Ldloc(array); // stack: [ref result, serializerId, array]
            context.LoadReader(argumentType); // stack: [ref result, serializerId, array, reader<arg>]
            context.LoadSerializerId(); // stack: [ref result, serializerId, array, reader<arg>, serializerId]
            il.Newobj(readerInvoker.GetConstructor(new[] {typeof(IntPtr), typeof(long)})); // stack: [ref result, serializerId, array, new ReaderInvoker(reader<arg>, serializerId)]
            il.Ldftn(readerInvoker.GetMethod("Read", BindingFlags.Instance | BindingFlags.Public));
            var readDataFuncType = typeof(Func<,>).MakeGenericType(typeof(byte[]), argumentType);
            il.Newobj(readDataFuncType.GetConstructor(new[] {typeof(object), typeof(IntPtr)})); // stack: [ref result, serializerId, array, new Func<byte[], arg>(..)]
            var rawDataType = typeof(RawData<>).MakeGenericType(argumentType);
            il.Newobj(rawDataType.GetConstructor(new[] {typeof(long), typeof(byte[]), readDataFuncType})); // stack: [ref result, new RawData(serializerId, array, func)]
            il.Ldftn(rawDataType.GetMethod("GetValue", BindingFlags.Instance | BindingFlags.Public)); // stack: [ref result, new RawData(..), RawData.GetValue]
            var factoryType = typeof(Func<>).MakeGenericType(argumentType);
            il.Newobj(factoryType.GetConstructor(new[] {typeof(object), typeof(IntPtr)})); // stack: [ref result, new Func<arg>(new RawData(), RawData.GetValue)]
            il.Newobj(Type.GetConstructor(new[] {factoryType})); // stack: [ref result, new Lazy<arg>(new Func<arg>(new RawData(), RawData.GetValue))]
            il.Stind(Type); // result = new Lazy<argument>(array, func); stack: []
        }

        protected override bool IsReference { get { return true; } }

        private Type BuildReaderInvoker()
        {
            var argument = Type.GetGenericArguments()[0];
            var typeBuilder = module.DefineType("ReaderInvoker_" + Type, TypeAttributes.Public | TypeAttributes.Class);
            var reader = typeBuilder.DefineField("reader", typeof(IntPtr), FieldAttributes.Private);
            var serializerId = typeBuilder.DefineField("serializerId", typeof(long), FieldAttributes.Private);
            var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] {typeof(IntPtr), typeof(long)});
            using(var il = new GroboIL(constructor))
            {
                il.Ldarg(0); // stack: [this]
                il.Ldarg(1); // stack: [this, reader]
                il.Stfld(reader); // this.reader = reader; stack: []
                il.Ldarg(0); // stack: [this]
                il.Ldarg(2); // stack: [this, serializerId]
                il.Stfld(serializerId); // this.serializerId = serializerId; stack: []
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
                var context = il.DeclareLocal(typeof(ReaderContext), "context");
                il.Ldarg(0); // stack: [this]
                il.Ldfld(serializerId); // stack: [this.serializerId]
                il.Ldarg(1); // stack: [this.serializerId, data]
                il.Ldlen(); // stack: [this.serializerId, data.Length]
                il.Ldc_I4(0); // stack: [this.serializerId, data.Length, 0]
                il.Ldc_I4(0); // stack: [this.serializerId, data.Length, 0, 0]
                il.Newobj(typeof(ReaderContext).GetConstructor(new[] {typeof(long), typeof(int), typeof(int), typeof(int)})); // stack: [new ReaderContext(this.serializerId, data.Length, 0, 0)]
                il.Stloc(context); // context = new ReaderContext(..); stack: []

                var result = il.DeclareLocal(argument, "result");
                il.Ldloc(pinnedData); // stack: [data]
                il.Conv<IntPtr>(); // stack: [(IntPtr)data]
                il.Ldloca(index); // stack: [(IntPtr)data, ref index]
                il.Ldloca(result); // stack: [(IntPtr)data, ref index, ref result]
                il.Ldloc(context); // stack: [(IntPtr)data, ref index, ref result, context]
                il.Ldarg(0); // stack: [(IntPtr)data, ref index, ref result, context, this]
                il.Ldfld(reader); // stack: [(IntPtr)data, ref index, ref result, context, this.reader]
                var parameterTypes = new[] {typeof(IntPtr), typeof(int).MakeByRefType(), argument.MakeByRefType(), typeof(ReaderContext)};
                il.Calli(CallingConventions.Standard, typeof(void), parameterTypes); // this.reader((IntPtr)data, ref index, ref result, context); stack: []
                il.FreePinnedLocal(pinnedData); // pinnedData = null; stack: []
                var retLabel = il.DefineLabel("ret");
                il.Ldarg(1); // stack: [data]
                il.Ldlen(); // stack: [data.Length]
                il.Ldloc(index); // stack: [data.Length, index]
                il.Beq(retLabel); // if(data.Length == index) goto ret; stack: []
                il.Ldstr("Encountered extra data");
                il.Newobj(typeof(DataCorruptedException).GetConstructor(new[] {typeof(string)}));
                il.Throw();

                il.MarkLabel(retLabel);
                il.Ldloc(result);
                il.Ret();
            }
            return typeBuilder.CreateType();
        }

        private readonly ModuleBuilder module;
        private readonly Type readerInvoker;
    }
}