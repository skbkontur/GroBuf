using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;

namespace GroBuf.SizeCounters
{
    internal class ObjectSizeCounterBuilder : SizeCounterBuilderBase
    {
        public ObjectSizeCounterBuilder()
            : base(typeof(object))
        {
        }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
            context.SetFields(Type, new[]
                {
                    new KeyValuePair<string, Type>("counters_" + Type.Name + "_" + Guid.NewGuid(), typeof(IntPtr[])),
                    new KeyValuePair<string, Type>("delegates_" + Type.Name + "_" + Guid.NewGuid(), typeof(Delegate[]))
                });
            Array.ForEach(primitiveTypes, type => context.BuildConstants(type));
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;

            var counters = GetCounters(context);
            var countersField = context.Context.InitConstField(Type, 0, counters.Select(pair => pair.Value).ToArray());
            context.Context.InitConstField(Type, 1, counters.Select(pair => pair.Key).ToArray());

            context.LoadObj(); // stack: [obj]
            context.LoadWriteEmpty(); // stack: [obj, writeEmpty]
            context.LoadContext(); // stack: [obj, writeEmpty, context]
            context.LoadField(countersField); // stack: [obj, writeEmpty, context, counters]
            context.LoadObj(); // stack: [obj, writeEmpty, context, ref index, counters, obj]
            il.Call(getTypeMethod, Type); // stack: [obj, writeEmpty, context, counters, obj.GetType()]
            il.Call(getTypeCodeMethod); // stack: [obj, writeEmpty, context, counters, GroBufHelpers.GetTypeCode(obj.GetType())]
            il.Ldelem(typeof(IntPtr)); // stack: [obj, writeEmpty, context, counters[GroBufHelpers.GetTypeCode(obj.GetType())]]
            il.Dup(); // stack: [obj, writeEmpty, context, counters[GroBufHelpers.GetTypeCode(obj.GetType())], counters[GroBufHelpers.GetTypeCode(obj.GetType())]]
            var returnForNullLabel = il.DefineLabel("returnForNull");
            il.Brfalse(returnForNullLabel); // if(counters[GroBufHelpers.GetTypeCode(obj.GetType())] == 0) goto returnForNull;
            var parameterTypes = new[] {typeof(object), typeof(bool), typeof(WriterContext)};
            il.Calli(CallingConventions.Standard, typeof(int), parameterTypes); // stack: [counters[GroBufHelpers.GetTypeCode(obj.GetType())](obj, writeEmpty)]
            il.Ret();
            il.MarkLabel(returnForNullLabel);
            il.Pop();
            il.Pop();
            il.Pop();
            il.Pop();
            context.ReturnForNull();
        }

        protected override bool IsReference { get { return false; } }

        private static KeyValuePair<Delegate, IntPtr>[] GetCounters(SizeCounterMethodBuilderContext context)
        {
            var dict = primitiveTypes.ToDictionary(GroBufTypeCodeMap.GetTypeCode, type => GetCounter(context, type));
            foreach(GroBufTypeCode value in Enum.GetValues(typeof(GroBufTypeCode)))
            {
                if(!dict.ContainsKey(value))
                    dict.Add(value, new KeyValuePair<Delegate, IntPtr>(null, IntPtr.Zero));
            }
            int max = dict.Keys.Cast<int>().Max();
            var result = new KeyValuePair<Delegate, IntPtr>[max + 1];
            foreach(var entry in dict)
                result[(int)entry.Key] = entry.Value;
            return result;
        }

        private static KeyValuePair<Delegate, IntPtr> GetCounter(SizeCounterMethodBuilderContext context, Type type)
        {
            var method = new DynamicMethod("CastTo_" + type.Name + "_AndCount_" + Guid.NewGuid(), typeof(int), new[] {typeof(object), typeof(bool), typeof(WriterContext)}, context.Context.Module, true);
            var il = new GroboIL(method);
            il.Ldarg(0); // stack: [obj]
            if(type.IsValueType)
                il.Unbox_Any(type); // stack: [(type)obj]
            else
                il.Castclass(type); // stack: [(type)obj]
            il.Ldarg(1); // stack: [(type)obj, writeEmpty]
            il.Ldarg(2); // stack: [(type)obj, writeEmpty, context]
            context.CallSizeCounter(il, type);
//            var counter = context.Context.GetCounter(type).Pointer;
//            if(counter == IntPtr.Zero)
//                throw new InvalidOperationException("Attempt to call method at Zero pointer");
//            il.Ldc_IntPtr(counter);
//            il.Calli(CallingConventions.Standard, typeof(int), new[] {type, typeof(bool), typeof(WriterContext)}); // stack: [count<type>((type)obj, writeEmpty, context)]
            il.Ret();
            var @delegate = method.CreateDelegate(typeof(SizeCounterDelegate<object>));
            return new KeyValuePair<Delegate, IntPtr>(@delegate, GroBufHelpers.ExtractDynamicMethodPointer(method));
        }

        private static readonly MethodInfo getTypeMethod = ((MethodCallExpression)((Expression<Func<object, Type>>)(obj => obj.GetType())).Body).Method;
        private static readonly MethodInfo getTypeCodeMethod = ((MethodCallExpression)((Expression<Func<Type, GroBufTypeCode>>)(type => GroBufTypeCodeMap.GetTypeCode(type))).Body).Method;

        private static readonly Type[] primitiveTypes = new[]
            {
                typeof(bool), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong),
                typeof(float), typeof(double), typeof(decimal), typeof(string), typeof(Guid), typeof(DateTime), typeof(Array), typeof(Hashtable)
            };
    }
}