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
        public GroBufWriter(long serializerId, IDataMembersExtractor dataMembersExtractor, IGroBufCustomSerializerCollection customSerializerCollection, GroBufOptions options, Func<Type, IGroBufCustomSerializer> factory, Func<Type, IGroBufCustomSerializer> baseFactory)
        {
            this.serializerId = serializerId;
            this.dataMembersExtractor = dataMembersExtractor;
            this.options = options;
            sizeCounterCollection = new SizeCounterCollection(customSerializerCollection, factory, baseFactory);
            writerCollection = new WriterCollection(customSerializerCollection, factory, baseFactory);
            assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
            module = assembly.DefineDynamicModule(Guid.NewGuid().ToString());
        }

        public int GetSize<T>(T obj)
        {
            return GetSize(false, obj, true);
        }

        public void Write<T>(T obj, IntPtr result, int length)
        {
            int index = 0;
            Write(false, obj, result, ref index, length);
            if (index != length)
                throw new Exception("Bug: at the end of serialization index must point at the end of array");
        }

        private void Write<T>(bool ignoreCustomSerialization, T obj, IntPtr result, ref int index, int length)
        {
            var writerAndSizeCounter = GetWriterAndSizeCounter<T>(ignoreCustomSerialization);
            if(!options.HasFlag(GroBufOptions.PackReferences))
                writerAndSizeCounter.Item1(obj, true, result, ref index, new WriterContext(serializerId, length, index));
            else
            {
                var context = new WriterContext(serializerId, length, index);
                writerAndSizeCounter.Item2(obj, true, context);
                Write(obj, result, ref index, writerAndSizeCounter.Item1, context);
            }
        }

        public unsafe void Write<T>(T obj, byte[] result, ref int index)
        {
            fixed (byte* r = &result[0])
                Write(false, obj, (IntPtr)r, ref index, result.Length);
        }

        public void Write<T>(T obj, IntPtr result, ref int index, int length)
        {
            Write(false, obj, result, ref index, length);
        }

        public byte[] Write<T>(T obj)
        {
            return Write(false, obj);
        }

        public unsafe byte[] Write<T>(bool ignoreCustomSerialization, T obj)
        {
            var writerAndCounter = GetWriterAndSizeCounter<T>(ignoreCustomSerialization);
            var context = new WriterContext(serializerId, 0, 0);
            var size = writerAndCounter.Item2(obj, true, context);
            size = context.references > 0 ? size + 5 : size;
            var result = new byte[size];
            context.length = size;
            int index = 0;
            fixed(byte* r = &result[0])
                Write(obj, (IntPtr)r, ref index, writerAndCounter.Item1, context);
            if (index != size)
                throw new Exception("Bug: at the end of serialization index must point at the end of array");
            return result;
        }

        private unsafe void Write<T>(T obj, IntPtr data, ref int index, WriterDelegate<T> writer, WriterContext context)
        {
            if (context.references > 0)
            {
                var r = (byte*)(data + index);
                *r = (byte)GroBufTypeCode.Reference;
                *(int*)(r + 1) = context.references;
                index += 5;
            }
            context.start = index;
            writer(obj, true, data, ref index, new WriterContext(serializerId, context.length, context.start));
        }

        private unsafe void Write(object obj, IntPtr data, ref int index, WriterDelegate writer, WriterContext context)
        {
            if (context.references > 0)
            {
                var r = (byte*)(data + index);
                *r = (byte)GroBufTypeCode.Reference;
                *(int*)(r + 1) = context.references;
                index += 5;
            }
            context.start = index;
            writer(obj, true, data, ref index, new WriterContext(serializerId, context.length, context.start));
        }

        public int GetSize(Type type, object obj)
        {
            var context = new WriterContext(serializerId, 0, 0);
            var result = GetSize(type, false, obj, true, context);
            return context.references > 0 ? result + 5 : result;
        }

        public void Write(Type type, object obj, IntPtr result, int length)
        {
            int index = 0;
            Write(type, false, obj, result, ref index, length);
            if (index != length)
                throw new Exception("Bug: at the end of serialization index must point at the end of array");
        }

        private void Write(Type type, bool ignoreCustomSerialization, object obj, IntPtr result, ref int index, int length)
        {
            var writerAndSizeCounter = GetWriterAndSizeCounter(type, ignoreCustomSerialization);
            if (!options.HasFlag(GroBufOptions.PackReferences))
                writerAndSizeCounter.Item1(obj, true, result, ref index, new WriterContext(serializerId, length, index));
            else
            {
                var context = new WriterContext(serializerId, length, index);
                writerAndSizeCounter.Item2(obj, true, context);
                Write(obj, result, ref index, writerAndSizeCounter.Item1, context);
            }
        }

        public void Write(Type type, object obj, IntPtr result, ref int index, int length)
        {
            Write(type, false, obj, result, ref index, length);
        }

        public void Write(Type type, object obj, byte[] result, ref int index)
        {
            Write(type, false, obj, result, ref index);
        }

        public unsafe void Write(Type type, bool ignoreCustomSerialization, object obj, byte[] result, ref int index)
        {
            fixed (byte* r = &result[0])
                Write(type, ignoreCustomSerialization, obj, (IntPtr)r, ref index, result.Length);
        }

        public void Write(Type type, bool ignoreCustomSerialization, object obj, bool writeEmpty, IntPtr result, ref int index, WriterContext context)
        {
            GetWriterAndSizeCounter(type, ignoreCustomSerialization).Item1(obj, writeEmpty, result, ref index, context);
        }

        public byte[] Write(Type type, object obj)
        {
            return Write(type, false, obj);
        }

        private unsafe byte[] Write(Type type, bool ignoreCustomSerialization, object obj)
        {
            var writerAndCounter = GetWriterAndSizeCounter(type, ignoreCustomSerialization);
            var context = new WriterContext(serializerId, 0, 0);
            var size = writerAndCounter.Item2(obj, true, context);
            size = context.references > 0 ? size + 5 : size;
            var result = new byte[size];
            context.length = size;
            int index = 0;
            fixed(byte* r = &result[0])
                Write(obj, (IntPtr)r, ref index, writerAndCounter.Item1, context);
            if(index != size)
                throw new Exception("At the end of serialization index must point at the end of array");
            return result;
        }

        public int GetSize(Type type, bool ignoreCustomSerialization, object obj, bool writeEmpty, WriterContext context)
        {
            return GetWriterAndSizeCounter(type, ignoreCustomSerialization).Item2(obj, writeEmpty, context);
        }

        public GroBufOptions Options { get { return options; } }

        private int GetSize<T>(bool ignoreCustomSerialization, T obj, bool writeEmpty)
        {
            var context = new WriterContext(serializerId, 0, 0);
            var result = GetWriterAndSizeCounter<T>(ignoreCustomSerialization).Item2(obj, writeEmpty, context);
            return context.references > 0 ? result + 5 : result;
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
            var hashtable = ignoreCustomSerialization ? writersWithoutCustomSerialization : writersWithCustomSerialization;
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

            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {type, typeof(bool), typeof(IntPtr), typeof(int).MakeByRefType(), typeof(WriterContext)}, module, true);
            using (var il = new GroboIL(dynamicMethod))
            {
                il.Ldarg(0); // stack: [obj]
                il.Ldarg(1); // stack: [obj, writeEmpty]
                il.Ldarg(2); // stack: [obj, writeEmpty, result]
                il.Ldarg(3); // stack: [obj, writeEmpty, result, ref index]
                il.Ldarg(4); // stack: [obj, writeEmpty, result, ref index, context]
                il.Ldc_IntPtr(writer);
                il.Calli(CallingConventions.Standard, typeof(void), new[] {type, typeof(bool), typeof(IntPtr), typeof(int).MakeByRefType(), typeof(WriterContext)}); // writer.write<T>(obj, writeEmpty, result, ref index, context); stack: []
                il.Ret();
            }

            return (WriterDelegate<T>)dynamicMethod.CreateDelegate(typeof(WriterDelegate<T>));
        }

        private WriterDelegate BuildWriter(Type type, bool ignoreCustomSerialization)
        {
            IntPtr writer = GetWriter(type, ignoreCustomSerialization);

            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(object), typeof(bool), typeof(IntPtr), typeof(int).MakeByRefType(), typeof(WriterContext)}, module, true);
            using (var il = new GroboIL(dynamicMethod))
            {
                il.Ldarg(0); // stack: [obj]
                if(type.IsValueType)
                    il.Unbox_Any(type); // stack: [(type)obj]
                else
                    il.Castclass(type); // stack: [(type)obj]
                il.Ldarg(1); // stack: [(type)obj, writeEmpty]
                il.Ldarg(2); // stack: [(type)obj, writeEmpty, result]
                il.Ldarg(3); // stack: [(type)obj, writeEmpty, result, ref index]
                il.Ldarg(4); // stack: [(type)obj, writeEmpty, result, ref index, context]
                il.Ldc_IntPtr(writer);
                il.Calli(CallingConventions.Standard, typeof(void), new[] {type, typeof(bool), typeof(IntPtr), typeof(int).MakeByRefType(), typeof(WriterContext)}); // writer.write<T>((type)obj, writeEmpty, result, ref index, context); stack: []
                il.Ret();
            }

            return (WriterDelegate)dynamicMethod.CreateDelegate(typeof(WriterDelegate));
        }

        private IntPtr GetCounter(Type type, bool ignoreCustomSerialization)
        {
            var hashtable = ignoreCustomSerialization ? countersWithoutCustomSerialization : countersWithCustomSerialization;
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

            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] {type, typeof(bool), typeof(WriterContext)}, GetType(), true);
            using (var il = new GroboIL(dynamicMethod))
            {
                il.Ldarg(0); // stack: [obj]
                il.Ldarg(1); // stack: [obj, writeEmpty]
                il.Ldarg(2); // stack: [obj, writeEmpty, context]
                il.Ldc_IntPtr(counter); // stack: [obj, writeEmpty, context, counter]
                il.Calli(CallingConventions.Standard, typeof(int), new[] {type, typeof(bool), typeof(WriterContext)}); // counter(obj, writeEmpty, context); stack: []
                il.Ret();
            }
            return (SizeCounterDelegate<T>)dynamicMethod.CreateDelegate(typeof(SizeCounterDelegate<T>));
        }

        private SizeCounterDelegate BuildCounter(Type type, bool ignoreCustomSerialization)
        {
            IntPtr counter = GetCounter(type, ignoreCustomSerialization);

            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(int), new[] {typeof(object), typeof(bool), typeof(WriterContext)}, GetType(), true);
            using (var il = new GroboIL(dynamicMethod))
            {
                il.Ldarg(0); // stack: [obj]
                if(type.IsValueType)
                    il.Unbox_Any(type); // stack: [(type)obj]
                else
                    il.Castclass(type); // stack: [(type)obj]
                il.Ldarg(1); // stack: [(type)obj, writeEmpty]
                il.Ldarg(2); // stack: [(type)obj, writeEmpty, context]
                il.Ldc_IntPtr(counter); // stack: [(type)obj, writeEmpty, context, counter]
                il.Calli(CallingConventions.Standard, typeof(int), new[] {type, typeof(bool), typeof(WriterContext)}); // counter((type)obj, writeEmpty, context); stack: []
                il.Ret();
            }

            return (SizeCounterDelegate)dynamicMethod.CreateDelegate(typeof(SizeCounterDelegate));
        }

        private readonly long serializerId;
        private readonly IDataMembersExtractor dataMembersExtractor;
        private readonly GroBufOptions options;

        private readonly Hashtable writersAndSizeCounters = new Hashtable();
        private readonly Hashtable writersAndSizeCounters2 = new Hashtable();
        private readonly Hashtable writersAndSizeCounters3 = new Hashtable();
        private readonly Hashtable writersAndSizeCounters4 = new Hashtable();
        internal readonly Hashtable writersWithCustomSerialization = new Hashtable();
        private readonly Hashtable writersWithoutCustomSerialization = new Hashtable();
        internal readonly Hashtable countersWithCustomSerialization = new Hashtable();
        private readonly Hashtable countersWithoutCustomSerialization = new Hashtable();
        private readonly object writersAndSizeCountersLock = new object();
        private readonly object writersLock = new object();
        private readonly object countersLock = new object();
        private readonly IWriterCollection writerCollection;
        private readonly ISizeCounterCollection sizeCounterCollection;
        private readonly AssemblyBuilder assembly;
        private readonly ModuleBuilder module;
    }
}