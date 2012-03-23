using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

using GroBuf.DataMembersExtracters;
using GroBuf.Readers;

namespace GroBuf
{
    // TODO: decimal
    internal class GroBufReader
    {
        public GroBufReader(IDataMembersExtracter dataMembersExtracter)
        {
            this.dataMembersExtracter = dataMembersExtracter;
            assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
            module = assembly.DefineDynamicModule(Guid.NewGuid().ToString());
        }

        public void Read<T>(IntPtr data, int length, ref T result)
        {
            int index = 0;
            Read(data, ref index, length, ref result);
            if(index < length)
                throw new DataCorruptedException("Encountered extra data");
        }

        public T Read<T>(IntPtr data, int length)
        {
            T result = default(T);
            Read(data, length, ref result);
            return result;
        }

        public unsafe void Read<T>(byte[] data, ref T result)
        {
            fixed(byte* d = &data[0])
                Read((IntPtr)d, data.Length, ref result);
        }

        public unsafe T Read<T>(byte[] data)
        {
            fixed(byte* d = &data[0])
                return Read<T>((IntPtr)d, data.Length);
        }

        private delegate void ReaderDelegate<T>(IntPtr data, ref int index, int length, ref T result);

        private void Read<T>(IntPtr data, ref int index, int length, ref T result)
        {
            GetReader<T>()(data, ref index, length, ref result);
        }

        private ReaderDelegate<T> GetReader<T>()
        {
            var type = typeof(T);
            var reader = (ReaderDelegate<T>)readers[type];
            if(reader == null)
            {
                lock(readersLock)
                {
                    reader = (ReaderDelegate<T>)readers[type];
                    if(reader == null)
                    {
                        reader = BuildReader<T>();
                        readers[type] = reader;
                    }
                }
            }
            return reader;
        }

        private ReaderDelegate<T> BuildReader<T>()
        {
            var type = typeof(T);
            var readMethod = new ReaderTypeBuilder(module, readerCollection, dataMembersExtracter).BuildReader<T>();
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(IntPtr), typeof(int).MakeByRefType(), typeof(int), type.MakeByRefType()}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // stack: [data]
            il.Emit(OpCodes.Ldarg_1); // stack: [data, ref index]
            il.Emit(OpCodes.Ldarg_2); // stack: [data, ref index, length]
            il.Emit(OpCodes.Ldarg_3); // stack: [data, ref index, length, ref result]

            if(type.IsClass)
            {
                il.Emit(OpCodes.Dup); // stack: [data, ref index, length, ref result, ref result]
                il.Emit(OpCodes.Ldind_Ref); // stack: [data, ref index, length, ref result, result]
                var notNullLabel = il.DefineLabel();
                il.Emit(OpCodes.Brtrue, notNullLabel); // if(result != null) goto notNull; stack: [data, ref index, length, ref result]
                il.Emit(OpCodes.Dup); // stack: [data, ref index, length, ref result, ref result]
                if(type == typeof(string))
                    il.Emit(OpCodes.Ldstr, "");
                else if(type.IsArray)
                {
                    il.Emit(OpCodes.Ldc_I4_0); // stack: [data, ref index, length, ref result, ref result, 0]
                    il.Emit(OpCodes.Newarr, type.GetElementType()); // stack: [data, ref index, length, ref result, ref result, new elementType[0]]
                }
                else
                {
                    var constructor = type.GetConstructor(Type.EmptyTypes);
                    if(constructor == null)
                        throw new MissingConstructorException(type);
                    il.Emit(OpCodes.Newobj, constructor); // stack: [data, ref index, length, ref result, ref result, new type()]
                }
                il.Emit(OpCodes.Stind_Ref); // result = new type(); stack: [data, ref index, length, ref result]
                il.MarkLabel(notNullLabel);
            }

            il.Emit(OpCodes.Call, readMethod); // reader(data, ref index, length, ref result); stack: []
            il.Emit(OpCodes.Ret);

            return (ReaderDelegate<T>)dynamicMethod.CreateDelegate(typeof(ReaderDelegate<T>));
        }

        private readonly IDataMembersExtracter dataMembersExtracter;

        private readonly IReaderCollection readerCollection = new ReaderCollection();

        private readonly Hashtable readers = new Hashtable();
        private readonly object readersLock = new object();
        private readonly AssemblyBuilder assembly;
        private readonly ModuleBuilder module;
    }
}