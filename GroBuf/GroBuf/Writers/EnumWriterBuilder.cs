using System;
using System.Collections.Generic;
using System.Linq;

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
            var table = BuildHashCodesTable();
            var hashCodes = table.Key;
            var values = table.Value;
            var hashCodesField = context.Context.InitConstField(Type, 0, hashCodes);
            var valuesField = context.Context.InitConstField(Type, 1, values);

            var il = context.Il;
            context.LoadField(valuesField); // stack: [values]
            context.LoadObj(); // stack: [values, obj]
            il.Ldc_I4(values.Length); // stack: [values, obj, values.Length]
            il.Rem(typeof(uint)); // stack: [values, obj % values.Length]
            il.Ldelem(typeof(int)); // stack: [values[obj % values.Length]]
            context.LoadObj(); // stack: [values[obj % values.Length], obj]
            il.Ceq(); // stack: [values[obj % values.Length] == obj]
            var writeIntLabel = il.DefineLabel("writeInt");
            il.Brfalse(writeIntLabel); // if(values[obj % values.Length] != obj) goto writeInt

            context.LoadField(hashCodesField); // stack: [hashCodes]
            context.LoadObj(); // stack: [hashCodes, obj]
            il.Ldc_I4(hashCodes.Length); // stack: [hashCodes, obj, hashCodes.Length]
            il.Rem(typeof(uint)); // stack: [hashCodes, obj % hashCodes.Length]
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

        private KeyValuePair<ulong[], int[]> BuildHashCodesTable()
        {
            var values = (int[])Enum.GetValues(Type);
            var uniqueValues = new HashSet<int>(values).ToArray();
            var nameHashes = GroBufHelpers.CalcHashAndCheck(Enum.GetNames(Type));
            var hashSet = new HashSet<uint>();
            for (var x = (uint)values.Length; ; ++x)
            {
                hashSet.Clear();
                bool ok = true;
                foreach (var value in uniqueValues)
                {
                    var item = (uint)(value % x);
                    if (hashSet.Contains(item))
                    {
                        ok = false;
                        break;
                    }
                    hashSet.Add(item);
                }
                if (!ok) continue;
                var hashCodes = new ulong[x];
                var valuez = new int[x];
                for (int i = 0; i < x; ++i)
                    valuez[i] = -1;
                for (int i = 0; i < values.Length; i++)
                {
                    var value = values[i];
                    var index = (int)(value % x);
                    hashCodes[index] = nameHashes[i];
                    valuez[index] = value;
                }
                return new KeyValuePair<ulong[], int[]>(hashCodes, valuez);
            }
        }
    }
}