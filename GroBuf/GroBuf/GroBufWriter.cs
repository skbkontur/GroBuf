using System;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using SKBKontur.GroBuf.Writers;

namespace SKBKontur.GroBuf
{
    public class GroBufWriter
    {
        // TODO: enum, derived types, decimal
        public byte[] Write<T>(T obj)
        {
            var buf = new byte[4096];
            int index = 0;
            Write(obj, true, ref buf, ref index);
            var result = new byte[index];
            // TODO
            Array.Copy(buf, result, index);
            return result;
        }

        private delegate void PinningWriterDelegate<in T>(T obj, bool writeEmpty, ref byte[] result, ref int index);

        private delegate void InternalPinningWriterDelegate<in T>(Delegate writerDelegate, T obj, bool writeEmpty, ref byte[] result, ref int index);

        private void Write<T>(T obj, bool writeEmpty, ref byte[] buf, ref int index)
        {
            GetPinningWriter<T>()(obj, writeEmpty, ref buf, ref index);
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
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void),
                                                  new[] {typeof(Delegate), type, typeof(bool), typeof(byte[]).MakeByRefType(), typeof(int).MakeByRefType()}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            var pinnedResult = il.DeclareLocal(typeof(byte).MakeByRefType(), true);
            il.Emit(OpCodes.Ldarg_3); // stack: [ref result]
            il.Emit(OpCodes.Ldind_Ref); // stack: [result]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [result, 0]
            il.Emit(OpCodes.Ldelema, typeof(byte)); // stack: [&result[0]]
            il.Emit(OpCodes.Stloc, pinnedResult); // pinnedResult = &result[0]; stack: []
            var writerDelegate = GetWriter(type);
            il.Emit(OpCodes.Ldarg_0); // stack: [writerDelegate]
            il.Emit(OpCodes.Ldarg_1); // stack: [writerDelegate, obj]
            il.Emit(OpCodes.Ldarg_2); // stack: [writerDelegate, obj, writeEmpty]
            il.Emit(OpCodes.Ldarg_3); // stack: [writerDelegate, obj, writeEmpty, ref result]
            il.Emit(OpCodes.Ldarg_S, 4); // stack: [writerDelegate, obj, writeEmpty, ref result, ref index]
            il.Emit(OpCodes.Ldloca, pinnedResult); // stack: [writerDelegate, obj, writeEmpty, ref result, ref index, ref pinnedResult]
            il.Emit(OpCodes.Call, writerDelegate.GetType().GetMethod("Invoke")); // writer.write<T>(obj, writeEmpty, ref result, ref index, ref pinnedResult); stack: []
            il.Emit(OpCodes.Ldc_I4_0); // stack: [0]
            il.Emit(OpCodes.Conv_U); // stack: [(uint)0]
            il.Emit(OpCodes.Stloc, pinnedResult); // pinnedResult = null
            il.Emit(OpCodes.Ret);

            var pinningWriter = (InternalPinningWriterDelegate<T>)dynamicMethod.CreateDelegate(typeof(InternalPinningWriterDelegate<T>));
            return (T obj, bool writeEmpty, ref byte[] result, ref int index) => pinningWriter(writerDelegate, obj, writeEmpty, ref result, ref index);
        }

        private unsafe Delegate GetWriter(Type type)
        {
            if(getWriterMethod == null)
                getWriterMethod = ((MethodCallExpression)((Expression<Action<IWriterCollection>>)(collection => collection.GetWriter<int>())).Body).Method.GetGenericMethodDefinition();
            return ((Delegate)getWriterMethod.MakeGenericMethod(new[] {type}).Invoke(writerCollection, new object[0]));
        }

        private readonly IWriterCollection writerCollection = new WriterCollection();

        private static MethodInfo getWriterMethod;

        private readonly Hashtable pinningWriters = new Hashtable();
        private readonly object pinningWritersLock = new object();
    }
}