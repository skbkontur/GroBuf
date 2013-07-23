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
        public GroBufWriter(IDataMembersExtractor dataMembersExtractor, IGroBufCustomSerializerCollection customSerializerCollection, GroBufOptions options, Func<Type, IGroBufCustomSerializer> factory, Func<Type, IGroBufCustomSerializer> baseFactory)
        {
            this.dataMembersExtractor = dataMembersExtractor;
            this.options = options;
            sizeCounterCollection = new SizeCounterCollection(customSerializerCollection, factory, baseFactory);
            writerCollection = new WriterCollection(customSerializerCollection, factory, baseFactory);
            assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
            module = assembly.DefineDynamicModule(Guid.NewGuid().ToString());
        }

        public int GetSize<T>(T obj)
        {
            return GetSize(obj, true);
        }

        public void Write<T>(T obj, IntPtr result)
        {
            Write(false, obj, result);
        }

        public void Write<T>(bool ignoreCustomSerialization, T obj, IntPtr result)
        {
            int index = 0;
            GetWriterAndSizeCounter<T>(ignoreCustomSerialization).Item1(obj, true, result, ref index);
        }

        public void Write<T>(T obj, byte[] result, ref int index)
        {
            Write(false, obj, result, ref index);
        }

        public void Write<T>(T obj, IntPtr result, ref int index)
        {
            GetWriterAndSizeCounter<T>(false).Item1(obj, true, result, ref index);
        }

        public unsafe void Write<T>(bool ignoreCustomSerialization, T obj, byte[] result, ref int index)
        {
            fixed(byte* r = &result[0])
                GetWriterAndSizeCounter<T>(ignoreCustomSerialization).Item1(obj, true, (IntPtr)r, ref index);
        }

        public byte[] Write<T>(T obj)
        {
            return Write(false, obj);
        }

        public unsafe byte[] Write<T>(bool ignoreCustomSerialization, T obj)
        {
            var writerAndCounter = GetWriterAndSizeCounter<T>(ignoreCustomSerialization);
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
            Write(type, false, obj, result);
        }

        public void Write(Type type, bool ignoreCustomSerialization, object obj, IntPtr result)
        {
            int index = 0;
            GetWriterAndSizeCounter(type, ignoreCustomSerialization).Item1(obj, true, result, ref index);
        }

        public void Write(Type type, object obj, IntPtr result, ref int index)
        {
            GetWriterAndSizeCounter(type, false).Item1(obj, true, result, ref index);
        }

        public void Write(Type type, object obj, byte[] result, ref int index)
        {
            Write(type, false, obj, result, ref index);
        }

        public unsafe void Write(Type type, bool ignoreCustomSerialization, object obj, byte[] result, ref int index)
        {
            fixed(byte* r = &result[0])
                GetWriterAndSizeCounter(type, ignoreCustomSerialization).Item1(obj, true, (IntPtr)r, ref index);
        }

        public void Write(Type type, object obj, bool writeEmpty, IntPtr result, ref int index)
        {
            Write(type, false, obj, writeEmpty, result, ref index);
        }

        public void Write(Type type, bool ignoreCustomSerialization, object obj, bool writeEmpty, IntPtr result, ref int index)
        {
            GetWriterAndSizeCounter(type, ignoreCustomSerialization).Item1(obj, writeEmpty, result, ref index);
        }

        public byte[] Write(Type type, object obj)
        {
            return Write(type, false, obj);
        }

        public unsafe byte[] Write(Type type, bool ignoreCustomSerialization, object obj)
        {
            var writerAndCounter = GetWriterAndSizeCounter(type, ignoreCustomSerialization);
            var size = writerAndCounter.Item2(obj, true);
            var result = new byte[size];
            int index = 0;
            fixed(byte* r = &result[0])
                writerAndCounter.Item1(obj, true, (IntPtr)r, ref index);
            return result;
        }

        public int GetSize(Type type, object obj, bool writeEmpty)
        {
            return GetSize(type, false, obj, writeEmpty);
        }

        public int GetSize(Type type, bool ignoreCustomSerialization, object obj, bool writeEmpty)
        {
            return GetWriterAndSizeCounter(type, ignoreCustomSerialization).Item2(obj, writeEmpty);
        }

        public GroBufOptions Options { get { return options; } }

        private int GetSize<T>(T obj, bool writeEmpty)
        {
            return GetSize(false, obj, writeEmpty);
        }

        private int GetSize<T>(bool ignoreCustomSerialization, T obj, bool writeEmpty)
        {
            return GetWriterAndSizeCounter<T>(ignoreCustomSerialization).Item2(obj, writeEmpty);
        }

        private Tuple<WriterDelegate, SizeCounterDelegate> GetWriterAndSizeCounter(Type type, bool ignoreCustomSerialization)
        {
            var hashtable = ignoreCustomSerialization ? writersAndSizeCounters3 : writersAndSizeCounters2;
            var writerAndSizeCounter = (Tuple<WriterDelegate, SizeCounterDelegate>)hashtable[type];
            if(writerAndSizeCounter == null)
            {
                lock(writersAndSizeCountersLock)
                {
                    writerAndSizeCounter = (Tuple<WriterDelegate, SizeCounterDelegate>)hashtable[type];
                    if(writerAndSizeCounter == null)
                    {
                        writerAndSizeCounter = new Tuple<WriterDelegate, SizeCounterDelegate>(BuildWriter(type, ignoreCustomSerialization), BuildCounter(type, ignoreCustomSerialization));
                        hashtable[type] = writerAndSizeCounter;
                    }
                }
            }
            return writerAndSizeCounter;
        }

        private Tuple<WriterDelegate<T>, SizeCounterDelegate<T>> GetWriterAndSizeCounter<T>(bool ignoreCustomSerialization)
        {
            var hashtable = ignoreCustomSerialization ? writersAndSizeCounters4 : writersAndSizeCounters;
            var type = typeof(T);
            var writerAndSizeCounter = (Tuple<WriterDelegate<T>, SizeCounterDelegate<T>>)hashtable[type];
            if(writerAndSizeCounter == null)
            {
                lock(writersAndSizeCountersLock)
                {
                    writerAndSizeCounter = (Tuple<WriterDelegate<T>, SizeCounterDelegate<T>>)hashtable[type];
                    if(writerAndSizeCounter == null)
                    {
                        writerAndSizeCounter = new Tuple<WriterDelegate<T>, SizeCounterDelegate<T>>(BuildWriter<T>(ignoreCustomSerialization), BuildCounter<T>(ignoreCustomSerialization));
                        hashtable[type] = writerAndSizeCounter;
                    }
                }
            }
            return writerAndSizeCounter;
        }

        private IntPtr GetWriter(Type type, bool ignoreCustomSerialization)
        {
            var hashtable = ignoreCustomSerialization ? writers2 : writers;
            var writer = (IntPtr?)hashtable[type];
            if(writer == null)
            {
                lock(writersLock)
                {
                    writer = (IntPtr?)hashtable[type];
                    if(writer == null)
                    {
                        writer = new WriterTypeBuilder(this, module, writerCollection, dataMembersExtractor).BuildWriter(type, ignoreCustomSerialization);
                        hashtable[type] = writer;
                    }
                }
            }
            return writer.Value;
        }

        private WriterDelegate<T> BuildWriter<T>(bool ignoreCustomSerialization)
        {
            var type = typeof(T);
            IntPtr writer = GetWriter(type, ignoreCustomSerialization);

            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {type, typeof(bool), typeof(IntPtr), typeof(int).MakeByRefType()}, module, true);
            var il = new GroboIL(dynamicMethod);
            il.Ldarg(0); // stack: [obj]
            il.Ldarg(1); // stack: [obj, writeEmpty]
            il.Ldarg(2); // stack: [obj, writeEmpty, result]
            il.Ldarg(3); // stack: [obj, writeEmpty, result, ref index]
            il.Ldc_IntPtr(writer);
            il.Calli(CallingConventions.Standard, typeof(void), new[] {type, typeof(bool), typeof(IntPtr), typeof(int).MakeByRefType()}); // writer.write<T>(obj, writeEmpty, result, ref index); stack: []
            il.Ret();

            return (WriterDelegate<T>)dynamicMethod.CreateDelegate(typeof(WriterDelegate<T>));
        }

        private WriterDelegate BuildWriter(Type type, bool ignoreCustomSerialization)
        {
            IntPtr writer = GetWriter(type, ignoreCustomSerialization);

            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(object), typeof(bool), typeof(IntPtr), typeof(int).MakeByRefType()}, module, true);
            var il = new GroboIL(dynamicMethod);
            il.Ldarg(0); // stack: [obj]
            if(type.IsValueType)
                il.Unbox_Any(type); // stack: [(type)obj]
//            else
//                il.Castclass(type); // stack: [(type)obj]
            il.Ldarg(1); // stack: [(type)obj, writeEmpty]
            il.Ldarg(2); // stack: [(type)obj, writeEmpty, result]
            il.Ldarg(3); // stack: [(type)obj, writeEmpty, result, ref index]
            il.Ldc_IntPtr(writer);
            il.Calli(CallingConventions.Standard, typeof(void), new[] {type, typeof(bool), typeof(IntPtr), typeof(int).MakeByRefType()}); // writer.write<T>((type)obj, writeEmpty, result, ref index); stack: []
            il.Ret();

            return (WriterDelegate)dynamicMethod.CreateDelegate(typeof(WriterDelegate));
        }

        private IntPtr GetCounter(Type type, bool ignoreCustomSerialization)
        {
            var hashtable = ignoreCustomSerialization ? counters2 : counters;
            var counter = (IntPtr?)hashtable[type];
            if(counter == null)
            {
                lock(countersLock)
                {
                    counter = (IntPtr?)hashtable[type];
                    if(counter == null)
                    {
                        counter = new SizeCounterTypeBuilder(this, module, sizeCounterCollection, dataMembersExtractor).BuildSizeCounter(type, ignoreCustomSerialization);
                        hashtable[type] = counter;
                    }
                }
            }
            return counter.Value;
        }

        private SizeCounterDelegate<T> BuildCounter<T>(bool ignoreCustomSerialization)
        {
            var type = typeof(T);
            IntPtr counter = GetCounter(type, ignoreCustomSerialization);

            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] {type, typeof(bool)}, GetType(), true);
            var il = new GroboIL(dynamicMethod);
            il.Ldarg(0); // stack: [obj]
            il.Ldarg(1); // stack: [obj, writeEmpty]
            il.Ldc_IntPtr(counter); // stack: [obj, writeEmpty, counter]
            il.Calli(CallingConventions.Standard, typeof(int), new[] {type, typeof(bool)}); // counter(obj, writeEmpty); stack: []
            il.Ret();

            return (SizeCounterDelegate<T>)dynamicMethod.CreateDelegate(typeof(SizeCounterDelegate<T>));
        }

        private SizeCounterDelegate BuildCounter(Type type, bool ignoreCustomSerialization)
        {
            IntPtr counter = GetCounter(type, ignoreCustomSerialization);

            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] {typeof(object), typeof(bool)}, GetType(), true);
            var il = new GroboIL(dynamicMethod);
            il.Ldarg(0); // stack: [obj]
            if(type.IsValueType)
                il.Unbox_Any(type); // stack: [(type)obj]
            //            else
            //                il.Castclass(type); // stack: [(type)obj]
            il.Ldarg(1); // stack: [(type)obj, writeEmpty]
            il.Ldc_IntPtr(counter); // stack: [(type)obj, writeEmpty, counter]
            il.Calli(CallingConventions.Standard, typeof(int), new[] {type, typeof(bool)}); // counter((type)obj, writeEmpty); stack: []
            il.Ret();

            return (SizeCounterDelegate)dynamicMethod.CreateDelegate(typeof(SizeCounterDelegate));
        }

        private readonly IDataMembersExtractor dataMembersExtractor;
        private readonly GroBufOptions options;

        private readonly Hashtable writersAndSizeCounters = new Hashtable();
        private readonly Hashtable writersAndSizeCounters2 = new Hashtable();
        private readonly Hashtable writersAndSizeCounters3 = new Hashtable();
        private readonly Hashtable writersAndSizeCounters4 = new Hashtable();
        private readonly Hashtable writers = new Hashtable();
        private readonly Hashtable writers2 = new Hashtable();
        private readonly Hashtable counters = new Hashtable();
        private readonly Hashtable counters2 = new Hashtable();
        private readonly object writersAndSizeCountersLock = new object();
        private readonly object writersLock = new object();
        private readonly object countersLock = new object();
        private readonly IWriterCollection writerCollection;
        private readonly ISizeCounterCollection sizeCounterCollection;
        private readonly AssemblyBuilder assembly;
        private readonly ModuleBuilder module;
    }
}