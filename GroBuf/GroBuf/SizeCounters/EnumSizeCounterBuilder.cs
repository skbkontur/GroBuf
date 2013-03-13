using System;
using System.Collections.Generic;
using System.Linq;

namespace GroBuf.SizeCounters
{
    internal class EnumSizeCounterBuilder : SizeCounterBuilderBase
    {
        public EnumSizeCounterBuilder(Type type)
            : base(type)
        {
            if(!Type.IsEnum) throw new InvalidOperationException("Enum expected but was '" + Type + "'");
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var hashCodes = BuildHashCodesTable();
            var hashCodesField = context.Context.BuildConstField("hashCodes_" + Type.Name + "_" + Guid.NewGuid(), hashCodes);

            var il = context.Il;
            context.LoadField(hashCodesField); // stack: [hashCodes]
            context.LoadObj(); // stack: [hashCodes, obj]
            il.Ldc_I4(hashCodes.Length); // stack: [hashCodes, obj, hashCodes.Length]
            il.Rem(typeof(uint)); // stack: [hashCodes, obj % hashCodes.Length]
            il.Ldelem(typeof(long)); // stack: [hashCodes[obj % hashCodes.Length] = hashCode]
            var countAsIntLabel = il.DefineLabel("countAsInt");
            il.Brfalse(countAsIntLabel); // if(hashCode == 0) goto countAsInt;
            il.Ldc_I4(9); // stack: [9]
            il.Ret(); // return 9;
            il.MarkLabel(countAsIntLabel);
            il.Ldc_I4(5); // stack: [5]
        }

        private ulong[] BuildHashCodesTable()
        {
            var values = (int[])Enum.GetValues(Type);
            var uniqueValues = new HashSet<int>(values).ToArray();
            var nameHashes = GroBufHelpers.CalcHashAndCheck(Enum.GetNames(Type));
            var hashSet = new HashSet<uint>();
            for(var x = (uint)values.Length;; ++x)
            {
                hashSet.Clear();
                bool ok = true;
                foreach(var value in uniqueValues)
                {
                    var item = (uint)(value % x);
                    if(hashSet.Contains(item))
                    {
                        ok = false;
                        break;
                    }
                    hashSet.Add(item);
                }
                if(!ok) continue;
                var hashCodes = new ulong[x];
                for(int i = 0; i < values.Length; i++)
                {
                    var value = values[i];
                    var index = (int)(value % x);
                    hashCodes[index] = nameHashes[i];
                }
                return hashCodes;
            }
        }
    }
}