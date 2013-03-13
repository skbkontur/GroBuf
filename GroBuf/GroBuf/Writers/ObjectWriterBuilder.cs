using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit;

namespace GroBuf.Writers
{
    internal class ObjectWriterBuilder : WriterBuilderBase
    {
        public ObjectWriterBuilder()
            : base(typeof(object))
        {
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;

            var writers = GetWriters(context.Context);
            var writersField = context.Context.BuildConstField<IntPtr[]>("writers_" + Type.Name + "_" + Guid.NewGuid(), field => BuildWritersFieldInitializer(context.Context, field, writers));

            context.LoadObj(); // stack: [obj]
            context.LoadWriteEmpty(); // stack: [obj, writeEmpty]
            context.LoadResult(); // stack: [obj, writeEmpty, result]
            context.LoadIndexByRef(); // stack: [obj, writeEmpty, result, ref index]
            context.LoadField(writersField); // stack: [obj, writeEmpty, result, ref index, writers]
            context.LoadObj(); // stack: [obj, writeEmpty, result, ref index, writers, obj]
            il.Call(getTypeMethod, Type); // stack: [obj, writeEmpty, result, ref index, writers, obj.GetType()]
            il.Call(getTypeCodeMethod); // stack: [obj, writeEmpty, result, ref index, writers, GroBufHelpers.GetTypeCode(obj.GetType())]
            il.Ldelem(typeof(IntPtr)); // stack: [obj, writeEmpty, result, ref index, writers[GroBufHelpers.GetTypeCode(obj.GetType())]]
            il.Dup(); // stack: [obj, writeEmpty, result, ref index, writers[GroBufHelpers.GetTypeCode(obj.GetType())], writers[GroBufHelpers.GetTypeCode(obj.GetType())]]
            var writeNullLabel = il.DefineLabel("writeNull");
            il.Brfalse(writeNullLabel); // if(writers[GroBufHelpers.GetTypeCode(obj.GetType())] == 0) goto writeNull;
            var parameterTypes = new[] {typeof(object), typeof(bool), typeof(byte*), typeof(int).MakeByRefType()};
            il.Calli(CallingConventions.Standard, typeof(void), parameterTypes); // writers[GroBufHelpers.GetTypeCode(obj.GetType())](obj, writeEmpty, result, ref index); stack: []
            il.Ret();
            il.MarkLabel(writeNullLabel);
            il.Pop();
            il.Pop();
            il.Pop();
            il.Pop();
            il.Pop();
            context.WriteNull();
        }

        private static Action BuildWritersFieldInitializer(WriterTypeBuilderContext context, FieldInfo field, MethodInfo[] writers)
        {
            var typeBuilder = context.TypeBuilder;
            var method = typeBuilder.DefineMethod(field.Name + "_Init", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
            var il = new GroboIL(method);
            il.Ldc_I4(writers.Length); // stack: [writers.Length]
            il.Newarr(typeof(IntPtr)); // stack: [new IntPtr[writers.Length]]
            il.Stfld(field); // writersField = new IntPtr[writers.Length]
            il.Ldfld(field); // stack: [writersField]
            for(int i = 0; i < writers.Length; ++i)
            {
                if(writers[i] == null) continue;
                il.Dup(); // stack: [writersField, writersField]
                il.Ldc_I4(i); // stack: [writersField, writersField, i]
                il.Ldftn(writers[i]); // stack: [writersField, writersField, i, writers[i]]
                il.Stelem(typeof(IntPtr)); // writersField[i] = writers[i]; stack: [writersField]
            }
            il.Pop();
            il.Ret();
            return () => typeBuilder.GetMethod(method.Name).Invoke(null, null);
        }

        private static MethodInfo[] GetWriters(WriterTypeBuilderContext context)
        {
            var dict = new[]
                {
                    typeof(bool), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
                    typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(string), typeof(Guid), typeof(DateTime), typeof(Array)
                }.ToDictionary(GroBufTypeCodeMap.GetTypeCode, type => GetWriter(context, type));
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

        private static MethodInfo GetWriter(WriterTypeBuilderContext context, Type type)
        {
            var method = context.TypeBuilder.DefineMethod("CastTo_" + type.Name + "_AndWrite_" + Guid.NewGuid(), MethodAttributes.Public | MethodAttributes.Static, typeof(void),
                                                          new[]
                                                              {
                                                                  typeof(object), typeof(bool), typeof(byte*), typeof(int).MakeByRefType()
                                                              });
            var il = new GroboIL(method);
            il.Ldarg(0); // stack: [obj]
            if(type.IsClass)
                il.Castclass(type); // stack: [(type)obj]
            else
                il.Unbox_Any(type); // stack: [(type)obj]
            il.Ldarg(1); // stack: [(type)obj, writeEmpty]
            il.Ldarg(2); // stack: [(type)obj, writeEmpty, result]
            il.Ldarg(3); // stack: [(type)obj, writeEmpty, result, ref index]
            il.Call(context.GetWriter(type)); // write<type>((type)obj, writeEmpty, result, ref index)
            il.Ret();
            return method;
        }

        private static readonly MethodInfo getTypeMethod = ((MethodCallExpression)((Expression<Func<object, Type>>)(obj => obj.GetType())).Body).Method;
        private static readonly MethodInfo getTypeCodeMethod = ((MethodCallExpression)((Expression<Func<Type, GroBufTypeCode>>)(type => GroBufTypeCodeMap.GetTypeCode(type))).Body).Method;
    }
}