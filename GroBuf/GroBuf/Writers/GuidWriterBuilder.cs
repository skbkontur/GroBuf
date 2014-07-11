using System;

namespace GroBuf.Writers
{
    internal class GuidWriterBuilder : WriterBuilderBase
    {
        public GuidWriterBuilder()
            : base(typeof(Guid))
        {
        }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            context.WriteTypeCode(GroBufTypeCode.Guid);
            il.Ldc_I4(16);
            context.AssertLength();
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObjByRef(); // stack: [&result[index], &obj]
            il.Ldind(typeof(long)); // stack: [&result[index], (int64)*obj]
            il.Stind(typeof(long)); // result[index] = (int64)*obj
            context.IncreaseIndexBy8(); // index = index + 8
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObjByRef(); // stack: [&result[index], &obj]
            il.Ldc_I4(8); // stack: [&result[index], &obj, 8]
            il.Add(); // stack: [&result[index], &obj + 8]
            il.Ldind(typeof(long)); // stack: [&result[index], *(&obj+8)]
            il.Stind(typeof(long)); // result[index] = (int64)*(obj + 8)
            context.IncreaseIndexBy8(); // index = index + 8
        }

        protected override bool IsReference { get { return false; } }
    }
}