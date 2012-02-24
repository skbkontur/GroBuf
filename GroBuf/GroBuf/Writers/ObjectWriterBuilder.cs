using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.Writers
{
    internal class ObjectWriterBuilder : WriterBuilderBase<object>
    {
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
            il.Emit(OpCodes.Callvirt, getTypeMethod); // stack: [obj, writeEmpty, result, ref index, writers, obj.GetType()]
            il.Emit(OpCodes.Call, getTypeCodeMethod); // stack: [obj, writeEmpty, result, ref index, writers, GroBufHelpers.GetTypeCode(obj.GetType())]
            il.Emit(OpCodes.Ldelem_I); // stack: [obj, writeEmpty, result, ref index, writers[GroBufHelpers.GetTypeCode(obj.GetType())]]
            var parameterTypes = new[] {typeof(object), typeof(bool), typeof(byte*), typeof(int).MakeByRefType()};
            context.Il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(void), parameterTypes, null); // writers[GroBufHelpers.GetTypeCode(obj.GetType())](obj, writeEmpty, result, ref index); stack: []
        }

        private static Action BuildWritersFieldInitializer(WriterTypeBuilderContext context, FieldInfo field, MethodInfo[] writers)
        {
            var typeBuilder = context.TypeBuilder;
            var method = typeBuilder.DefineMethod(field.Name + "_Init", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldnull); // stack: [null]
            il.Emit(OpCodes.Ldc_I4, writers.Length); // stack: [null, writers.Length]
            il.Emit(OpCodes.Newarr, typeof(IntPtr)); // stack: [null, new IntPtr[writers.Length]]
            il.Emit(OpCodes.Stfld, field); // writersField = new IntPtr[writers.Length]
            il.Emit(OpCodes.Ldnull); // stack: [null]
            il.Emit(OpCodes.Ldfld, field); // stack: [writersField]
            for(int i = 0; i < writers.Length; ++i)
            {
                if(writers[i] == null) continue;
                il.Emit(OpCodes.Dup); // stack: [writersField, writersField]
                il.Emit(OpCodes.Ldc_I4, i); // stack: [writersField, writersField, i]
                il.Emit(OpCodes.Ldftn, writers[i]); // stack: [writersField, writersField, i, writers[i]]
                il.Emit(OpCodes.Stelem_I); // writersField[i] = writers[i]; stack: [writersField]
            }
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);
            return () => typeBuilder.GetMethod(method.Name).Invoke(null, null);
        }

        private static MethodInfo[] GetWriters(WriterTypeBuilderContext context)
        {
            var dict = new[]
                {
                    typeof(bool), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
                    typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(string), typeof(Guid), typeof(DateTime)
                }.ToDictionary(GroBufHelpers.GetTypeCode, type => GetWriter(context, type));
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
            il.Emit(OpCodes.Ldarg_2); // stack: [(type)obj, writeEmpty, result]
            il.Emit(OpCodes.Ldarg_3); // stack: [(type)obj, writeEmpty, result, ref index]
            il.Emit(OpCodes.Call, context.GetWriter(type)); // write<type>((type)obj, writeEmpty, result, ref index)
            il.Emit(OpCodes.Ret);
            return method;
        }

        private static readonly MethodInfo getTypeMethod = ((MethodCallExpression)((Expression<Func<object, Type>>)(obj => obj.GetType())).Body).Method;
        private static readonly MethodInfo getTypeCodeMethod = ((MethodCallExpression)((Expression<Func<Type, GroBufTypeCode>>)(type => GroBufHelpers.GetTypeCode(type))).Body).Method;
    }
}