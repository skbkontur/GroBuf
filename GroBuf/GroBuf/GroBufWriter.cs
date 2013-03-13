using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;

using GroBuf.DataMembersExtracters;
using GroBuf.SizeCounters;
using GroBuf.Writers;

namespace GroBuf
{
    internal class GroBufWriter
    {
        public GroBufWriter(IDataMembersExtractor dataMembersExtractor)
        {
            this.dataMembersExtractor = dataMembersExtractor;
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
            GetWriterAndSizeCounter<T>().Item1(obj, true, result, ref index);
        }

        public unsafe void Write<T>(T obj, byte[] result, ref int index)
        {
            fixed(byte* r = &result[0])
                GetWriterAndSizeCounter<T>().Item1(obj, true, (IntPtr)r, ref index);
        }

        // TODO: decimal
        public unsafe byte[] Write<T>(T obj)
        {
            var writerAndCounter = GetWriterAndSizeCounter<T>();
            var size = writerAndCounter.Item2(obj, true);
            var result = new byte[size];
            int index = 0;
            fixed(byte* r = &result[0])
                writerAndCounter.Item1(obj, true, (IntPtr)r, ref index);
            return result;
        }

        public int GetSize(Type type, object obj)
        {
            return GetSize(type, obj, true);
        }

        public void Write(Type type, object obj, IntPtr result)
        {
            int index = 0;
            GetWriterAndSizeCounter(type).Item1(obj, true, result, ref index);
        }

        public unsafe void Write(Type type, object obj, byte[] result, ref int index)
        {
            fixed(byte* r = &result[0])
                GetWriterAndSizeCounter(type).Item1(obj, true, (IntPtr)r, ref index);
        }

        public void Write(Type type, object obj, bool writeEmpty, IntPtr result, ref int index)
        {
            GetWriterAndSizeCounter(type).Item1(obj, writeEmpty, result, ref index);
        }

        // TODO: decimal
        public unsafe byte[] Write(Type type, object obj)
        {
            var writerAndCounter = GetWriterAndSizeCounter(type);
            var size = writerAndCounter.Item2(obj, true);
            var result = new byte[size];
            int index = 0;
            fixed(byte* r = &result[0])
                writerAndCounter.Item1(obj, true, (IntPtr)r, ref index);
            return result;
        }

        public int GetSize(Type type, object obj, bool writeEmpty)
        {
            return GetWriterAndSizeCounter(type).Item2(obj, writeEmpty);
        }

        private int GetSize<T>(T obj, bool writeEmpty)
        {
            return GetWriterAndSizeCounter<T>().Item2(obj, writeEmpty);
        }

        private Tuple<WriterDelegate, SizeCounterDelegate> GetWriterAndSizeCounter(Type type)
        {
            var writerAndSizeCounter = (Tuple<WriterDelegate, SizeCounterDelegate>)writersAndSizeCounters2[type];
            if(writerAndSizeCounter == null)
            {
                lock(writersAndSizeCountersLock)
                {
                    writerAndSizeCounter = (Tuple<WriterDelegate, SizeCounterDelegate>)writersAndSizeCounters2[type];
                    if(writerAndSizeCounter == null)
                    {
                        writerAndSizeCounter = new Tuple<WriterDelegate, SizeCounterDelegate>(BuildWriter(type), BuildCounter(type));
                        writersAndSizeCounters2[type] = writerAndSizeCounter;
                    }
                }
            }
            return writerAndSizeCounter;
        }

        private Tuple<WriterDelegate<T>, SizeCounterDelegate<T>> GetWriterAndSizeCounter<T>()
        {
            var type = typeof(T);
            var writerAndSizeCounter = (Tuple<WriterDelegate<T>, SizeCounterDelegate<T>>)writersAndSizeCounters[type];
            if(writerAndSizeCounter == null)
            {
                lock(writersAndSizeCountersLock)
                {
                    writerAndSizeCounter = (Tuple<WriterDelegate<T>, SizeCounterDelegate<T>>)writersAndSizeCounters[type];
                    if(writerAndSizeCounter == null)
                    {
                        writerAndSizeCounter = new Tuple<WriterDelegate<T>, SizeCounterDelegate<T>>(BuildWriter<T>(), BuildCounter<T>());
                        writersAndSizeCounters[type] = writerAndSizeCounter;
                    }
                }
            }
            return writerAndSizeCounter;
        }

        private MethodInfo GetWriter(Type type)
        {
            var writer = (MethodInfo)writers[type];
            if(writer == null)
            {
                lock(writersLock)
                {
                    writer = (MethodInfo)writers[type];
                    if(writer == null)
                    {
                        writer = new WriterTypeBuilder(this, module, writerCollection, dataMembersExtractor).BuildWriter(type);
                        writers[type] = writer;
                    }
                }
            }
            return writer;
        }

        private WriterDelegate<T> BuildWriter<T>()
        {
            var type = typeof(T);
            MethodInfo writeMethod = GetWriter(type);

            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {type, typeof(bool), typeof(IntPtr), typeof(int).MakeByRefType()}, GetType(), true);
            var il = new GroboIL(dynamicMethod);
            il.Ldarg(0); // stack: [obj]
            il.Ldarg(1); // stack: [obj, writeEmpty]
            il.Ldarg(2); // stack: [obj, writeEmpty, result]
            il.Ldarg(3); // stack: [obj, writeEmpty, result, ref index]
            il.Call(writeMethod); // writer.write<T>(obj, writeEmpty, result, ref index); stack: []
            il.Ret();

            return (WriterDelegate<T>)dynamicMethod.CreateDelegate(typeof(WriterDelegate<T>));
        }

        private WriterDelegate BuildWriter(Type type)
        {
            MethodInfo writeMethod = GetWriter(type);

            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(object), typeof(bool), typeof(IntPtr), typeof(int).MakeByRefType()}, GetType(), true);
            var il = new GroboIL(dynamicMethod);
            il.Ldarg(0); // stack: [obj]
            if(type.IsValueType)
                il.Unbox_Any(type); // stack: [(type)obj]
//            else
//                il.Castclass(type); // stack: [(type)obj]
            il.Ldarg(1); // stack: [(type)obj, writeEmpty]
            il.Ldarg(2); // stack: [(type)obj, writeEmpty, result]
            il.Ldarg(3); // stack: [(type)obj, writeEmpty, result, ref index]
            il.Call(writeMethod); // writer.write<T>((type)obj, writeEmpty, result, ref index); stack: []
            il.Ret();

            return (WriterDelegate)dynamicMethod.CreateDelegate(typeof(WriterDelegate));
        }

        private MethodInfo GetCounter(Type type)
        {
            var counter = (MethodInfo)counters[type];
            if(counter == null)
            {
                lock(countersLock)
                {
                    counter = (MethodInfo)counters[type];
                    if(counter == null)
                    {
                        counter = new SizeCounterTypeBuilder(this, module, sizeCounterCollection, dataMembersExtractor).BuildSizeCounter(type);
                        counters[type] = counter;
                    }
                }
            }
            return counter;
        }

        private SizeCounterDelegate<T> BuildCounter<T>()
        {
            var type = typeof(T);
            MethodInfo counter = GetCounter(type);

            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] {type, typeof(bool)}, GetType(), true);
            var il = new GroboIL(dynamicMethod);
            il.Ldarg(0); // stack: [obj]
            il.Ldarg(1); // stack: [obj, writeEmpty]
            il.Call(counter); // counter(obj, writeEmpty); stack: []
            il.Ret();

            return (SizeCounterDelegate<T>)dynamicMethod.CreateDelegate(typeof(SizeCounterDelegate<T>));
        }

        private SizeCounterDelegate BuildCounter(Type type)
        {
            MethodInfo counter = GetCounter(type);

            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] {typeof(object), typeof(bool)}, GetType(), true);
            var il = new GroboIL(dynamicMethod);
            il.Ldarg(0); // stack: [obj]
            if(type.IsValueType)
                il.Unbox_Any(type); // stack: [(type)obj]
            //            else
            //                il.Castclass(type); // stack: [(type)obj]
            il.Ldarg(1); // stack: [(type)obj, writeEmpty]
            il.Call(counter); // counter((type)obj, writeEmpty); stack: []
            il.Ret();

            return (SizeCounterDelegate)dynamicMethod.CreateDelegate(typeof(SizeCounterDelegate));
        }

        private readonly IDataMembersExtractor dataMembersExtractor;

        private readonly Hashtable writersAndSizeCounters = new Hashtable();
        private readonly Hashtable writersAndSizeCounters2 = new Hashtable();
        private readonly Hashtable writers = new Hashtable();
        private readonly Hashtable counters = new Hashtable();
        private readonly object writersAndSizeCountersLock = new object();
        private readonly object writersLock = new object();
        private readonly object countersLock = new object();
        private readonly IWriterCollection writerCollection = new WriterCollection();
        private readonly ISizeCounterCollection sizeCounterCollection = new SizeCounterCollection();
        private readonly AssemblyBuilder assembly;
        private readonly ModuleBuilder module;
    }
}