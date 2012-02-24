using System;
using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.Readers
{
    internal class ObjectReaderBuilder : ReaderBuilderBase<object>
    {
        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            var il = context.Il;

            var readers = GetReaders(context.Context);
            var readersField = context.Context.BuildConstField<IntPtr[]>("readers_" + Type.Name + "_" + Guid.NewGuid(), field => BuildReadersFieldInitializer(context.Context, field, readers));

            context.LoadData(); // stack: [data]
            context.LoadIndexByRef(); // stack: [data, ref index]
            context.LoadDataLength(); // stack: [data, ref index, dataLength]
            context.LoadResultByRef(); // stack: [data, ref index, dataLength, ref result]
            context.LoadField(readersField); // stack: [data, ref index, dataLength, ref result, readers]
            il.Emit(OpCodes.Ldloc, context.TypeCode); // stack: [data, ref index, dataLength, ref result, readers, typeCode]
            il.Emit(OpCodes.Ldelem_I); // stack: [data, ref index, dataLength, ref result, readers[typeCode]]
            il.Emit(OpCodes.Dup); // stack: [data, ref index, dataLength, ref result, readers[typeCode], readers[typeCode]]
            var skipValueLabel = il.DefineLabel();
            il.Emit(OpCodes.Brfalse, skipValueLabel); // if(readers[typeCode] == 0) goto skipValue;
            var parameterTypes = new[] {typeof(byte*), typeof(int).MakeByRefType(), typeof(int), typeof(object).MakeByRefType()};
            context.Il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(void), parameterTypes, null); // readers[typeCode](data, ref index, dataLength, ref result); stack: []
            il.Emit(OpCodes.Ret);

            il.MarkLabel(skipValueLabel);
            context.IncreaseIndexBy1();
            context.SkipValue();
        }

        private static Action BuildReadersFieldInitializer(ReaderTypeBuilderContext context, FieldInfo field, MethodInfo[] readers)
        {
            var typeBuilder = context.TypeBuilder;
            var method = typeBuilder.DefineMethod(field.Name + "_Init", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldnull); // stack: [null]
            il.Emit(OpCodes.Ldc_I4, readers.Length); // stack: [null, readers.Length]
            il.Emit(OpCodes.Newarr, typeof(IntPtr)); // stack: [null, new IntPtr[readers.Length]]
            il.Emit(OpCodes.Stfld, field); // readersField = new IntPtr[readers.Length]
            il.Emit(OpCodes.Ldnull); // stack: [null]
            il.Emit(OpCodes.Ldfld, field); // stack: [readersField]
            for(int i = 0; i < readers.Length; ++i)
            {
                if(readers[i] == null) continue;
                il.Emit(OpCodes.Dup); // stack: [readersField, readersField]
                il.Emit(OpCodes.Ldc_I4, i); // stack: [readersField, readersField, i]
                il.Emit(OpCodes.Ldftn, readers[i]); // stack: [readersField, readersField, i, readers[i]]
                il.Emit(OpCodes.Stelem_I); // readersField[i] = readers[i]; stack: [readersField]
            }
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);
            return () => typeBuilder.GetMethod(method.Name).Invoke(null, null);
        }

        private static MethodInfo[] GetReaders(ReaderTypeBuilderContext context)
        {
            var result = new MethodInfo[256];
            foreach(var type in new[]
                {
                    typeof(bool), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
                    typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(string), typeof(Guid), typeof(DateTime)
                })
                result[(int)GroBufHelpers.GetTypeCode(type)] = GetReader(context, type);
            return result;
        }

        private static MethodInfo GetReader(ReaderTypeBuilderContext context, Type type)
        {
            var method = context.TypeBuilder.DefineMethod("Read_" + type.Name + "_AndCastToObject_" + Guid.NewGuid(), MethodAttributes.Public | MethodAttributes.Static, typeof(void),
                                                          new[]
                                                              {
                                                                  typeof(byte*), typeof(int).MakeByRefType(), typeof(int), typeof(object).MakeByRefType()
                                                              });
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_3); // stack: [ref result]
            il.Emit(OpCodes.Ldarg_0); // stack: [ref result, data]
            il.Emit(OpCodes.Ldarg_1); // stack: [ref result, data, ref index]
            il.Emit(OpCodes.Ldarg_2); // stack: [ref result, data, ref index, dataLength]
            var value = il.DeclareLocal(type);
            il.Emit(OpCodes.Ldloca, value); // stack: [ref result, data, ref index, dataLength, ref value]
            il.Emit(OpCodes.Call, context.GetReader(type)); // read<type>(data, ref index, dataLength, ref value); stack: [ref result]
            il.Emit(OpCodes.Ldloc, value); // stack: [ref result, value]
            if(!type.IsClass)
                il.Emit(OpCodes.Box, type); // stack: [ref result, (object)value]
            il.Emit(OpCodes.Stind_Ref); // result = (object)value
            il.Emit(OpCodes.Ret);
            return method;
        }
    }
}