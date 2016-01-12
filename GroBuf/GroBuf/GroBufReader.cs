using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;

using GroBuf.DataMembersExtracters;
using GroBuf.Readers;

namespace GroBuf
{
    internal class GroBufReader
    {
        public GroBufReader(long serializerId, IDataMembersExtractor dataMembersExtractor, IGroBufCustomSerializerCollection customSerializerCollection, GroBufOptions options, Func<Type, IGroBufCustomSerializer> factory, Func<Type, IGroBufCustomSerializer> baseFactory)
        {
            this.serializerId = serializerId;
            this.dataMembersExtractor = dataMembersExtractor;
            this.customSerializerCollection = customSerializerCollection;
            this.options = options;
            this.factory = factory;
            this.baseFactory = baseFactory;
            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
            module = assembly.DefineDynamicModule(Guid.NewGuid().ToString());
            readerCollection = new ReaderCollection(customSerializerCollection, factory, baseFactory, module);
        }

        public void Read<T>(IntPtr data, ref T result, int length)
        {
            if (data == IntPtr.Zero)
                throw new ArgumentNullException("data");
            int index = 0;
            Read(data, ref index, length, ref result);
        }

        public unsafe void Read<T>(byte[] data, ref T result)
        {
            if(data == null)
                throw new ArgumentNullException("data");
            if(data.Length == 0)
                throw new ArgumentException("Cannot read data from empty array");
            fixed(byte* d = &data[0])
            {
                int index = 0;
                Read((IntPtr)d, ref index, data.Length, ref result);
                if(index < data.Length)
                    throw new DataCorruptedException("Encountered extra data");
            }
        }

        public T Read<T>(byte[] data, ref int index)
        {
            if(data == null)
                throw new ArgumentNullException("data");
            return Read<T>(data, ref index, data.Length);
        }

        public unsafe T Read<T>(byte[] data, ref int index, int length)
        {
            if(data == null)
                throw new ArgumentNullException("data");
            if(data.Length == 0)
                throw new ArgumentException("Cannot read data from empty array");
            if(length > data.Length)
                throw new ArgumentOutOfRangeException("length");
            T result = default(T);
            fixed(byte* d = &data[0])
                Read((IntPtr)d, ref index, length, ref result);
            return result;
        }

        public T Read<T>(byte[] data)
        {
            T result = default(T);
            Read(data, ref result);
            return result;
        }

        public T Read<T>(byte[] data, int length)
        {
            int index = 0;
            return Read<T>(data, ref index, length);
        }

        public unsafe void Read<T>(IntPtr data, ref int index, int length, ref T result)
        {
            int references = 0;
            if(index >= length)
                throw new DataCorruptedException("Unexpected end of data");
            var start = (byte*)(data + index);
            if(*start == (byte)GroBufTypeCode.Reference)
            {
                if(index + 5 >= length)
                    throw new DataCorruptedException("Unexpected end of data");
                references = *(int*)(start + 1);
                index += 5;
            }
            GetReader<T>(false)(data, ref index, ref result, new ReaderContext(serializerId, length, index, references));
        }

        public T Read<T>(IntPtr data, ref int index, int length)
        {
            T result = default(T);
            Read(data, ref index, length, ref result);
            return result;
        }

        public unsafe void Read(Type type, byte[] data, ref object result)
        {
            if(data == null)
                throw new ArgumentNullException("data");
            if(data.Length == 0)
                throw new ArgumentException("Cannot read data from empty array");
            fixed(byte* d = &data[0])
            {
                int index = 0;
                Read(type, (IntPtr)d, ref index, data.Length, ref result);
                if(index < data.Length)
                    throw new DataCorruptedException("Encountered extra data");
            }
        }

        public object Read(Type type, byte[] data, ref int index)
        {
            if(data == null)
                throw new ArgumentNullException("data");
            return Read(type, data, ref index, data.Length);
        }

        public unsafe object Read(Type type, byte[] data, ref int index, int length)
        {
            if(data == null)
                throw new ArgumentNullException("data");
            if(data.Length == 0)
                throw new ArgumentException("Cannot read data from empty array");
            if(length > data.Length)
                throw new ArgumentOutOfRangeException("length");
            object result = null;
            fixed(byte* d = &data[0])
                Read(type, (IntPtr)d, ref index, length, ref result);
            return result;
        }

        public object Read(Type type, IntPtr data, ref int index, int length)
        {
            if (data == IntPtr.Zero)
                throw new ArgumentNullException("data");
            object result = null;
            Read(type, data, ref index, length, ref result);
            return result;
        }

        public object Read(Type type, byte[] data)
        {
            object result = null;
            Read(type, data, ref result);
            return result;
        }

        public object Read(Type type, byte[] data, int length)
        {
            int index = 0;
            return Read(type, data, ref index, length);
        }

        public unsafe void Read(Type type, IntPtr data, ref int index, int length, ref object result)
        {
            int references = 0;
            if (index >= length)
                throw new DataCorruptedException("Unexpected end of data");
            var start = (byte*)(data + index);
            if (*start == (byte)GroBufTypeCode.Reference)
            {
                if (index + 5 >= length)
                    throw new DataCorruptedException("Unexpected end of data");
                references = *(int*)(start + 1);
                index += 5;
            }
            GetReader(type, false)(data, ref index, ref result, new ReaderContext(serializerId, length, index, references));
        }

        public void Read(Type type, bool ignoreCustomSerialization, IntPtr data, ref int index, ref object result, ReaderContext context)
        {
            GetReader(type, ignoreCustomSerialization)(data, ref index, ref result, context);
        }

        public GroBufOptions Options { get { return options; } }

        private ReaderDelegate<T> GetReader<T>(bool ignoreCustomSerialization)
        {
            var hashtable = ignoreCustomSerialization ? readers4 : readers;
            var type = typeof(T);
            var reader = (ReaderDelegate<T>)hashtable[type];
            if(reader == null)
            {
                lock(readersLock)
                {
                    reader = (ReaderDelegate<T>)hashtable[type];
                    if(reader == null)
                    {
                        reader = BuildReader<T>(ignoreCustomSerialization);
                        hashtable[type] = reader;
                    }
                }
            }
            return reader;
        }

        private ReaderDelegate GetReader(Type type, bool ignoreCustomSerialization)
        {
            var hashtable = ignoreCustomSerialization ? readers3 : readers2;
            var reader = (ReaderDelegate)hashtable[type];
            if(reader == null)
            {
                lock(readersLock)
                {
                    reader = (ReaderDelegate)hashtable[type];
                    if(reader == null)
                    {
                        reader = BuildReader(type, ignoreCustomSerialization);
                        hashtable[type] = reader;
                    }
                }
            }
            return reader;
        }

        private IntPtr GetReadMethod(Type type, bool ignoreCustomSerialization)
        {
            var hashtable = ignoreCustomSerialization ? readMethodsWithoutCustomSerialization : readMethodsWithCustomSerialization;
            var readMethod = (IntPtr?)hashtable[type];
            if(readMethod == null)
            {
                lock(readMethodsLock)
                {
                    readMethod = (IntPtr?)hashtable[type];
                    if(readMethod == null)
                    {
                        readMethod = new ReaderTypeBuilder(this, module, readerCollection, dataMembersExtractor).BuildReader(type, ignoreCustomSerialization);
                        hashtable[type] = readMethod;
                    }
                }
            }
            return readMethod.Value;
        }

        private ReaderDelegate<T> BuildReader<T>(bool ignoreCustomSerialization)
        {
            var type = typeof(T);
            var reader = GetReadMethod(type, ignoreCustomSerialization);
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(IntPtr), typeof(int).MakeByRefType(), type.MakeByRefType(), typeof(ReaderContext)}, GetType(), true);
            using (var il = new GroboIL(dynamicMethod))
            {
                il.Ldarg(0); // stack: [data]
                il.Ldarg(1); // stack: [data, ref index]
                il.Ldarg(2); // stack: [data, ref index, ref result]

                if(!type.IsValueType && type != typeof(string) && (ignoreCustomSerialization || customSerializerCollection.Get(type, factory, baseFactory(type)) == null))
                {
                    il.Dup(); // stack: [data, ref index, ref result, ref result]
                    il.Ldind(type); // stack: [data, ref index, ref result, result]
                    var notNullLabel = il.DefineLabel("notNull");
                    il.Brtrue(notNullLabel); // if(result != null) goto notNull; stack: [data, ref index, ref result]
                    il.Dup(); // stack: [data, ref index, ref result, ref result]
                    if(!type.IsArray)
                        ObjectConstructionHelper.EmitConstructionOfType(type, il); // stack: [data, ref index, ref result, ref result, new type()]
                    else
                    {
                        il.Ldc_I4(0); // stack: [data, ref index, ref result, ref result, 0]
                        il.Newarr(type.GetElementType()); // stack: [data, ref index, ref result, ref result, new elementType[0]]
                    }
                    il.Stind(type); // result = new type(); stack: [data, ref index, ref result]
                    il.MarkLabel(notNullLabel);
                }

                il.Ldarg(3); // stack: [data, ref index, ref result, context]
                il.Ldc_IntPtr(reader);
                il.Calli(CallingConventions.Standard, typeof(void), new[] {typeof(IntPtr), typeof(int).MakeByRefType(), type.MakeByRefType(), typeof(ReaderContext)}); // reader(data, ref index, ref result, context); stack: []
                il.Ret();
            }

            return (ReaderDelegate<T>)dynamicMethod.CreateDelegate(typeof(ReaderDelegate<T>));
        }

        private ReaderDelegate BuildReader(Type type, bool ignoreCustomSerialization)
        {
            var reader = GetReadMethod(type, ignoreCustomSerialization);
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(IntPtr), typeof(int).MakeByRefType(), typeof(object).MakeByRefType(), typeof(ReaderContext)}, GetType(), true);
            using (var il = new GroboIL(dynamicMethod))
            {
                il.Ldarg(0); // stack: [data]
                il.Ldarg(1); // stack: [data, ref index]
                il.Ldarg(2); // stack: [data, ref index, ref result]

                var local = il.DeclareLocal(type);
                if(!type.IsValueType)
                {
                    if(type != typeof(string) && (ignoreCustomSerialization || customSerializerCollection.Get(type, factory, baseFactory(type)) == null))
                    {
                        il.Dup(); // stack: [data, ref index, ref result, ref result]
                        il.Ldind(typeof(object)); // stack: [data, ref index, ref result, result]
                        var notNullLabel = il.DefineLabel("notNull");
                        il.Brtrue(notNullLabel); // if(result != null) goto notNull; stack: [data, ref index, ref result]
                        il.Dup(); // stack: [data, ref index, ref result, ref result]
                        if(!type.IsArray)
                            ObjectConstructionHelper.EmitConstructionOfType(type, il); // stack: [data, ref index, ref result, ref result, new type()]
                        else
                        {
                            il.Ldc_I4(0); // stack: [data, ref index, ref result, ref result, 0]
                            il.Newarr(type.GetElementType()); // stack: [data, ref index, ref result, ref result, new elementType[0]]
                        }
                        il.Stind(typeof(object)); // result = new type(); stack: [data, ref index, ref result]
                        il.MarkLabel(notNullLabel);
                    }
                    il.Ldind(typeof(object)); // stack: [data, ref index, result]
                    il.Castclass(type); // stack: [data, ref index, (type)result]
                    il.Stloc(local); // local = (type)result; stack: [data, ref index]
                }
                else
                {
                    il.Ldind(typeof(object)); // stack: [data, ref index, result]
                    var nullLabel = il.DefineLabel("null");
                    il.Dup(); // stack: [data, ref index, length, result]
                    il.Brfalse(nullLabel); // if(result == null) goto null; stack: [data, ref index, result]
                    il.Unbox_Any(type); // stack: [data, ref result, (type)result]
                    il.Stloc(local); // local = (type)result, stack: [data, ref result]
                    var notNullLabel = il.DefineLabel("notNull");
                    il.Br(notNullLabel);
                    il.MarkLabel(nullLabel);
                    il.Pop(); // stack: [data, ref index]
                    il.Ldloca(local); // stack: [data, ref index, ref local]
                    il.Initobj(type); // local = default(type); stack: [data, ref index]
                    il.MarkLabel(notNullLabel);
                }

                il.Ldloca(local); // stack: [data, ref index, ref local]
                il.Ldarg(3); // stack: [data, ref index, ref result, context]
                il.Ldc_IntPtr(reader);
                il.Calli(CallingConventions.Standard, typeof(void), new[] {typeof(IntPtr), typeof(int).MakeByRefType(), type.MakeByRefType(), typeof(ReaderContext)}); // reader(data, ref index, ref result, context); stack: []

                il.Ldarg(2); // stack: [ref result]
                il.Ldloc(local); // stack: [ref result, local]
                if(type.IsValueType)
                    il.Box(type); // stack: [ref result, (object)local]
                il.Stind(typeof(object)); // result = (object)local

                il.Ret();
            }

            return (ReaderDelegate)dynamicMethod.CreateDelegate(typeof(ReaderDelegate));
        }

        private readonly long serializerId;
        private readonly IDataMembersExtractor dataMembersExtractor;
        private readonly IGroBufCustomSerializerCollection customSerializerCollection;
        private readonly GroBufOptions options;
        private readonly Func<Type, IGroBufCustomSerializer> factory;
        private readonly Func<Type, IGroBufCustomSerializer> baseFactory;

        private readonly Hashtable readers = new Hashtable();
        private readonly Hashtable readers2 = new Hashtable();
        private readonly Hashtable readers3 = new Hashtable();
        private readonly Hashtable readers4 = new Hashtable();
        internal readonly Hashtable readMethodsWithCustomSerialization = new Hashtable();
        private readonly Hashtable readMethodsWithoutCustomSerialization = new Hashtable();
        private readonly object readersLock = new object();
        private readonly object readMethodsLock = new object();
        private readonly ModuleBuilder module;
        private readonly IReaderCollection readerCollection;
    }
}