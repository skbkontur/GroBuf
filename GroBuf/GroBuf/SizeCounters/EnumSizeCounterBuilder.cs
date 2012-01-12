using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace GroBuf.SizeCounters
{
    internal class EnumSizeCounterBuilder<T> : SizeCounterBuilderBase<T>
    {
        public EnumSizeCounterBuilder()
        {
            if(!Type.IsEnum) throw new InvalidOperationException("Enum expected but was '" + Type + "'");
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var hashCodes = BuildHashCodesTable();
            var hashCodesField = context.Context.BuildConstField("hashCodes_" + Type.Name + "_" + Guid.NewGuid(), hashCodes);

            context.LoadField(hashCodesField); // stack: [hashCodes]
            context.LoadObj(); // stack: [hashCodes, obj]
            context.Il.Emit(OpCodes.Ldc_I4, hashCodes.Length); // stack: [hashCodes, obj, hashCodes.Length]
            context.Il.Emit(OpCodes.Rem_Un); // stack: [hashCodes, obj % hashCodes.Length]
            context.Il.Emit(OpCodes.Ldelem_I8); // stack: [hashCodes[obj % hashCodes.Length] = hashCode]
            var countAsIntLabel = context.Il.DefineLabel();
            context.Il.Emit(OpCodes.Brfalse, countAsIntLabel); // if(hashCode == 0) goto countAsInt;
            context.Il.Emit(OpCodes.Ldc_I4, 9); // stack: [9]
            context.Il.Emit(OpCodes.Ret); // return 9;
            context.Il.MarkLabel(countAsIntLabel);
            context.Il.Emit(OpCodes.Ldc_I4_5); // stack: [5]
        }

        private ulong[] BuildHashCodesTable()
        {
            var values = (int[])Enum.GetValues(Type);
            var hashes = GroBufHelpers.CalcHashAndCheck(Enum.GetNames(Type));
            var hashSet = new HashSet<uint>();
            for(var x = (uint)values.Length;; ++x)
            {
                hashSet.Clear();
                bool ok = true;
                foreach(var value in values)
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
                    hashCodes[index] = hashes[i];
                }
                return hashCodes;
            }
        }
    }
}