using System;
using System.Collections.Generic;

using GrEmit.Utils;

namespace GroBuf.SizeCounters
{
    internal class CustomSizeCounterBuilder : SizeCounterBuilderBase
    {
        private readonly IGroBufCustomSerializer customSerializer;

        public CustomSizeCounterBuilder(Type type, IGroBufCustomSerializer customSerializer)
            : base(type)
        {
            this.customSerializer = customSerializer;
        }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
            context.SetFields(Type, new[] { new KeyValuePair<string, Type>("customSerializer_" + Type.Name + "_" + Guid.NewGuid(), typeof(IGroBufCustomSerializer)) });
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var customSerializerField = context.Context.InitConstField(Type, 0, customSerializer);
            var il = context.Il;

            context.LoadField(customSerializerField); // stack: [customSerializer]
            context.LoadObj(); // stack: [customSerializer, obj]
            if(Type.IsValueType)
                il.Box(Type); // stack: [customSerializer, (object)obj]
            context.LoadWriteEmpty(); // stack: [customSerializer, (object)obj, writeEmpty]
            il.Call(HackHelpers.GetMethodDefinition<IGroBufCustomSerializer>(serializer => serializer.CountSize(null, false)), typeof(IGroBufCustomSerializer)); // stack: [customSerializer.CountSize((object)obj, writeEmpty)]

            var countLengthLabel = il.DefineLabel("countLength");
            il.Dup(); // stack: [size, size]
            il.Brtrue(countLengthLabel); // if(size != 0) goto countLength; stack: [size]
            il.Pop(); // stack: []
            context.ReturnForNull();
            il.Ret();
            il.MarkLabel(countLengthLabel);
            il.Ldc_I4(5); // stack: [size, 5]
            il.Add(); // stack: [size + 5]
        }
    }
}