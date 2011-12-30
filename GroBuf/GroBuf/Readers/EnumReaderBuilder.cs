using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Readers
{
    internal class EnumReaderBuilder<T> : ReaderBuilderWithTwoParams<T, int[], ulong[]>
    {
        public EnumReaderBuilder(IReaderCollection readerCollection)
            : base(readerCollection)
        {
            if(!Type.IsEnum) throw new InvalidOperationException("Enum expected but was " + Type);
        }

        protected override Tuple<int[], ulong[]> ReadNotEmpty(ReaderBuilderContext<T> context)
        {
            int[] values;
            ulong[] hashCodes;
            BuildValuesTable(out values, out hashCodes);
            context.AssertTypeCode(GroBufTypeCode.Enum);
            context.IncreaseIndexBy1();
            context.Il.Emit(OpCodes.Ldc_I4_8); // stack: [8]
            context.AssertLength();
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.Il.Emit(OpCodes.Ldind_I8); // stack: [*(int64*)result[index] = hashCode]
            context.IncreaseIndexBy8(); // index = index + 8 // stack: [hashCode]

            context.Il.Emit(OpCodes.Dup); // stack: [hashCode, hashCode]
            context.Il.Emit(OpCodes.Ldc_I8, (long)hashCodes.Length); // stack: [hashCode, hashCode, (int64)hashCodes.Length]
            context.Il.Emit(OpCodes.Rem_Un); // stack: [hashCode, hashCode % hashCodes.Length = idx]
            context.Il.Emit(OpCodes.Conv_I4); // stack: [hashCode, (int)(hashCode % hashCodes.Length)]
            var idx = context.Length;
            context.Il.Emit(OpCodes.Stloc, idx); // idx = (int)(hashCode % hashCodes.Length); stack: [hashCode]
            context.LoadAdditionalParam(1); // stack: [hashCode, hashCodes]
            context.Il.Emit(OpCodes.Ldloc, idx); // stack: [hashCode, hashCodes, idx]
            context.Il.Emit(OpCodes.Ldelem_I8); // stack: [hashCode, hashCodes[idx]]
            var returnDefaultLabel = context.Il.DefineLabel();
            context.Il.Emit(OpCodes.Bne_Un, returnDefaultLabel); // if(hashCode != hashCodes[idx]) goto returnDefault;
            context.LoadAdditionalParam(0); // stack: [values]
            context.Il.Emit(OpCodes.Ldloc, idx); // stack: [values, idx]
            context.Il.Emit(OpCodes.Ldelem_I4); // stack: [values[idx]]
            context.Il.Emit(OpCodes.Ret);
            context.Il.MarkLabel(returnDefaultLabel);
            context.Il.Emit(OpCodes.Ldc_I4_0); // stack: [0]
            return new Tuple<int[], ulong[]>(values, hashCodes);
        }

        private void BuildValuesTable(out int[] values, out ulong[] hashCodes)
        {
            var names = Enum.GetNames(Type);
            var arr = Enum.GetValues(Type);
            var items = names.Select((s, i) => new Tuple<ulong, int>(GroBufHelpers.CalcHash(s), (int)arr.GetValue(i))).ToArray();
            var hashSet = new HashSet<uint>();
            for(var x = (uint)items.Length;; ++x)
            {
                hashSet.Clear();
                bool ok = true;
                foreach(var t in items)
                {
                    var item = (uint)(t.Item1 % x);
                    if(hashSet.Contains(item))
                    {
                        ok = false;
                        break;
                    }
                    hashSet.Add(item);
                }
                if(!ok) continue;
                hashCodes = new ulong[x];
                values = new int[x];
                foreach(var t in items)
                {
                    var index = (int)(t.Item1 % x);
                    hashCodes[index] = t.Item1;
                    values[index] = t.Item2;
                }
                return;
            }
        }
    }
}