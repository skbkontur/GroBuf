using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

using GroBuf.DataMembersExtracters;
using GroBuf.SizeCounters;
using GroBuf.Writers;

namespace GroBuf
{
    internal class GroBufWriter
    {
        public GroBufWriter(IDataMembersExtracter dataMembersExtracter)
        {
            this.dataMembersExtracter = dataMembersExtracter;
            assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
            module = assembly.DefineDynamicModule(Guid.NewGuid().ToString());
        }

        public int GetSize<T>(T obj)
        {
            return GetSize(obj, true);
        }

        public void Write<T>(T obj, IntPtr result)
        {
            // todo
        }

        public void Write<T>(T obj, byte[] result, int index)
        {
            var size = GetSize(obj);
            if(result.Length < index + size)
                throw new ArgumentOutOfRangeException("result", "Too small: required size = " + size);
            Write(obj, true, result, ref index);
        }

        // TODO: decimal
        public byte[] Write<T>(T obj)
        {
            var result = new byte[GetSize(obj)];
            int index = 0;
            Write(obj, true, result, ref index);
            return result;
        }

        private delegate int CounterDelegate<in T>(T obj, bool writeEmpty);

        private delegate void PinningWriterDelegate<in T>(T obj, bool writeEmpty, byte[] result, ref int index);

        private int GetSize<T>(T obj, bool writeEmpty)
        {
            return GetCounter<T>()(obj, writeEmpty);
        }

        private void Write<T>(T obj, bool writeEmpty, byte[] result, ref int index)
        {
            GetPinningWriter<T>()(obj, writeEmpty, result, ref index);
        }

        private PinningWriterDelegate<T> GetPinningWriter<T>()
        {
            var type = typeof(T);
            var pinningWriter = (PinningWriterDelegate<T>)pinningWriters[type];
            if(pinningWriter == null)
            {
                lock(pinningWritersLock)
                {
                    pinningWriter = (PinningWriterDelegate<T>)pinningWriters[type];
                    if(pinningWriter == null)
                    {
                        pinningWriter = BuildPinningWriter<T>();
                        pinningWriters[type] = pinningWriter;
                    }
                }
            }
            return pinningWriter;
        }

        private CounterDelegate<T> GetCounter<T>()
        {
            var type = typeof(T);
            var counter = (CounterDelegate<T>)counters[type];
            if(counter == null)
            {
                lock(countersLock)
                {
                    counter = (CounterDelegate<T>)counters[type];
                    if(counter == null)
                    {
                        counter = BuildCounter<T>();
                        counters[type] = counter;
                    }
                }
            }
            return counter;
        }

        private PinningWriterDelegate<T> BuildPinningWriter<T>()
        {
            var type = typeof(T);
            var writeMethod = new WriterTypeBuilder(module, writerCollection, dataMembersExtracter).BuildWriter<T>();

            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {type, typeof(bool), typeof(byte[]), typeof(int).MakeByRefType()}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            var pinnedResult = il.DeclareLocal(typeof(byte).MakeByRefType(), true);
            il.Emit(OpCodes.Ldarg_2); // stack: [result]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [result, 0]
            il.Emit(OpCodes.Ldelema, typeof(byte)); // stack: [&result[0]]
            il.Emit(OpCodes.Stloc, pinnedResult); // result = &result[0]; stack: []
            il.Emit(OpCodes.Ldarg_0); // stack: [obj]
            il.Emit(OpCodes.Ldarg_1); // stack: [obj, writeEmpty]
            il.Emit(OpCodes.Ldloc, pinnedResult); // stack: [obj, writeEmpty, pinnedResult]
            il.Emit(OpCodes.Ldarg_3); // stack: [obj, writeEmpty, pinnedResult, ref index]
            il.Emit(OpCodes.Call, writeMethod); // writer.write<T>(obj, writeEmpty, result, ref index); stack: []
            il.Emit(OpCodes.Ldc_I4_0); // stack: [0]
            il.Emit(OpCodes.Conv_U); // stack: [(uint)0]
            il.Emit(OpCodes.Stloc, pinnedResult); // result = null
            il.Emit(OpCodes.Ret);

            return (PinningWriterDelegate<T>)dynamicMethod.CreateDelegate(typeof(PinningWriterDelegate<T>));
        }

        private CounterDelegate<T> BuildCounter<T>()
        {
            var type = typeof(T);
            var counter = new SizeCounterTypeBuilder(module, sizeCounterCollection, dataMembersExtracter).BuildSizeCounter<T>();

            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] {type, typeof(bool)}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // stack: [obj]
            il.Emit(OpCodes.Ldarg_1); // stack: [obj, writeEmpty]
            il.Emit(OpCodes.Call, counter); // counter(obj, writeEmpty); stack: []
            il.Emit(OpCodes.Ret);

            return (CounterDelegate<T>)dynamicMethod.CreateDelegate(typeof(CounterDelegate<T>));
        }

        private readonly IDataMembersExtracter dataMembersExtracter;

        private readonly Hashtable pinningWriters = new Hashtable();
        private readonly object pinningWritersLock = new object();
        private readonly Hashtable counters = new Hashtable();
        private readonly object countersLock = new object();
        private readonly IWriterCollection writerCollection = new WriterCollection();
        private readonly ISizeCounterCollection sizeCounterCollection = new SizeCounterCollection();
        private readonly AssemblyBuilder assembly;
        private readonly ModuleBuilder module;
    }
}