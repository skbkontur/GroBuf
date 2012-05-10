using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.SizeCounters
{
    internal class ObjectSizeCounterBuilder : SizeCounterBuilderBase<object>
    {
        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;

            var counters = GetCounters(context.Context);
            var countersField = context.Context.BuildConstField<IntPtr[]>("counters_" + Type.Name + "_" + Guid.NewGuid(), field => BuildCountersFieldInitializer(context.Context, field, counters));

            context.LoadObj(); // stack: [obj]
            context.LoadWriteEmpty(); // stack: [obj, writeEmpty]
            context.LoadField(countersField); // stack: [obj, writeEmpty, counters]
            context.LoadObj(); // stack: [obj, writeEmpty, result, ref index, counters, obj]
            il.Emit(OpCodes.Callvirt, getTypeMethod); // stack: [obj, writeEmpty, counters, obj.GetType()]
            il.Emit(OpCodes.Call, getTypeCodeMethod); // stack: [obj, writeEmpty, counters, GroBufHelpers.GetTypeCode(obj.GetType())]
            il.Emit(OpCodes.Ldelem_I); // stack: [obj, writeEmpty, counters[GroBufHelpers.GetTypeCode(obj.GetType())]]
            il.Emit(OpCodes.Dup); // stack: [obj, writeEmpty, counters[GroBufHelpers.GetTypeCode(obj.GetType())], counters[GroBufHelpers.GetTypeCode(obj.GetType())]]
            var returnForNullLabel = il.DefineLabel();
            il.Emit(OpCodes.Brfalse, returnForNullLabel); // if(counters[GroBufHelpers.GetTypeCode(obj.GetType())] == 0) goto returnForNull;
            var parameterTypes = new[] { typeof(object), typeof(bool) };
            il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(int), parameterTypes, null); // stack: [counters[GroBufHelpers.GetTypeCode(obj.GetType())](obj, writeEmpty)]
            il.Emit(OpCodes.Ret);
            il.MarkLabel(returnForNullLabel);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Pop);
            context.ReturnForNull();
        }

        private static Action BuildCountersFieldInitializer(SizeCounterTypeBuilderContext context, FieldInfo field, MethodInfo[] counters)
        {
            var typeBuilder = context.TypeBuilder;
            var method = typeBuilder.DefineMethod(field.Name + "_Init", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldnull); // stack: [null]
            il.Emit(OpCodes.Ldc_I4, counters.Length); // stack: [null, counters.Length]
            il.Emit(OpCodes.Newarr, typeof(IntPtr)); // stack: [null, new IntPtr[counters.Length]]
            il.Emit(OpCodes.Stfld, field); // countersField = new IntPtr[counters.Length]
            il.Emit(OpCodes.Ldnull); // stack: [null]
            il.Emit(OpCodes.Ldfld, field); // stack: [countersField]
            for(int i = 0; i < counters.Length; ++i)
            {
                if(counters[i] == null) continue;
                il.Emit(OpCodes.Dup); // stack: [countersField, countersField]
                il.Emit(OpCodes.Ldc_I4, i); // stack: [countersField, countersField, i]
                il.Emit(OpCodes.Ldftn, counters[i]); // stack: [countersField, countersField, i, counters[i]]
                il.Emit(OpCodes.Stelem_I); // countersField[i] = counters[i]; stack: [countersField]
            }
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);
            return () => typeBuilder.GetMethod(method.Name).Invoke(null, null);
        }

        private static MethodInfo[] GetCounters(SizeCounterTypeBuilderContext context)
        {
            var dict = new[]
                {
                    typeof(bool), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
                    typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(string), typeof(Guid), typeof(DateTime), typeof(Array)
                }.ToDictionary(GroBufTypeCodeMap.GetTypeCode, type => GetCounter(context, type));
            foreach (GroBufTypeCode value in Enum.GetValues(typeof(GroBufTypeCode)))
                if (!dict.ContainsKey(value))
                    dict.Add(value, null);
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
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // stack: [obj]
            if(type.IsClass)
                il.Emit(OpCodes.Castclass, type); // stack: [(type)obj]
            else
            {
                il.Emit(OpCodes.Unbox, type);
                il.Emit(OpCodes.Ldobj, type); // stack: [(type)obj]
            }
            il.Emit(OpCodes.Ldarg_1); // stack: [(type)obj, writeEmpty]
            il.Emit(OpCodes.Call, context.GetCounter(type)); // stack: [count<type>((type)obj, writeEmpty)]
            il.Emit(OpCodes.Ret);
            return method;
        }

        private static readonly MethodInfo getTypeMethod = ((MethodCallExpression)((Expression<Func<object, Type>>)(obj => obj.GetType())).Body).Method;
        private static readonly MethodInfo getTypeCodeMethod = ((MethodCallExpression)((Expression<Func<Type, GroBufTypeCode>>)(type => GroBufTypeCodeMap.GetTypeCode(type))).Body).Method;
    }
}