using System;
using System.Collections.Generic;

namespace GroBuf.Writers
{
    internal class EnumWriterBuilder : WriterBuilderBase
    {
        public EnumWriterBuilder(Type type)
            : base(type)
        {
            if(!Type.IsEnum) throw new InvalidOperationException("Enum expected but was " + Type);
        }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            context.SetFields(Type, new[]
                {
                    new KeyValuePair<string, Type>("hashCodes_" + Type.Name + "_" + Guid.NewGuid(), typeof(ulong[])),
                    new KeyValuePair<string, Type>("values_" + Type.Name + "_" + Guid.NewGuid(), typeof(int[])),
                });
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            int[] values;
            ulong[] hashCodes;
            EnumHelpers.BuildHashCodesTable(Type, out values, out hashCodes);
            var hashCodesField = context.Context.InitConstField(Type, 0, hashCodes);
            var valuesField = context.Context.InitConstField(Type, 1, values);

            var il = context.Il;
            context.LoadField(valuesField); // stack: [values]
            context.LoadObj(); // stack: [values, obj]
            il.Ldc_I4(values.Length); // stack: [values, obj, values.Length]
            il.Rem(true); // stack: [values, obj % values.Length]
            il.Ldelem(typeof(int)); // stack: [values[obj % values.Length]]
            context.LoadObj(); // stack: [values[obj % values.Length], obj]
            il.Ceq(); // stack: [values[obj % values.Length] == obj]
            var writeIntLabel = il.DefineLabel("writeInt");
            il.Brfalse(writeIntLabel); // if(values[obj % values.Length] != obj) goto writeInt

            context.LoadField(hashCodesField); // stack: [hashCodes]
            context.LoadObj(); // stack: [hashCodes, obj]
            il.Ldc_I4(hashCodes.Length); // stack: [hashCodes, obj, hashCodes.Length]
            il.Rem(true); // stack: [hashCodes, obj % hashCodes.Length]
            il.Ldelem(typeof(long)); // stack: [hashCodes[obj % hashCodes.Length] = hashCode]
            var hashCode = il.DeclareLocal(typeof(ulong));
            il.Dup(); // stack: [hashCode, hashCode]
            il.Stloc(hashCode); // hashCode = hashCodes[obj % hashCodes.Length]; stack: [hashCode]
            il.Brfalse(writeIntLabel);
            context.WriteTypeCode(GroBufTypeCode.Enum);
            il.Ldc_I4(8);
            context.AssertLength();
            context.GoToCurrentLocation(); // stack: [&result[index]]
            il.Ldloc(hashCode); // stack: [&result[index], hashCode]
            il.Stind(typeof(long)); // *(int64*)&result[index] = hashCode
            context.IncreaseIndexBy8(); // index = index + 8;
            il.Ret();
            il.MarkLabel(writeIntLabel);
            context.WriteTypeCode(GroBufTypeCode.Int32);
            il.Ldc_I4(4);
            context.AssertLength();
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObj(); // stack: [&result[index], obj]
            il.Stind(typeof(int)); // result[index] = obj
            context.IncreaseIndexBy4(); // index = index + 4
        }

        protected override bool IsReference { get { return false; } }
    }
}