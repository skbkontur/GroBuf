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
                    new KeyValuePair<string, Type>("delegates_" + Type.Name + "_" + Guid.NewGuid(), typeof(Delegate[])) // This field is needed only to save references to the dynamic methods. Otherwise GC will destroy them
                });
            Array.ForEach(GroBufHelpers.LeafTypes.Where(type => type != null).ToArray(), type => context.BuildConstants(type));
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            var il = context.Il;

            var readers = GetReaders(context);
            var readersField = context.Context.InitConstField(Type, 0, readers.Select(pair => pair.Value).ToArray());
            context.Context.InitConstField(Type, 1, readers.Select(pair => pair.Key).ToArray());

            context.LoadData(); // stack: [data]
            context.LoadIndexByRef(); // stack: [data, ref index]
            context.LoadResultByRef(); // stack: [data, ref index, ref result]
            context.LoadContext(); // stack: [data, ref index, ref result, context]
            context.LoadField(readersField); // stack: [data, ref index, ref result, context, readers]
            il.Ldloc(context.TypeCode); // stack: [data, ref index, ref result, context, readers, typeCode]
            il.Ldelem(typeof(IntPtr)); // stack: [data, ref index, ref result, context, readers[typeCode]]
            il.Dup(); // stack: [data, ref index, ref result, context, readers[typeCode], readers[typeCode]]
            var skipValueLabel = il.DefineLabel("skipValue");
            il.Brfalse(skipValueLabel); // if(readers[typeCode] == 0) goto skipValue;
            var parameterTypes = new[] {typeof(byte*), typeof(int).MakeByRefType(), typeof(object).MakeByRefType(), typeof(ReaderContext)};
            il.Calli(CallingConventions.Standard, typeof(void), parameterTypes); // readers[typeCode](data, ref index, ref result, context); stack: []
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

        protected override bool IsReference { get { return false; } }

        private static KeyValuePair<Delegate, IntPtr>[] GetReaders(ReaderMethodBuilderContext context)
        {
            var result = new KeyValuePair<Delegate, IntPtr>[256];
            foreach(var type in GroBufHelpers.LeafTypes.Where(type => type != null))
                result[(int)GroBufTypeCodeMap.GetTypeCode(type)] = GetReader(context, type);
            result[(int)GroBufTypeCode.DateTimeOld] = result[(int)GroBufTypeCode.DateTimeNew];
            return result;
        }

        private static KeyValuePair<Delegate, IntPtr> GetReader(ReaderMethodBuilderContext context, Type type)
        {
            var method = new DynamicMethod("Read_" + type.Name + "_AndCastToObject_" + Guid.NewGuid(), typeof(void),
                                           new[]
                                               {
                                                   typeof(IntPtr), typeof(int).MakeByRefType(), typeof(object).MakeByRefType(), typeof(ReaderContext)
                                               }, context.Context.Module, true);
            using(var il = new GroboIL(method))
            {
                il.Ldarg(2); // stack: [ref result]
                il.Ldarg(0); // stack: [ref result, data]
                il.Ldarg(1); // stack: [ref result, data, ref index]
                var value = il.DeclareLocal(type);
                il.Ldloca(value); // stack: [ref result, data, ref index, ref value]
                il.Ldarg(3); // stack: [ref result, data, ref index, ref value, context]

                ReaderMethodBuilderContext.CallReader(il, type, context.Context);

                il.Ldloc(value); // stack: [ref result, value]
                if(type.IsValueType)
                    il.Box(type); // stack: [ref result, (object)value]
                else
                    il.Castclass(type);
                il.Stind(typeof(object)); // result = (object)value
                il.Ret();
            }
            var @delegate = method.CreateDelegate(typeof(ReaderDelegate));
            return new KeyValuePair<Delegate, IntPtr>(@delegate, GroBufHelpers.ExtractDynamicMethodPointer(method));
        }
    }
}