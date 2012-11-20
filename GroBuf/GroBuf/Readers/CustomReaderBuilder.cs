using System;
using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.Readers
{
    internal class CustomReaderBuilder : ReaderBuilderBase
    {
        private readonly MethodInfo reader;

        public CustomReaderBuilder(Type type, MethodInfo reader)
            : base(type)
        {
            this.reader = reader;
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            var groBufReader = context.Context.GroBufReader;
            Func<Type, ReaderDelegate> readersFactory = type => ((IntPtr data, ref int index, int length, ref object result) => groBufReader.Read(type, data, ref index, length, ref result));
            var readerDelegate = (ReaderDelegate)reader.Invoke(null, new[] { readersFactory });
            var readerField = context.Context.BuildConstField("reader_" + Type.Name + "_" + Guid.NewGuid(), readerDelegate);
            var il = context.Il;

            context.IncreaseIndexBy1(); // index = index + 1
            context.AssertTypeCode(GroBufTypeCode.CustomData);

            il.Emit(OpCodes.Ldc_I4_4);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Emit(OpCodes.Ldind_U4); // stack: [(uint)data[index]]
            context.IncreaseIndexBy4(); // index = index + 4; stack: [(uint)data[index]]

            context.AssertLength(); // stack: []

            var local = il.DeclareLocal(typeof(object));
            context.LoadField(readerField); // stack: [readerDelegate]
            context.LoadData(); // stack: [readerDelegate, data]
            context.LoadIndexByRef(); // stack: [readerDelegate, data, ref index]
            context.LoadDataLength(); // stack: [readerDelegate, data, ref index, length]
            if(!Type.IsValueType)
                context.LoadResultByRef(); // stack: [readerDelegate, data, ref index, length, ref result]
            else
                il.Emit(OpCodes.Ldloca, local); // stack: [readerDelegate, data, ref index, length, ref local]
            il.Emit(OpCodes.Call, typeof(ReaderDelegate).GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance)); // readerDelegate.Invoke(readerDelegate, data, ref index, length, ref local)
            if(Type.IsValueType)
            {
                context.LoadResultByRef(); // stack: [ref result]
                il.Emit(OpCodes.Ldloc, local); // stack: [ref result, ref local]
                il.Emit(OpCodes.Unbox, Type); // stack: [ref result, ref (Type)local]
                il.Emit(OpCodes.Ldobj, Type); // stack: [ref result, (Type)local]
                il.Emit(OpCodes.Stobj, Type); // result = (Type)local
            }
        }
    }
}