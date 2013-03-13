using System;
using System.Reflection;

using GrEmit;

namespace GroBuf.Readers
{
    internal class ObjectReaderBuilder : ReaderBuilderBase
    {
        public ObjectReaderBuilder()
            : base(typeof(object))
        {
        }

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
            il.Ldloc(context.TypeCode); // stack: [data, ref index, dataLength, ref result, readers, typeCode]
            il.Ldelem(typeof(IntPtr)); // stack: [data, ref index, dataLength, ref result, readers[typeCode]]
            il.Dup(); // stack: [data, ref index, dataLength, ref result, readers[typeCode], readers[typeCode]]
            var skipValueLabel = il.DefineLabel("skipValue");
            il.Brfalse(skipValueLabel); // if(readers[typeCode] == 0) goto skipValue;
            var parameterTypes = new[] {typeof(byte*), typeof(int).MakeByRefType(), typeof(int), typeof(object).MakeByRefType()};
            il.Calli(CallingConventions.Standard, typeof(void), parameterTypes); // readers[typeCode](data, ref index, dataLength, ref result); stack: []
            il.Ret();
            il.MarkLabel(skipValueLabel);
            il.Pop();
            il.Pop();
            il.Pop();
            il.Pop();
            il.Pop();
            context.IncreaseIndexBy1();
            context.SkipValue();
        }

        private static Action BuildReadersFieldInitializer(ReaderTypeBuilderContext context, FieldInfo field, MethodInfo[] readers)
        {
            var typeBuilder = context.TypeBuilder;
            var method = typeBuilder.DefineMethod(field.Name + "_Init", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
            var il = new GroboIL(method);
            il.Ldc_I4(readers.Length); // stack: [readers.Length]
            il.Newarr(typeof(IntPtr)); // stack: [new IntPtr[readers.Length]]
            il.Stfld(field); // readersField = new IntPtr[readers.Length]
            il.Ldfld(field); // stack: [readersField]
            for(int i = 0; i < readers.Length; ++i)
            {
                if(readers[i] == null) continue;
                il.Dup(); // stack: [readersField, readersField]
                il.Ldc_I4(i); // stack: [readersField, readersField, i]
                il.Ldftn(readers[i]); // stack: [readersField, readersField, i, readers[i]]
                il.Stelem(typeof(IntPtr)); // readersField[i] = readers[i]; stack: [readersField]
            }
            il.Pop();
            il.Ret();
            return () => typeBuilder.GetMethod(method.Name).Invoke(null, null);
        }

        private static MethodInfo[] GetReaders(ReaderTypeBuilderContext context)
        {
            var result = new MethodInfo[256];
            foreach(var type in new[]
                {
                    typeof(bool), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
                    typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(string), typeof(Guid), typeof(DateTime), typeof(Array)
                })
                result[(int)GroBufTypeCodeMap.GetTypeCode(type)] = GetReader(context, type);
            return result;
        }

        private static MethodInfo GetReader(ReaderTypeBuilderContext context, Type type)
        {
            var method = context.TypeBuilder.DefineMethod("Read_" + type.Name + "_AndCastToObject_" + Guid.NewGuid(), MethodAttributes.Public | MethodAttributes.Static, typeof(void),
                                                          new[]
                                                              {
                                                                  typeof(byte*), typeof(int).MakeByRefType(), typeof(int), typeof(object).MakeByRefType()
                                                              });
            var il = new GroboIL(method);
            il.Ldarg(3); // stack: [ref result]
            il.Ldarg(0); // stack: [ref result, data]
            il.Ldarg(1); // stack: [ref result, data, ref index]
            il.Ldarg(2); // stack: [ref result, data, ref index, dataLength]
            var value = il.DeclareLocal(type);
            il.Ldloca(value); // stack: [ref result, data, ref index, dataLength, ref value]
            il.Call(context.GetReader(type)); // read<type>(data, ref index, dataLength, ref value); stack: [ref result]
            il.Ldloc(value); // stack: [ref result, value]
            if(!type.IsClass)
                il.Box(type); // stack: [ref result, (object)value]
            il.Stind(typeof(object)); // result = (object)value
            il.Ret();
            return method;
        }
    }
}