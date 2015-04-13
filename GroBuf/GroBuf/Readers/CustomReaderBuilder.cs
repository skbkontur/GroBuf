using System;
using System.Collections.Generic;

using GrEmit.Utils;

namespace GroBuf.Readers
{
    internal class CustomReaderBuilder : ReaderBuilderBase
    {
        public CustomReaderBuilder(Type type, IGroBufCustomSerializer customSerializer)
            : base(type)
        {
            this.customSerializer = customSerializer;
        }

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
            context.SetFields(Type, new[] {new KeyValuePair<string, Type>("customSerializer_" + Type.Name + "_" + Guid.NewGuid(), typeof(IGroBufCustomSerializer))});
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            var customSerializerField = context.Context.InitConstField(Type, 0, customSerializer);
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
            context.LoadField(customSerializerField); // stack: [customSerializer]
            context.LoadData(); // stack: [customSerializer, data]
            context.LoadIndexByRef(); // stack: [customSerializer, data, ref index]
            il.Ldloca(local); // stack: [customSerializer, data, ref index, ref local]
            context.LoadContext(); // stack: [customSerializer, data, ref index, ref local, context]
            int dummy = 0;
            object dummyObj = null;
            il.Call(HackHelpers.GetMethodDefinition<IGroBufCustomSerializer>(serializer => serializer.Read(IntPtr.Zero, ref dummy, ref dummyObj, null))); // customSerializer.Read(data, ref index, length, ref local); stack: []

            context.LoadResultByRef(); // stack: [ref result]
            il.Ldloc(local); // stack: [ref result, ref local]
            if(!Type.IsValueType)
            {
                il.Castclass(Type);
                il.Stind(Type);
            }
            else
            {
                il.Unbox_Any(Type); // stack: [ref result, (Type)local]
                il.Stobj(Type); // result = (Type)local
            }

            context.StoreObject(Type);
        }

        protected override bool IsReference { get { return false; } }

        private readonly IGroBufCustomSerializer customSerializer;
    }
}