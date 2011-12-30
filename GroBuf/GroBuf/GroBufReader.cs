using System;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using SKBKontur.GroBuf.Readers;

namespace SKBKontur.GroBuf
{
    public class GroBufReader
    {
        // TODO: enum, derived types, decimal
        public T Read<T>(byte[] data)
        {
            int index = 0;
            var result = Read<T>(data, ref index);
            if(index < data.Length)
                throw new DataCorruptedException("Encountered extra data");
            return result;
        }

        private delegate T PinningReaderDelegate<out T>(byte[] data, ref int index);

        private delegate T InternalPinningReaderDelegate<out T>(Delegate readerDelegate, byte[] data, ref int index);

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
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), type, new[] {typeof(Delegate), typeof(byte[]), typeof(int).MakeByRefType()}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            var pinnedData = il.DeclareLocal(typeof(byte).MakeByRefType(), true);
            il.Emit(OpCodes.Ldarg_1); // stack: [data]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [data, 0]
            il.Emit(OpCodes.Ldelema, typeof(byte)); // stack: [&data[0]]
            il.Emit(OpCodes.Stloc, pinnedData); // pinnedData = &data[0]; stack: []
            var reader = GetReader(type);
            il.Emit(OpCodes.Ldarg_0); // stack: [readerDelegate]
            il.Emit(OpCodes.Ldloc, pinnedData); // stack: [readerDelegate, pinnedData]
            il.Emit(OpCodes.Ldarg_2); // stack: [readerDelegate, pinnedData, ref index]
            il.Emit(OpCodes.Ldarg_1); // stack: [readerDelegate, pinnedData, ref index, data]
            il.Emit(OpCodes.Ldlen); // stack: [readerDelegate, pinnedData, ref index, data.Length]
            il.Emit(OpCodes.Call, reader.GetType().GetMethod("Invoke")); // reader.Read<T>(pinnedData, ref index, data.Length); stack: [result]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [result, 0]
            il.Emit(OpCodes.Conv_U); // stack: [result, (U)0]
            il.Emit(OpCodes.Stloc, pinnedData); // pinnedData = null; stack: [result]
            il.Emit(OpCodes.Ret);

            var pinningReader = (InternalPinningReaderDelegate<T>)dynamicMethod.CreateDelegate(typeof(InternalPinningReaderDelegate<T>));
            return (byte[] data, ref int index) => pinningReader(reader, data, ref index);
        }

        private unsafe Delegate GetReader(Type type)
        {
            if(getReaderMethod == null)
                getReaderMethod = ((MethodCallExpression)((Expression<Action<IReaderCollection>>)(collection => collection.GetReader<int>())).Body).Method.GetGenericMethodDefinition();
            return ((Delegate)getReaderMethod.MakeGenericMethod(new[] {type}).Invoke(readerCollection, new object[0]));
        }

        private readonly IReaderCollection readerCollection = new ReaderCollection();

        private static MethodInfo getReaderMethod;

        private readonly Hashtable pinnedReaders = new Hashtable();
        private readonly object pinningReadersLock = new object();
    }
}