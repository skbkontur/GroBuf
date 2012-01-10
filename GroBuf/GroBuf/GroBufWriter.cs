using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

using SKBKontur.GroBuf.DataMembersExtracters;
using SKBKontur.GroBuf.Writers;

namespace SKBKontur.GroBuf
{
    internal class GroBufWriter
    {
        private readonly IDataMembersExtracter dataMembersExtracter;

        public GroBufWriter(IDataMembersExtracter dataMembersExtracter)
        {
            this.dataMembersExtracter = dataMembersExtracter;
            assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
            module = assembly.DefineDynamicModule(Guid.NewGuid().ToString());
        }

        byte[] buf = new byte[/*4096*/1024 * 128];

        // TODO: decimal
        public byte[] Write<T>(T obj)
        {
            //var buf = new byte[/*4096*/1024 * 128];
            int index = 0;
            Write(obj, true, ref buf, ref index);
            /*Array.Resize(ref buf, index);
            return buf;*/
            var result = new byte[index];
            // TODO
            Array.Copy(buf, result, index);
            return result;
        }

        private delegate void PinningWriterDelegate<in T>(T obj, bool writeEmpty, ref byte[] result, ref int index);

        private void Write<T>(T obj, bool writeEmpty, ref byte[] result, ref int index)
        {
            GetPinningWriter<T>()(obj, writeEmpty, ref result, ref index);
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

        private PinningWriterDelegate<T> BuildPinningWriter<T>()
        {
            var type = typeof(T);
            var writeMethod = new TypeWriterBuilder(module, writerCollection, dataMembersExtracter).BuildTypeWriter<T>();

            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {type, typeof(bool), typeof(byte[]).MakeByRefType(), typeof(int).MakeByRefType()}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            var pinnedResult = il.DeclareLocal(typeof(byte).MakeByRefType(), true);
            il.Emit(OpCodes.Ldarg_2); // stack: [ref result]
            il.Emit(OpCodes.Ldind_Ref); // stack: [result]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [result, 0]
            il.Emit(OpCodes.Ldelema, typeof(byte)); // stack: [&result[0]]
            il.Emit(OpCodes.Stloc, pinnedResult); // pinnedResult = &result[0]; stack: []
            il.Emit(OpCodes.Ldarg_0); // stack: [obj]
            il.Emit(OpCodes.Ldarg_1); // stack: [obj, writeEmpty]
            il.Emit(OpCodes.Ldarg_2); // stack: [obj, writeEmpty, ref result]
            il.Emit(OpCodes.Ldarg_3); // stack: [obj, writeEmpty, ref result, ref index]
            il.Emit(OpCodes.Ldloca, pinnedResult); // stack: [obj, writeEmpty, ref result, ref index, ref pinnedResult]
            il.Emit(OpCodes.Call, writeMethod); // writer.write<T>(obj, writeEmpty, ref result, ref index, ref pinnedResult); stack: []
            il.Emit(OpCodes.Ldc_I4_0); // stack: [0]
            il.Emit(OpCodes.Conv_U); // stack: [(uint)0]
            il.Emit(OpCodes.Stloc, pinnedResult); // pinnedResult = null
            il.Emit(OpCodes.Ret);

            return (PinningWriterDelegate<T>)dynamicMethod.CreateDelegate(typeof(PinningWriterDelegate<T>));
        }

        private readonly Hashtable pinningWriters = new Hashtable();
        private readonly object pinningWritersLock = new object();
        private readonly IWriterCollection writerCollection = new WriterCollection();
        private readonly AssemblyBuilder assembly;
        private readonly ModuleBuilder module;
    }
}