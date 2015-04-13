using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using GroBuf.DataMembersExtracters;

namespace GroBuf.Readers
{
    internal class EnumReaderBuilder : ReaderBuilderBase
    {
        public EnumReaderBuilder(Type type)
            : base(type)
        {
            if(!Type.IsEnum) throw new InvalidOperationException("Enum expected but was '" + Type + "'");
        }

        protected override void BuildConstantsInternal(ReaderConstantsBuilderContext context)
        {
            context.SetFields(Type, new[]
                {
                    new KeyValuePair<string, Type>("values_" + Type.Name + "_" + Guid.NewGuid(), typeof(int[])),
                    new KeyValuePair<string, Type>("hashCodes_" + Type.Name + "_" + Guid.NewGuid(), typeof(ulong[])),
                });
            context.BuildConstants(typeof(int));
            context.BuildConstants(typeof(string));
        }

        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            int[] values;
            ulong[] hashCodes;
            BuildValuesTable(out values, out hashCodes);
            var valuesField = context.Context.InitConstField(Type, 0, values);
            var hashCodesField = context.Context.InitConstField(Type, 1, hashCodes);
            var il = context.Il;
            il.Ldloc(context.TypeCode); // stack: [typeCode]
            il.Ldc_I4((int)GroBufTypeCode.Enum);
            var tryParseLabel = il.DefineLabel("tryParse");
            il.Bne_Un(tryParseLabel); // if(typeCode != GroBufTypeCode.Enum) goto tryParse;
            context.IncreaseIndexBy1();
            il.Ldc_I4(8); // stack: [8]
            context.AssertLength();
            context.LoadResultByRef(); // stack: [ref result]
            context.GoToCurrentLocation(); // stack: [ref result, &result[index]]
            il.Ldind(typeof(long)); // stack: [ref result, *(int64*)result[index] = hashCode]
            context.IncreaseIndexBy8(); // index = index + 8; stack: [ref result, hashCode]

            var parseByHashCodeLabel = il.DefineLabel("parseByHashCode");
            il.MarkLabel(parseByHashCodeLabel);

            il.Dup(); // stack: [ref result, hashCode, hashCode]
            il.Ldc_I8(hashCodes.Length); // stack: [ref result, hashCode, hashCode, (int64)hashCodes.Length]
            il.Rem(true); // stack: [ref result, hashCode, hashCode % hashCodes.Length = idx]
            il.Conv<int>(); // stack: [ref result, hashCode, (int)(hashCode % hashCodes.Length)]
            var idx = context.Length;
            il.Stloc(idx); // idx = (int)(hashCode % hashCodes.Length); stack: [ref result, hashCode]

            context.LoadField(hashCodesField); // stack: [ref result, hashCode, hashCodes]
            il.Ldloc(idx); // stack: [ref result, hashCode, hashCodes, idx]
            il.Ldelem(typeof(long)); // stack: [ref result, hashCode, hashCodes[idx]]
            var returnDefaultLabel = il.DefineLabel("returnDefault");
            il.Bne_Un(returnDefaultLabel); // if(hashCode != hashCodes[idx]) goto returnDefault; stack: [ref result]
            context.LoadField(valuesField); // stack: [ref result, values]
            il.Ldloc(idx); // stack: [ref result, values, idx]
            il.Ldelem(typeof(int)); // stack: [ref result, values[idx]]
            il.Stind(typeof(int)); // result = values[idx]; stack: []
            il.Ret();
            il.MarkLabel(returnDefaultLabel);
            il.Ldc_I4(0); // stack: [0]
            il.Stind(typeof(int)); // result = 0
            il.Ret();

            il.MarkLabel(tryParseLabel);
            il.Ldloc(context.TypeCode); // stack: [typeCode]
            il.Ldc_I4((int)GroBufTypeCode.String); // stack: [typeCode, GroBufTypeCode.String]
            var readAsIntLabel = il.DefineLabel("readAsInt");
            il.Bne_Un(readAsIntLabel); // if(typeCode != GroBufTypeCode.String) goto readAsInt;
            var str = il.DeclareLocal(typeof(string));

//            context.LoadReader(typeof(string));

            context.LoadData(); // stack: [pinnedData]
            context.LoadIndexByRef(); // stack: [pinnedData, ref index]
            il.Ldloca(str); // stack: [pinnedData, ref index, ref str]
            context.LoadContext(); // stack: [pinnedData, ref index, ref str, context]
            context.CallReader(typeof(string)); // reader<string>(pinnedData, ref index, ref str, context); stack: []
            context.LoadResultByRef(); // stack: [ref result]
            il.Ldloc(str); // stack: [ref result, str]
            il.Call(typeof(GroBufHelpers).GetMethod("CalcHash", BindingFlags.Public | BindingFlags.Static)); // stack: [ref result, GroBufHelpers.CalcHash(str)]
            il.Br(parseByHashCodeLabel);

            il.MarkLabel(readAsIntLabel);

//            context.LoadReader(typeof(int));

            context.LoadData(); // stack: [pinnedData]
            context.LoadIndexByRef(); // stack: [pinnedData, ref index]
            context.LoadResultByRef(); // stack: [pinnedData, ref index, ref result]
            context.LoadContext(); // stack: [pinnedData, ref index, ref result, context]
            context.CallReader(typeof(int)); // reader<int>(pinnedData, ref index, ref result, context)
        }

        protected override bool IsReference { get { return false; } }

        private void BuildValuesTable(out int[] values, out ulong[] hashCodes)
        {
            var fields = Type.GetFields(BindingFlags.Public | BindingFlags.Static);
            var arr = fields.Select(field => (int)Enum.Parse(Type, field.Name)).ToArray();
            var hashes = GroBufHelpers.CalcHashesAndCheck(fields.Select(DataMember.Create));
            var hashSet = new HashSet<uint>();
            for(var x = (uint)hashes.Length;; ++x)
            {
                hashSet.Clear();
                var ok = true;
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
                for(var i = 0; i < hashes.Length; i++)
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