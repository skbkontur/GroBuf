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
        public GroBufReader(IDataMembersExtractor dataMembersExtractor)
        {
            this.dataMembersExtractor = dataMembersExtractor;
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
            if (data == null)
                throw new ArgumentNullException("data");
            if (data.Length == 0)
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
            if (data == null)
                throw new ArgumentNullException("data");
            if (data.Length == 0)
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

        private MethodInfo GetReadMethod(Type type)
        {
            var readMethod = (MethodInfo)readMethods[type];
            if(readMethod == null)
            {
                lock(readMethodsLock)
                {
                    readMethod = (MethodInfo)readMethods[type];
                    if(readMethod == null)
                    {
                        readMethod = new ReaderTypeBuilder(this, module, readerCollection, dataMembersExtractor).BuildReader(type);
                        readMethods[type] = readMethod;
                    }
                }
            }
            return readMethod;
        }

        private ReaderDelegate<T> BuildReader<T>()
        {
            var type = typeof(T);
            MethodInfo readMethod = GetReadMethod(type);
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(IntPtr), typeof(int).MakeByRefType(), typeof(int), type.MakeByRefType()}, GetType(), true);
            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // stack: [data]
            il.Emit(OpCodes.Ldarg_1); // stack: [data, ref index]
            il.Emit(OpCodes.Ldarg_2); // stack: [data, ref index, length]
            il.Emit(OpCodes.Ldarg_3); // stack: [data, ref index, length, ref result]

            if(type.IsClass && type != typeof(string))
            {
                il.Emit(OpCodes.Dup); // stack: [data, ref index, length, ref result, ref result]
                il.Emit(OpCodes.Ldind_Ref); // stack: [data, ref index, length, ref result, result]
                var notNullLabel = il.DefineLabel();
                il.Emit(OpCodes.Brtrue, notNullLabel); // if(result != null) goto notNull; stack: [data, ref index, length, ref result]
                il.Emit(OpCodes.Dup); // stack: [data, ref index, length, ref result, ref result]
                /*if(type == typeof(string))
                    il.Emit(OpCodes.Ldstr, "");
                else*/ if(type.IsArray)
                {
                    il.Emit(OpCodes.Ldc_I4_0); // stack: [data, ref index, length, ref result, ref result, 0]
                    il.Emit(OpCodes.Newarr, type.GetElementType()); // stack: [data, ref index, length, ref result, ref result, new elementType[0]]
                }
                else
                {
                    ObjectConstructionHelper.EmitConstructionOfType(type, il);// stack: [data, ref index, length, ref result, ref result, new type()]
                    //var constructor = type.GetConstructor(Type.EmptyTypes);
                    //if(constructor == null)
                    //    throw new MissingConstructorException(type);
                    //il.Emit(OpCodes.Newobj, constructor); // stack: [data, ref index, length, ref result, ref result, new type()]
                }
                il.Emit(OpCodes.Stind_Ref); // result = new type(); stack: [data, ref index, length, ref result]
                il.MarkLabel(notNullLabel);
            }

            il.Emit(OpCodes.Call, readMethod); // reader(data, ref index, length, ref result); stack: []
            il.Emit(OpCodes.Ret);

            return (ReaderDelegate<T>)dynamicMethod.CreateDelegate(typeof(ReaderDelegate<T>));
        }

        private ReaderDelegate BuildReader(Type type)
        {
            var readMethod = GetReadMethod(type);
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(IntPtr), typeof(int).MakeByRefType(), typeof(int), typeof(object).MakeByRefType()}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // stack: [data]
            il.Emit(OpCodes.Ldarg_1); // stack: [data, ref index]
            il.Emit(OpCodes.Ldarg_2); // stack: [data, ref index, length]
            il.Emit(OpCodes.Ldarg_3); // stack: [data, ref index, length, ref result]

            LocalBuilder local = il.DeclareLocal(type);
            if (!type.IsValueType)
            {
                if(type != typeof(string))
                {
                    il.Emit(OpCodes.Dup); // stack: [data, ref index, length, ref result, ref result]
                    il.Emit(OpCodes.Ldind_Ref); // stack: [data, ref index, length, ref result, result]
                    var notNullLabel = il.DefineLabel();
                    il.Emit(OpCodes.Brtrue, notNullLabel); // if(result != null) goto notNull; stack: [data, ref index, length, ref result]
                    il.Emit(OpCodes.Dup); // stack: [data, ref index, length, ref result, ref result]
                    /*if(type == typeof(string))
                        il.Emit(OpCodes.Ldstr, "");
                    else*/
                    if(type.IsArray)
                    {
                        il.Emit(OpCodes.Ldc_I4_0); // stack: [data, ref index, length, ref result, ref result, 0]
                        il.Emit(OpCodes.Newarr, type.GetElementType()); // stack: [data, ref index, length, ref result, ref result, new elementType[0]]
                    }
                    else
                    {
                        ObjectConstructionHelper.EmitConstructionOfType(type, il); // stack: [data, ref index, length, ref result, ref result, new type()]
                        //var constructor = type.GetConstructor(Type.EmptyTypes);
                        //if(constructor == null)
                        //    throw new MissingConstructorException(type);
                        //il.Emit(OpCodes.Newobj, constructor); // stack: [data, ref index, length, ref result, ref result, new type()]
                    }
                    il.Emit(OpCodes.Stind_Ref); // result = new type(); stack: [data, ref index, length, ref result]
                    il.MarkLabel(notNullLabel);
                }
            }
            else
            {
                il.Emit(OpCodes.Ldind_Ref); // stack: [data, ref index, length, result]
                var nullLabel = il.DefineLabel();
                il.Emit(OpCodes.Dup); // stack: [data, ref index, length, result, result]
                il.Emit(OpCodes.Brfalse, nullLabel); // if(result == null) goto null; stack: [data, ref index, length, result]
                il.Emit(OpCodes.Unbox, type); // stack: [data, ref result, length, ref (type)result]
                il.Emit(OpCodes.Ldobj, type); // stack: [data, ref result, length, (type)result]
                il.Emit(OpCodes.Stloc, local); // local = (type)result, stack: [data, ref result, length]
                var notNullLabel = il.DefineLabel();
                il.Emit(OpCodes.Br, notNullLabel);
                il.MarkLabel(nullLabel);
                il.Emit(OpCodes.Pop); // stack: [data, ref index, length]
                il.Emit(OpCodes.Ldloca, local); // stack: [data, ref index, length, ref local]
                il.Emit(OpCodes.Initobj, type); // local = default(type); stack: [data, ref index, length]
                il.MarkLabel(notNullLabel);
                il.Emit(OpCodes.Ldloca, local); // stack: [data, ref index, length, ref local]
            }

            il.Emit(OpCodes.Call, readMethod); // reader(data, ref index, length, ref result); stack: []

            if(type.IsValueType)
            {
                il.Emit(OpCodes.Ldarg_3); // stack: [ref result]
                il.Emit(OpCodes.Ldloc, local); // stack: [ref result, local]
                il.Emit(OpCodes.Box, type); // stack: [ref result, (object)local]
                il.Emit(OpCodes.Stind_Ref); // result = (object)local
            }

            il.Emit(OpCodes.Ret);

            return (ReaderDelegate)dynamicMethod.CreateDelegate(typeof(ReaderDelegate));
        }

        private readonly IDataMembersExtractor dataMembersExtractor;

        private readonly IReaderCollection readerCollection = new ReaderCollection();

        private readonly Hashtable readers = new Hashtable();
        private readonly Hashtable readers2 = new Hashtable();
        private readonly Hashtable readMethods = new Hashtable();
        private readonly object readersLock = new object();
        private readonly object readMethodsLock = new object();
        private readonly AssemblyBuilder assembly;
        private readonly ModuleBuilder module;
    }
}