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
        private readonly IDataMembersExtracter dataMembersExtracter;

        public GroBufReader(IDataMembersExtracter dataMembersExtracter)
        {
            this.dataMembersExtracter = dataMembersExtracter;
            assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
            module = assembly.DefineDynamicModule(Guid.NewGuid().ToString());
        }

        // TODO: decimal
        public T Read<T>(byte[] data)
        {
            int index = 0;
            var result = Read<T>(data, ref index);
            if(index < data.Length)
                throw new DataCorruptedException("Encountered extra data");
            return result;
        }

        private delegate T PinningReaderDelegate<out T>(byte[] data, ref int index);

        private T Read<T>(byte[] data, ref int index)
        {
            return GetPinningReader<T>()(data, ref index);
        }

        private PinningReaderDelegate<T> GetPinningReader<T>()
        {
            var type = typeof(T);
            var pinningReader = (PinningReaderDelegate<T>)pinnedReaders[type];
            if(pinningReader == null)
            {
                lock(pinningReadersLock)
                {
                    pinningReader = (PinningReaderDelegate<T>)pinnedReaders[type];
                    if(pinningReader == null)
                    {
                        pinningReader = BuildPinningReader<T>();
                        pinnedReaders[type] = pinningReader;
                    }
                }
            }
            return pinningReader;
        }

        private PinningReaderDelegate<T> BuildPinningReader<T>()
        {
            var type = typeof(T);
            var readMethod = new ReaderTypeBuilder(module, readerCollection, dataMembersExtracter).BuildReader<T>();
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), type, new[] {typeof(byte[]), typeof(int).MakeByRefType()}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            var pinnedData = il.DeclareLocal(typeof(byte).MakeByRefType(), true);
            il.Emit(OpCodes.Ldarg_0); // stack: [data]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [data, 0]
            il.Emit(OpCodes.Ldelema, typeof(byte)); // stack: [&data[0]]
            il.Emit(OpCodes.Stloc, pinnedData); // pinnedData = &data[0]; stack: []
            il.Emit(OpCodes.Ldloc, pinnedData); // stack: [pinnedData]
            il.Emit(OpCodes.Ldarg_1); // stack: [pinnedData, ref index]
            il.Emit(OpCodes.Ldarg_0); // stack: [pinnedData, ref index, data]
            il.Emit(OpCodes.Ldlen); // stack: [pinnedData, ref index, data.Length]
            il.Emit(OpCodes.Call, readMethod); // reader(pinnedData, ref index, data.Length); stack: [result]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [result, 0]
            il.Emit(OpCodes.Conv_U); // stack: [result, (U)0]
            il.Emit(OpCodes.Stloc, pinnedData); // pinnedData = null; stack: [result]
            il.Emit(OpCodes.Ret);

            return (PinningReaderDelegate<T>)dynamicMethod.CreateDelegate(typeof(PinningReaderDelegate<T>));
        }

        private readonly IReaderCollection readerCollection = new ReaderCollection();

        private readonly Hashtable pinnedReaders = new Hashtable();
        private readonly object pinningReadersLock = new object();
        private readonly AssemblyBuilder assembly;
        private readonly ModuleBuilder module;
    }
}