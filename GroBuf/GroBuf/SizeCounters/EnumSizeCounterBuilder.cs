using System;
using System.Collections.Generic;

namespace GroBuf.SizeCounters
{
    internal class EnumSizeCounterBuilder : SizeCounterBuilderBase
    {
        public EnumSizeCounterBuilder(Type type)
            : base(type)
        {
            if(!Type.IsEnum) throw new InvalidOperationException("Enum expected but was '" + Type + "'");
        }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
            context.SetFields(Type, new[]
                {
                    new KeyValuePair<string, Type>("hashCodes_" + Type.Name + "_" + Guid.NewGuid(), typeof(ulong[])),
                    new KeyValuePair<string, Type>("values_" + Type.Name + "_" + Guid.NewGuid(), typeof(int[])),
                });
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
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
            var countAsIntLabel = il.DefineLabel("countAsInt");
            il.Brfalse(countAsIntLabel); // if(values[obj % values.Length] != obj) goto countAsInt
            context.LoadField(hashCodesField); // stack: [hashCodes]
            context.LoadObj(); // stack: [hashCodes, obj]
            il.Ldc_I4(hashCodes.Length); // stack: [hashCodes, obj, hashCodes.Length]
            il.Rem(true); // stack: [hashCodes, obj % hashCodes.Length]
            il.Ldelem(typeof(long)); // stack: [hashCodes[obj % hashCodes.Length] = hashCode]
            il.Brfalse(countAsIntLabel); // if(hashCode == 0) goto countAsInt;
            il.Ldc_I4(9); // stack: [9]
            il.Ret(); // return 9;
            il.MarkLabel(countAsIntLabel);
            il.Ldc_I4(5); // stack: [5]
        }

        protected override bool IsReference { get { return false; } }
    }
}