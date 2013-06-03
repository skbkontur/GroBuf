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
        public GroBufReader(IDataMembersExtractor dataMembersExtractor, IGroBufCustomSerializerCollection customSerializerCollection, Func<Type, IGroBufCustomSerializer> func)
        {
            this.dataMembersExtractor = dataMembersExtractor;
            this.customSerializerCollection = customSerializerCollection;
            this.func = func;
            readerCollection = new ReaderCollection(customSerializerCollection, func);
            assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
            module = assembly.DefineDynamicModule(Guid.NewGuid().ToString());
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

        public unsafe T Read<T>(byte[] data, ref int index)
        {
            if(data == null)
                throw new ArgumentNullException("data");
            if(data.Length == 0)
                throw new ArgumentException("Cannot read data from empty array");
            T result = default(T);
            fixed(byte* d = &data[0])
                Read((IntPtr)d, ref index, data.Length, ref result);
            return result;
        }

        public T Read<T>(byte[] data)
        {
            T result = default(T);
            Read(data, ref result);
            return result;
        }

        public void Read<T>(IntPtr data, ref int index, int length, ref T result)
        {
            GetReader<T>()(data, ref index, length, ref result);
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

        public unsafe object Read(Type type, byte[] data, ref int index)
        {
            if(data == null)
                throw new ArgumentNullException("data");
            if(data.Length == 0)
                throw new ArgumentException("Cannot read data from empty array");
            object result = null;
            fixed(byte* d = &data[0])
                Read(type, (IntPtr)d, ref index, data.Length, ref result);
            return result;
        }

        public object Read(Type type, byte[] data)
        {
            object result = null;
            Read(type, data, ref result);
            return result;
        }

        public void Read(Type type, IntPtr data, ref int index, int length, ref object result)
        {
            GetReader(type)(data, ref index, length, ref result);
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

        private ReaderDelegate GetReader(Type type)
        {
            var reader = (ReaderDelegate)readers2[type];
            if(reader == null)
            {
                lock(readersLock)
                {
                    reader = (ReaderDelegate)readers2[type];
                    if(reader == null)
                    {
                        reader = BuildReader(type);
                        readers2[type] = reader;
                    }
                }
            }
            return reader;
        }

        private IntPtr GetReadMethod(Type type)
        {
            var readMethod = (IntPtr?)readMethods[type];
            if(readMethod == null)
            {
                lock(readMethodsLock)
                {
                    readMethod = (IntPtr?)readMethods[type];
                    if(readMethod == null)
                    {
                        readMethod = new ReaderTypeBuilder(this, module, readerCollection, dataMembersExtractor).BuildReader(type);
                        readMethods[type] = readMethod;
                    }
                }
            }
            return readMethod.Value;
        }

        private ReaderDelegate<T> BuildReader<T>()
        {
            var type = typeof(T);
            var reader = GetReadMethod(type);
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(IntPtr), typeof(int).MakeByRefType(), typeof(int), type.MakeByRefType()}, GetType(), true);
            var il = new GroboIL(dynamicMethod);
            il.Ldarg(0); // stack: [data]
            il.Ldarg(1); // stack: [data, ref index]
            il.Ldarg(2); // stack: [data, ref index, length]
            il.Ldarg(3); // stack: [data, ref index, length, ref result]

            if(!type.IsValueType && type != typeof(string) && customSerializerCollection.Get(type, func) == null)
            {
                il.Dup(); // stack: [data, ref index, length, ref result, ref result]
                il.Ldind(typeof(object)); // stack: [data, ref index, length, ref result, result]
                var notNullLabel = il.DefineLabel("notNull");
                il.Brtrue(notNullLabel); // if(result != null) goto notNull; stack: [data, ref index, length, ref result]
                il.Dup(); // stack: [data, ref index, length, ref result, ref result]
                if(type.IsArray)
                {
                    il.Ldc_I4(0); // stack: [data, ref index, length, ref result, ref result, 0]
                    il.Newarr(type.GetElementType()); // stack: [data, ref index, length, ref result, ref result, new elementType[0]]
                }
                else
                    ObjectConstructionHelper.EmitConstructionOfType(type, il); // stack: [data, ref index, length, ref result, ref result, new type()]
                il.Stind(typeof(object)); // result = new type(); stack: [data, ref index, length, ref result]
                il.MarkLabel(notNullLabel);
            }

            il.Ldc_IntPtr(reader);
            il.Calli(CallingConventions.Standard, typeof(void), new[] {typeof(IntPtr), typeof(int).MakeByRefType(), typeof(int), type.MakeByRefType()}); // reader(data, ref index, length, ref result); stack: []
            il.Ret();

            return (ReaderDelegate<T>)dynamicMethod.CreateDelegate(typeof(ReaderDelegate<T>));
        }

        private ReaderDelegate BuildReader(Type type)
        {
            var reader = GetReadMethod(type);
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(IntPtr), typeof(int).MakeByRefType(), typeof(int), typeof(object).MakeByRefType()}, GetType(), true);
            var il = new GroboIL(dynamicMethod);
            il.Ldarg(0); // stack: [data]
            il.Ldarg(1); // stack: [data, ref index]
            il.Ldarg(2); // stack: [data, ref index, length]
            il.Ldarg(3); // stack: [data, ref index, length, ref result]

            var local = il.DeclareLocal(type);
            if(!type.IsValueType)
            {
                if(type != typeof(string) && customSerializerCollection.Get(type, func) == null)
                {
                    il.Dup(); // stack: [data, ref index, length, ref result, ref result]
                    il.Ldind(typeof(object)); // stack: [data, ref index, length, ref result, result]
                    var notNullLabel = il.DefineLabel("notNull");
                    il.Brtrue(notNullLabel); // if(result != null) goto notNull; stack: [data, ref index, length, ref result]
                    il.Dup(); // stack: [data, ref index, length, ref result, ref result]
                    if(type.IsArray)
                    {
                        il.Ldc_I4(0); // stack: [data, ref index, length, ref result, ref result, 0]
                        il.Newarr(type.GetElementType()); // stack: [data, ref index, length, ref result, ref result, new elementType[0]]
                    }
                    else
                        ObjectConstructionHelper.EmitConstructionOfType(type, il); // stack: [data, ref index, length, ref result, ref result, new type()]
                    il.Stind(typeof(object)); // result = new type(); stack: [data, ref index, length, ref result]
                    il.MarkLabel(notNullLabel);
                }
            }
            else
            {
                il.Ldind(typeof(object)); // stack: [data, ref index, length, result]
                var nullLabel = il.DefineLabel("null");
                il.Dup(); // stack: [data, ref index, length, result, result]
                il.Brfalse(nullLabel); // if(result == null) goto null; stack: [data, ref index, length, result]
                il.Unbox_Any(type); // stack: [data, ref result, length, (type)result]
                il.Stloc(local); // local = (type)result, stack: [data, ref result, length]
                var notNullLabel = il.DefineLabel("notNull");
                il.Br(notNullLabel);
                il.MarkLabel(nullLabel);
                il.Pop(); // stack: [data, ref index, length]
                il.Ldloca(local); // stack: [data, ref index, length, ref local]
                il.Initobj(type); // local = default(type); stack: [data, ref index, length]
                il.MarkLabel(notNullLabel);
                il.Ldloca(local); // stack: [data, ref index, length, ref local]
            }

            il.Ldc_IntPtr(reader);
            il.Calli(CallingConventions.Standard, typeof(void), new[] {typeof(IntPtr), typeof(int).MakeByRefType(), typeof(int), type.MakeByRefType()}); // reader(data, ref index, length, ref result); stack: []

            if(type.IsValueType)
            {
                il.Ldarg(3); // stack: [ref result]
                il.Ldloc(local); // stack: [ref result, local]
                il.Box(type); // stack: [ref result, (object)local]
                il.Stind(typeof(object)); // result = (object)local
            }

            il.Ret();

            return (ReaderDelegate)dynamicMethod.CreateDelegate(typeof(ReaderDelegate));
        }

        private readonly IDataMembersExtractor dataMembersExtractor;
        private readonly IGroBufCustomSerializerCollection customSerializerCollection;
        private readonly Func<Type, IGroBufCustomSerializer> func;

        private readonly IReaderCollection readerCollection;

        private readonly Hashtable readers = new Hashtable();
        private readonly Hashtable readers2 = new Hashtable();
        private readonly Hashtable readMethods = new Hashtable();
        private readonly object readersLock = new object();
        private readonly object readMethodsLock = new object();
        private readonly AssemblyBuilder assembly;
        private readonly ModuleBuilder module;
    }
}