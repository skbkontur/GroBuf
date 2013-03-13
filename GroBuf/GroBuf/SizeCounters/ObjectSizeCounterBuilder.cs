using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit;

namespace GroBuf.SizeCounters
{
    internal class ObjectSizeCounterBuilder : SizeCounterBuilderBase
    {
        public ObjectSizeCounterBuilder()
            : base(typeof(object))
        {
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;

            var counters = GetCounters(context.Context);
            var countersField = context.Context.BuildConstField<IntPtr[]>("counters_" + Type.Name + "_" + Guid.NewGuid(), field => BuildCountersFieldInitializer(context.Context, field, counters));

            context.LoadObj(); // stack: [obj]
            context.LoadWriteEmpty(); // stack: [obj, writeEmpty]
            context.LoadField(countersField); // stack: [obj, writeEmpty, counters]
            context.LoadObj(); // stack: [obj, writeEmpty, result, ref index, counters, obj]
            il.Call(getTypeMethod, Type); // stack: [obj, writeEmpty, counters, obj.GetType()]
            il.Call(getTypeCodeMethod); // stack: [obj, writeEmpty, counters, GroBufHelpers.GetTypeCode(obj.GetType())]
            il.Ldelem(typeof(IntPtr)); // stack: [obj, writeEmpty, counters[GroBufHelpers.GetTypeCode(obj.GetType())]]
            il.Dup(); // stack: [obj, writeEmpty, counters[GroBufHelpers.GetTypeCode(obj.GetType())], counters[GroBufHelpers.GetTypeCode(obj.GetType())]]
            var returnForNullLabel = il.DefineLabel("returnForNull");
            il.Brfalse(returnForNullLabel); // if(counters[GroBufHelpers.GetTypeCode(obj.GetType())] == 0) goto returnForNull;
            var parameterTypes = new[] {typeof(object), typeof(bool)};
            il.Calli(CallingConventions.Standard, typeof(int), parameterTypes); // stack: [counters[GroBufHelpers.GetTypeCode(obj.GetType())](obj, writeEmpty)]
            il.Ret();
            il.MarkLabel(returnForNullLabel);
            il.Pop();
            il.Pop();
            il.Pop();
            context.ReturnForNull();
        }

        private static Action BuildCountersFieldInitializer(SizeCounterTypeBuilderContext context, FieldInfo field, MethodInfo[] counters)
        {
            var typeBuilder = context.TypeBuilder;
            var method = typeBuilder.DefineMethod(field.Name + "_Init", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
            var il = new GroboIL(method);
            il.Ldc_I4(counters.Length); // stack: [counters.Length]
            il.Newarr(typeof(IntPtr)); // stack: [new IntPtr[counters.Length]]
            il.Stfld(field); // countersField = new IntPtr[counters.Length]
            il.Ldfld(field); // stack: [countersField]
            for(int i = 0; i < counters.Length; ++i)
            {
                if(counters[i] == null) continue;
                il.Dup(); // stack: [countersField, countersField]
                il.Ldc_I4(i); // stack: [countersField, countersField, i]
                il.Ldftn(counters[i]); // stack: [countersField, countersField, i, counters[i]]
                il.Stelem(typeof(IntPtr)); // countersField[i] = counters[i]; stack: [countersField]
            }
            il.Pop();
            il.Ret();
            return () => typeBuilder.GetMethod(method.Name).Invoke(null, null);
        }

        private static MethodInfo[] GetCounters(SizeCounterTypeBuilderContext context)
        {
            var dict = new[]
                {
                    typeof(bool), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
                    typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(string), typeof(Guid), typeof(DateTime), typeof(Array)
                }.ToDictionary(GroBufTypeCodeMap.GetTypeCode, type => GetCounter(context, type));
            foreach(GroBufTypeCode value in Enum.GetValues(typeof(GroBufTypeCode)))
            {
                if(!dict.ContainsKey(value))
                    dict.Add(value, null);
            }
            int max = dict.Keys.Cast<int>().Max();
            var result = new MethodInfo[max + 1];
            foreach(var entry in dict)
                result[(int)entry.Key] = entry.Value;
            return result;
        }

        private static MethodInfo GetCounter(SizeCounterTypeBuilderContext context, Type type)
        {
            var method = context.TypeBuilder.DefineMethod("CastTo_" + type.Name + "_AndCount_" + Guid.NewGuid(), MethodAttributes.Public | MethodAttributes.Static, typeof(int),
                                                          new[]
                                                              {
                                                                  typeof(object), typeof(bool)
                                                              });
            var il = new GroboIL(method);
            il.Ldarg(0); // stack: [obj]
            if(type.IsClass)
                il.Castclass(type); // stack: [(type)obj]
            else
                il.Unbox_Any(type); // stack: [(type)obj]
            il.Ldarg(1); // stack: [(type)obj, writeEmpty]
            il.Call(context.GetCounter(type)); // stack: [count<type>((type)obj, writeEmpty)]
            il.Ret();
            return method;
        }

        private static readonly MethodInfo getTypeMethod = ((MethodCallExpression)((Expression<Func<object, Type>>)(obj => obj.GetType())).Body).Method;
        private static readonly MethodInfo getTypeCodeMethod = ((MethodCallExpression)((Expression<Func<Type, GroBufTypeCode>>)(type => GroBufTypeCodeMap.GetTypeCode(type))).Body).Method;
    }
}