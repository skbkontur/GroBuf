using System;
using System.Reflection;

namespace GroBuf.Writers
{
    internal class LazyWriterBuilder : WriterBuilderBase
    {
        public LazyWriterBuilder(Type type)
            : base(type)
        {
            if (!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Lazy<>)))
                throw new InvalidOperationException("Expected Lazy but was " + Type);
        }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            context.BuildConstants(Type.GetGenericArguments()[0]);
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            var argument = Type.GetGenericArguments()[0];

            context.LoadObj(); // stack: [obj]
            var factoryField = Type.GetField(PlatformHelpers.LazyValueFactoryFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            il.Ldfld(factoryField); // stack: [obj.m_valueFactory]
            var factory = il.DeclareLocal(typeof(Func<>).MakeGenericType(argument));
            il.Dup();
            il.Stloc(factory); // factory = obj.m_valueFactory; stack: [factory]
            var writeUsual = il.DefineLabel("writeUsual");
            il.Brfalse(writeUsual); // if(factory == null) goto writeUsual; stack: []
            il.Ldloc(factory);
            il.Ldfld(typeof(Delegate).GetField(PlatformHelpers.DelegateTargetFieldName, BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [factory.target]
            var rawData = il.DeclareLocal(typeof(RawData<>).MakeGenericType(Type.GetGenericArguments()));
            il.Isinst(rawData.Type); // stack: [factory.target as RawData]
            il.Dup();
            il.Stloc(rawData); // rawData = factory.target as RawData; stack: [rawData]
            il.Brfalse(writeUsual); // if(!(rawData is RawData)) goto writeUsual; stack: []
            il.Ldloc(rawData); // stack: [rawData]
            il.Ldfld(rawData.Type.GetField("serializerId", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [rawData.serializerId]
            context.LoadSerializerId(); // stack: [rawData.serializerId, context.serializerId]
            il.Bne_Un(writeUsual); // if(rawData.serializerId != context.serializerId) goto writeUsual; stack: []

            var data = il.DeclareLocal(typeof(byte).MakeByRefType(), true);
            var length = il.DeclareLocal(typeof(int));
            il.Ldloc(rawData); // stack: [rawData]
            il.Ldfld(rawData.Type.GetField("data", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [rawData.data]

            il.Dup(); // stack: [rawData.data, rawData.data]
            il.Ldlen(); // stack: [rawData.data, rawData.data.Length]
            il.Stloc(length); // length = rawData.data.Length; stack: [rawData.data]
            il.Ldc_I4(0); // stack: [rawData.data, 0]
            il.Ldelema(typeof(byte)); // stack: [&rawData.data[0]]
            il.Stloc(data); // data = &rawData.data; stack: []
            il.Ldloc(length);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&result[index]]
            il.Ldloc(data); // stack: [&result[index], data]
            il.Ldloc(length); // stack: [&result[index], data, data.Length]
            il.Cpblk(); // result[index] = data; stack: []
            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Ldloc(length); // stack: [ref index, index, data.Length]
            il.Add(); // stack: [ref index, index + data.Length]
            il.Stind(typeof(int)); // index = index + data.Length; stack: []
            il.FreePinnedLocal(data); // data = null; stack: []
            il.Ret();

            il.MarkLabel(writeUsual);
            context.LoadObj(); // stack: [obj]
            il.Call(Type.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public).GetGetMethod()); // stack: [obj.Value]
            context.LoadWriteEmpty(); // stack: [obj.Value, writeEmpty]
            context.LoadResult(); // stack: [obj.Value, writeEmpty, result]
            context.LoadIndexByRef(); // stack: [obj.Value, writeEmpty, result, ref index]
            context.LoadContext(); // stack: [obj.Value, writeEmpty, result, ref index, context]
            context.CallWriter(argument); // writer(obj.Value, writeEmpty, result, ref index, context)
        }

        protected override bool IsReference => false;
    }
}