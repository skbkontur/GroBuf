using System;
using System.Reflection;

namespace GroBuf.SizeCounters
{
    internal class LazySizeCounterBuilder : SizeCounterBuilderBase
    {
        public LazySizeCounterBuilder(Type type)
            : base(type)
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Lazy<>)))
                throw new InvalidOperationException("Expected Lazy but was '" + Type + "'");
        }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
            context.BuildConstants(Type.GetGenericArguments()[0]);
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;
            var argument = Type.GetGenericArguments()[0];

            context.LoadObj(); // stack: [obj]
            var factoryField = Type.GetField("m_valueFactory", BindingFlags.Instance | BindingFlags.NonPublic);
            il.Ldfld(factoryField); // stack: [obj.m_valueFactory]
            var factory = il.DeclareLocal(typeof(Func<>).MakeGenericType(argument));
            il.Dup();
            il.Stloc(factory); // factory = obj.m_valueFactory; stack: [factory]
            var countUsual = il.DefineLabel("countUsual");
            il.Brfalse(countUsual); // if(factory == null) goto countUsual; stack: []
            il.Ldloc(factory); // stack: [factory]
            string targetFieldName = GroBufHelpers.IsMono ? "m_target" : "_target";
            il.Ldfld(typeof(Delegate).GetField(targetFieldName, BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [factory.target]
            var rawData = il.DeclareLocal(typeof(RawData<>).MakeGenericType(Type.GetGenericArguments()));
            il.Isinst(rawData.Type); // stack: [factory.target as RawData]
            il.Dup();
            il.Stloc(rawData); // rawData = factory.target as RawData; stack: [rawData]
            il.Brfalse(countUsual); // if(!(rawData is RawData)) goto countUsual; stack: []
            il.Ldloc(rawData); // stack: [rawData]
            il.Ldfld(rawData.Type.GetField("serializerId", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [rawData.serializerId]
            context.LoadSerializerId(); // stack: [rawData.serializerId, context.serializerId]
            il.Bne_Un(countUsual); // if(rawData.serializerId != context.serializerId) goto countUsual; stack: []
            il.Ldloc(rawData); // stack: [rawData]
            il.Ldfld(rawData.Type.GetField("data", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [rawData.data]
            il.Ldlen(); // stack: [rawData.data.Length]
            il.Ret();

            il.MarkLabel(countUsual);
            context.LoadObj(); // stack: [obj]
            il.Call(Type.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public).GetGetMethod()); // stack: [obj.Value]
            context.LoadWriteEmpty(); // stack: [obj.Value, writeEmpty]
            context.LoadContext(); // stack: [obj.Value, writeEmpty, context]
            context.CallSizeCounter(argument); // stack: [counter(obj.Value, writeEmpty, context)]
        }

        protected override bool IsReference { get { return false; } }
    }
}