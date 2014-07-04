using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;

namespace GroBuf.Writers
{
    internal class ObjectWriterBuilder : WriterBuilderBase
    {
        public ObjectWriterBuilder()
            : base(typeof(object))
        {
        }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            context.SetFields(Type, new[]
                {
                    new KeyValuePair<string, Type>("writers_" + Type.Name + "_" + Guid.NewGuid(), typeof(IntPtr[])),
                    new KeyValuePair<string, Type>("delegates_" + Type.Name + "_" + Guid.NewGuid(), typeof(Delegate[])),
                });
            Array.ForEach(primitiveTypes, type => context.BuildConstants(type));
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;

            var writers = GetWriters(context.Context);
            var writersField = context.Context.InitConstField(Type, 0, writers.Select(pair => pair.Value).ToArray());
            context.Context.InitConstField(Type, 1, writers.Select(pair => pair.Key).ToArray());

            context.LoadObj(); // stack: [obj]
            context.LoadWriteEmpty(); // stack: [obj, writeEmpty]
            context.LoadResult(); // stack: [obj, writeEmpty, result]
            context.LoadIndexByRef(); // stack: [obj, writeEmpty, result, ref index]
            context.LoadContext(); // stack: [obj, writeEmpty, result, ref index, context]
            context.LoadField(writersField); // stack: [obj, writeEmpty, result, ref index, context, writers]
            context.LoadObj(); // stack: [obj, writeEmpty, result, ref index, context, writers, obj]
            il.Call(getTypeMethod, Type); // stack: [obj, writeEmpty, result, ref index, context, writers, obj.GetType()]
            il.Call(getTypeCodeMethod); // stack: [obj, writeEmpty, result, ref index, context, writers, GroBufHelpers.GetTypeCode(obj.GetType())]
            il.Ldelem(typeof(IntPtr)); // stack: [obj, writeEmpty, result, ref index, context, writers[GroBufHelpers.GetTypeCode(obj.GetType())]]
            il.Dup(); // stack: [obj, writeEmpty, result, ref index, context, writers[GroBufHelpers.GetTypeCode(obj.GetType())], writers[GroBufHelpers.GetTypeCode(obj.GetType())]]
            var writeNullLabel = il.DefineLabel("writeNull");
            il.Brfalse(writeNullLabel); // if(writers[GroBufHelpers.GetTypeCode(obj.GetType())] == 0) goto writeNull;
            var parameterTypes = new[] {typeof(object), typeof(bool), typeof(byte*), typeof(int).MakeByRefType(), typeof(WriterContext)};
            il.Calli(CallingConventions.Standard, typeof(void), parameterTypes); // writers[GroBufHelpers.GetTypeCode(obj.GetType())](obj, writeEmpty, result, ref index, context); stack: []
            il.Ret();
            il.MarkLabel(writeNullLabel);
            // todo мутное место, нафига эти нелепые Pop?
            il.Pop();
            il.Pop();
            il.Pop();
            il.Pop();
            il.Pop();
            il.Pop();
            context.WriteNull();
        }

        private static KeyValuePair<Delegate, IntPtr>[] GetWriters(WriterTypeBuilderContext context)
        {
            var dict = primitiveTypes.ToDictionary(GroBufTypeCodeMap.GetTypeCode, type => GetWriter(context, type));
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

        private static KeyValuePair<Delegate, IntPtr> GetWriter(WriterTypeBuilderContext context, Type type)
        {
            var method = new DynamicMethod("CastTo_" + type.Name + "_AndWrite_" + Guid.NewGuid(), typeof(void),
                                           new[]
                                               {
                                                   typeof(object), typeof(bool), typeof(IntPtr), typeof(int).MakeByRefType(), typeof(WriterContext)
                                               }, context.Module, true);
            var il = new GroboIL(method);
            il.Ldarg(0); // stack: [obj]
            if(type.IsValueType)
                il.Unbox_Any(type); // stack: [(type)obj]
//            else
//                il.Castclass(type); // stack: [(type)obj]
            il.Ldarg(1); // stack: [(type)obj, writeEmpty]
            il.Ldarg(2); // stack: [(type)obj, writeEmpty, result]
            il.Ldarg(3); // stack: [(type)obj, writeEmpty, result, ref index]
            il.Ldarg(4); // stack: [(type)obj, writeEmpty, result, ref index, context]
            var writer = context.GetWriter(type).Pointer;
            if(writer == IntPtr.Zero)
                throw new InvalidOperationException();
            il.Ldc_IntPtr(writer);
            il.Calli(CallingConventions.Standard, typeof(void), new[] {type, typeof(bool), typeof(IntPtr), typeof(int).MakeByRefType(), typeof(WriterContext)}); // write<type>((type)obj, writeEmpty, result, ref index, context)
            il.Ret();
            var @delegate = method.CreateDelegate(typeof(WriterDelegate<object>));
            return new KeyValuePair<Delegate, IntPtr>(@delegate, GroBufHelpers.ExtractDynamicMethodPointer(method));
        }

        private static readonly MethodInfo getTypeMethod = ((MethodCallExpression)((Expression<Func<object, Type>>)(obj => obj.GetType())).Body).Method;
        private static readonly MethodInfo getTypeCodeMethod = ((MethodCallExpression)((Expression<Func<Type, GroBufTypeCode>>)(type => GroBufTypeCodeMap.GetTypeCode(type))).Body).Method;

        private static readonly Type[] primitiveTypes = new[]
            {
                typeof(bool), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong),
                typeof(decimal), typeof(float), typeof(double), typeof(string), typeof(Guid), typeof(DateTime), typeof(Array)
            };
    }
}