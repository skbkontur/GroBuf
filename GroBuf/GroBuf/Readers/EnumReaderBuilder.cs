using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace GroBuf.Readers
{
    internal class EnumReaderBuilder<T> : ReaderBuilderBase<T>
    {
        public EnumReaderBuilder()
        {
            if(!Type.IsEnum) throw new InvalidOperationException("Enum expected but was '" + Type + "'");
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext<T> context)
        {
            int[] values;
            ulong[] hashCodes;
            BuildValuesTable(out values, out hashCodes);
            var valuesField = context.Context.BuildConstField("values_" + Type.Name + "_" + Guid.NewGuid(), values);
            var hashCodesField = context.Context.BuildConstField("hashCodes_" + Type.Name + "_" + Guid.NewGuid(), hashCodes);
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

            context.LoadField(hashCodesField); // stack: [hashCode, hashCodes]
            context.Il.Emit(OpCodes.Ldloc, idx); // stack: [hashCode, hashCodes, idx]
            context.Il.Emit(OpCodes.Ldelem_I8); // stack: [hashCode, hashCodes[idx]]
            var returnDefaultLabel = context.Il.DefineLabel();
            context.Il.Emit(OpCodes.Bne_Un, returnDefaultLabel); // if(hashCode != hashCodes[idx]) goto returnDefault;
            context.LoadField(valuesField); // stack: [values]
            context.Il.Emit(OpCodes.Ldloc, idx); // stack: [values, idx]
            context.Il.Emit(OpCodes.Ldelem_I4); // stack: [values[idx]]
            context.Il.Emit(OpCodes.Ret);
            context.Il.MarkLabel(returnDefaultLabel);
            context.Il.Emit(OpCodes.Ldc_I4_0); // stack: [0]
        }

        private void BuildValuesTable(out int[] values, out ulong[] hashCodes)
        {
            var names = Enum.GetNames(Type);
            var arr = Enum.GetValues(Type);
            var hashes = GroBufHelpers.CalcHashAndCheck(names);
            var hashSet = new HashSet<uint>();
            for(var x = (uint)hashes.Length;; ++x)
            {
                hashSet.Clear();
                bool ok = true;
                foreach(var hash in hashes)
                {
                    var item = (uint)(hash % x);
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
                for(int i = 0; i < hashes.Length; i++)
                {
                    var hash = hashes[i];
                    var index = (int)(hash % x);
                    hashCodes[index] = hash;
                    values[index] = (int)arr.GetValue(i);
                }
                return;
            }
        }
    }
}