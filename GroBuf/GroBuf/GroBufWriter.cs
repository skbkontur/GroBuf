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
            int index = 0;
            GetWriterAndCounter<T>().Item1(obj, true, result, ref index);
        }

        public unsafe void Write<T>(T obj, byte[] result, ref int index)
        {
            fixed(byte* r = &result[index])
                GetWriterAndCounter<T>().Item1(obj, true, (IntPtr)r, ref index);
        }

        // TODO: decimal
        public unsafe byte[] Write<T>(T obj)
        {
            var writerAndCounter = GetWriterAndCounter<T>();
            var size = writerAndCounter.Item2(obj, true);
            var result = new byte[size];
            int index = 0;
            fixed(byte* r = &result[index])
                writerAndCounter.Item1(obj, true, (IntPtr)r, ref index);
            return result;
        }

        private delegate int CounterDelegate<in T>(T obj, bool writeEmpty);

        private delegate void WriterDelegate<in T>(T obj, bool writeEmpty, IntPtr result, ref int index);

        private int GetSize<T>(T obj, bool writeEmpty)
        {
            return GetWriterAndCounter<T>().Item2(obj, writeEmpty);
        }

        private Tuple<WriterDelegate<T>, CounterDelegate<T>> GetWriterAndCounter<T>()
        {
            var type = typeof(T);
            var writerAndCounter = (Tuple<WriterDelegate<T>, CounterDelegate<T>>)writers[type];
            if(writerAndCounter == null)
            {
                lock(writersLock)
                {
                    writerAndCounter = (Tuple<WriterDelegate<T>, CounterDelegate<T>>)writers[type];
                    if(writerAndCounter == null)
                    {
                        writerAndCounter = new Tuple<WriterDelegate<T>, CounterDelegate<T>>(BuildWriter<T>(), BuildCounter<T>());
                        writers[type] = writerAndCounter;
                    }
                }
            }
            return writerAndCounter;
        }

        private WriterDelegate<T> BuildWriter<T>()
        {
            var type = typeof(T);
            var writeMethod = new WriterTypeBuilder(module, writerCollection, dataMembersExtracter).BuildWriter<T>();

            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {type, typeof(bool), typeof(IntPtr), typeof(int).MakeByRefType()}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // stack: [obj]
            il.Emit(OpCodes.Ldarg_1); // stack: [obj, writeEmpty]
            il.Emit(OpCodes.Ldarg_2); // stack: [obj, writeEmpty, result]
            il.Emit(OpCodes.Ldarg_3); // stack: [obj, writeEmpty, result, ref index]
            il.Emit(OpCodes.Call, writeMethod); // writer.write<T>(obj, writeEmpty, result, ref index); stack: []
            il.Emit(OpCodes.Ret);

            return (WriterDelegate<T>)dynamicMethod.CreateDelegate(typeof(WriterDelegate<T>));
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

        private readonly Hashtable writers = new Hashtable();
        private readonly object writersLock = new object();
        private readonly IWriterCollection writerCollection = new WriterCollection();
        private readonly ISizeCounterCollection sizeCounterCollection = new SizeCounterCollection();
        private readonly AssemblyBuilder assembly;
        private readonly ModuleBuilder module;
    }
}