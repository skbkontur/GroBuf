using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;

namespace GroBuf.Readers
{
    internal class ObjectReaderBuilder : ReaderBuilderBase
    {
        public ObjectReaderBuilder()
            : base(typeof(object))
        {
        }

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
            context.SetFields(Type, new[]
                {
                    new KeyValuePair<string, Type>("readers_" + Type.Name + "_" + Guid.NewGuid(), typeof(IntPtr[])),
                    new KeyValuePair<string, Type>("delegates_" + Type.Name + "_" + Guid.NewGuid(), typeof(Delegate[]))
                });
            Array.ForEach(primitiveTypes, context.BuildConstants);
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            var il = context.Il;

            var readers = GetReaders(context.Context);
            var readersField = context.Context.InitConstField(Type, 0, readers.Select(pair => pair.Value).ToArray());
            context.Context.InitConstField(Type, 1, readers.Select(pair => pair.Key).ToArray());

            context.LoadData(); // stack: [data]
            context.LoadIndexByRef(); // stack: [data, ref index]
            context.LoadDataLength(); // stack: [data, ref index, dataLength]
            context.LoadResultByRef(); // stack: [data, ref index, dataLength, ref result]
            context.LoadField(readersField); // stack: [data, ref index, dataLength, ref result, readers]
            il.Ldloc(context.TypeCode); // stack: [data, ref index, dataLength, ref result, readers, typeCode]
            il.Ldelem(typeof(IntPtr)); // stack: [data, ref index, dataLength, ref result, readers[typeCode]]
            il.Dup(); // stack: [data, ref index, dataLength, ref result, readers[typeCode], readers[typeCode]]
            var skipValueLabel = il.DefineLabel("skipValue");
            il.Brfalse(skipValueLabel); // if(readers[typeCode] == 0) goto skipValue;
            var parameterTypes = new[] {typeof(byte*), typeof(int).MakeByRefType(), typeof(int), typeof(object).MakeByRefType()};
            il.Calli(CallingConventions.Standard, typeof(void), parameterTypes); // readers[typeCode](data, ref index, dataLength, ref result); stack: []
            il.Ret();
            il.MarkLabel(skipValueLabel);
            il.Pop();
            il.Pop();
            il.Pop();
            il.Pop();
            il.Pop();
            context.IncreaseIndexBy1();
            context.SkipValue();
        }

        private static KeyValuePair<Delegate, IntPtr>[] GetReaders(ReaderTypeBuilderContext context)
        {
            var result = new KeyValuePair<Delegate, IntPtr>[256];
            foreach(var type in primitiveTypes)
                result[(int)GroBufTypeCodeMap.GetTypeCode(type)] = GetReader(context, type);
            return result;
        }

        private static KeyValuePair<Delegate, IntPtr> GetReader(ReaderTypeBuilderContext context, Type type)
        {
            var method = new DynamicMethod("Read_" + type.Name + "_AndCastToObject_" + Guid.NewGuid(), typeof(void),
                                           new[]
                                               {
                                                   typeof(IntPtr), typeof(int).MakeByRefType(), typeof(int), typeof(object).MakeByRefType()
                                               }, context.Module, true);
            var il = new GroboIL(method);
            il.Ldarg(3); // stack: [ref result]
            il.Ldarg(0); // stack: [ref result, data]
            il.Ldarg(1); // stack: [ref result, data, ref index]
            il.Ldarg(2); // stack: [ref result, data, ref index, dataLength]
            var value = il.DeclareLocal(type);
            il.Ldloca(value); // stack: [ref result, data, ref index, dataLength, ref value]
            var reader = context.GetReader(type).Pointer;
            if(reader == IntPtr.Zero)
                throw new InvalidOperationException();
            il.Ldc_IntPtr(reader);
            il.Calli(CallingConventions.Standard, typeof(void), new[] {typeof(IntPtr), typeof(int).MakeByRefType(), typeof(int), type.MakeByRefType()}); // read<type>(data, ref index, dataLength, ref value); stack: [ref result]
            il.Ldloc(value); // stack: [ref result, value]
            if(type.IsValueType)
                il.Box(type); // stack: [ref result, (object)value]
            il.Stind(typeof(object)); // result = (object)value
            il.Ret();
            var @delegate = method.CreateDelegate(typeof(ReaderDelegate));
            return new KeyValuePair<Delegate, IntPtr>(@delegate, GroBufHelpers.ExtractDynamicMethodPointer(method));
        }

        private static readonly Type[] primitiveTypes = new[]
            {
                typeof(bool), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
                typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(string), typeof(Guid), typeof(DateTime), typeof(Array)
            };
    }
}