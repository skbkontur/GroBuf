using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

using GroBuf.DataMembersExtracters;
using GroBuf.Readers;

namespace GroBuf
{
    internal class GroBufReader
    {
        public GroBufReader(IDataMembersExtracter dataMembersExtracter)
        {
            this.dataMembersExtracter = dataMembersExtracter;
            assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
            module = assembly.DefineDynamicModule(Guid.NewGuid().ToString());
        }

        public T Read<T>(IntPtr data, int length)
        {
            int index = 0;
            var result = Read<T>(data, ref index, length);
            if(index < length)
                throw new DataCorruptedException("Encountered extra data");
            return result;
        }

        // TODO: decimal
        public unsafe T Read<T>(byte[] data)
        {
            fixed(byte* d = &data[0])
                return Read<T>((IntPtr)d, data.Length);
        }

        private delegate T ReaderDelegate<out T>(IntPtr data, ref int index, int length);

        private T Read<T>(IntPtr data, ref int index, int length)
        {
            return GetReader<T>()(data, ref index, length);
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
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), type, new[] {typeof(IntPtr), typeof(int).MakeByRefType(), typeof(int)}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // stack: [data]
            il.Emit(OpCodes.Ldarg_1); // stack: [data, ref index]
            il.Emit(OpCodes.Ldarg_2); // stack: [data, ref index, length]
            il.Emit(OpCodes.Call, readMethod); // reader(data, ref index, length); stack: [result]
            var retLabel = il.DefineLabel();
            if(type.IsClass)
            {
                il.Emit(OpCodes.Dup); // stack: [result, result]
                il.Emit(OpCodes.Brtrue, retLabel); // if(result != null) goto ret; stack: [result]
                il.Emit(OpCodes.Pop); // stack: []
                var constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor == null)
                    throw new MissingConstructorException(type);
                il.Emit(OpCodes.Newobj, constructor); // stack: [new type()]
            }
            il.MarkLabel(retLabel);
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