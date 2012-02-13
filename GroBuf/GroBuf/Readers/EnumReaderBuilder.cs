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
            var il = context.Il;
            il.Emit(OpCodes.Ldloc, context.TypeCode); // stack: [typeCode]
            il.Emit(OpCodes.Ldc_I4, (int)GroBufTypeCode.Enum);
            var readAsIntLabel = il.DefineLabel();
            il.Emit(OpCodes.Bne_Un, readAsIntLabel);
            context.IncreaseIndexBy1();
            il.Emit(OpCodes.Ldc_I4_8); // stack: [8]
            context.AssertLength();
            context.LoadResultByRef(); // stack: [ref result]
            context.GoToCurrentLocation(); // stack: [ref result, &result[index]]
            il.Emit(OpCodes.Ldind_I8); // stack: [ref result, *(int64*)result[index] = hashCode]
            context.IncreaseIndexBy8(); // index = index + 8; stack: [ref result, hashCode]

            il.Emit(OpCodes.Dup); // stack: [ref result, hashCode, hashCode]
            il.Emit(OpCodes.Ldc_I8, (long)hashCodes.Length); // stack: [ref result, hashCode, hashCode, (int64)hashCodes.Length]
            il.Emit(OpCodes.Rem_Un); // stack: [ref result, hashCode, hashCode % hashCodes.Length = idx]
            il.Emit(OpCodes.Conv_I4); // stack: [ref result, hashCode, (int)(hashCode % hashCodes.Length)]
            var idx = context.Length;
            il.Emit(OpCodes.Stloc, idx); // idx = (int)(hashCode % hashCodes.Length); stack: [ref result, hashCode]

            context.LoadField(hashCodesField); // stack: [ref result, hashCode, hashCodes]
            il.Emit(OpCodes.Ldloc, idx); // stack: [ref result, hashCode, hashCodes, idx]
            il.Emit(OpCodes.Ldelem_I8); // stack: [ref result, hashCode, hashCodes[idx]]
            var returnDefaultLabel = il.DefineLabel();
            il.Emit(OpCodes.Bne_Un, returnDefaultLabel); // if(hashCode != hashCodes[idx]) goto returnDefault; stack: [ref result]
            context.LoadField(valuesField); // stack: [ref result, values]
            il.Emit(OpCodes.Ldloc, idx); // stack: [ref result, values, idx]
            il.Emit(OpCodes.Ldelem_I4); // stack: [ref result, values[idx]]
            il.Emit(OpCodes.Stind_I4); // result = values[idx]; stack: []
            il.Emit(OpCodes.Ret);
            il.MarkLabel(returnDefaultLabel);
            il.Emit(OpCodes.Ldc_I4_0); // stack: [0]
            il.Emit(OpCodes.Stind_I4); // result = 0
            il.Emit(OpCodes.Ret);
            il.MarkLabel(readAsIntLabel);
            context.LoadData(); // stack: [pinnedData]
            context.LoadIndexByRef(); // stack: [pinnedData, ref index]
            context.LoadDataLength(); // stack: [pinnedData, ref index, dataLength]
            context.LoadResultByRef(); // stack: [pinnedData, ref index, dataLength, ref result]
            il.Emit(OpCodes.Call, context.Context.GetReader(typeof(int))); // reader<int>(pinnedData, ref index, dataLength, ref result)
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