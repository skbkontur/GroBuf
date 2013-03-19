using System;
using System.Collections.Generic;
using System.Reflection;

namespace GroBuf.Readers
{
    internal class CustomReaderBuilder : ReaderBuilderBase
    {
        public CustomReaderBuilder(Type type, MethodInfo reader)
            : base(type)
        {
            this.reader = reader;
        }

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
            context.SetFields(Type, new[] {new KeyValuePair<string, Type>("reader_" + Type.Name + "_" + Guid.NewGuid(), typeof(ReaderDelegate))});
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            var groBufReader = context.Context.GroBufReader;
            Func<Type, ReaderDelegate> readersFactory = type => ((IntPtr data, ref int index, int length, ref object result) => groBufReader.Read(type, data, ref index, length, ref result));
            var readerDelegate = (ReaderDelegate)reader.Invoke(null, new[] {readersFactory});
            var readerField = context.Context.InitConstField(Type, 0, readerDelegate);
            var il = context.Il;

            context.IncreaseIndexBy1(); // index = index + 1
            context.AssertTypeCode(GroBufTypeCode.CustomData);

            il.Ldc_I4(4);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Ldind(typeof(uint)); // stack: [(uint)data[index]]
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
                il.Ldloca(local); // stack: [readerDelegate, data, ref index, length, ref local]
            il.Call(typeof(ReaderDelegate).GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance), typeof(ReaderDelegate)); // readerDelegate.Invoke(readerDelegate, data, ref index, length, ref local)
            if(Type.IsValueType)
            {
                context.LoadResultByRef(); // stack: [ref result]
                il.Ldloc(local); // stack: [ref result, ref local]
                il.Unbox_Any(Type); // stack: [ref result, (Type)local]
                il.Stobj(Type); // result = (Type)local
            }
        }

        private readonly MethodInfo reader;
    }
}