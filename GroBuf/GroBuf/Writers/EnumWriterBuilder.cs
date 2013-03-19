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
            context.SetFields(Type, new[] {new KeyValuePair<string, Type>("hashCodes_" + Type.Name + "_" + Guid.NewGuid(), typeof(ulong[]))});
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var hashCodes = BuildHashCodesTable();
            var hashCodesField = context.Context.InitConstField(Type, 0, hashCodes);

            var il = context.Il;
            context.LoadField(hashCodesField); // stack: [hashCodes]
            context.LoadObj(); // stack: [hashCodes, obj]
            il.Ldc_I4(hashCodes.Length); // stack: [hashCodes, obj, hashCodes.Length]
            il.Rem(typeof(uint)); // stack: [hashCodes, obj % hashCodes.Length]
            il.Ldelem(typeof(long)); // stack: [hashCodes[obj % hashCodes.Length] = hashCode]
            var hashCode = il.DeclareLocal(typeof(ulong));
            il.Dup(); // stack: [hashCode, hashCode]
            il.Stloc(hashCode); // hashCode = hashCodes[obj % hashCodes.Length]; stack: [hashCode]
            var writeIntLabel = il.DefineLabel("writeInt");
            il.Brfalse(writeIntLabel);
            context.WriteTypeCode(GroBufTypeCode.Enum);
            context.GoToCurrentLocation(); // stack: [&result[index]]
            il.Ldloc(hashCode); // stack: [&result[index], hashCode]
            il.Stind(typeof(long)); // *(int64*)&result[index] = hashCode
            context.IncreaseIndexBy8(); // index = index + 8;
            il.Ret();
            il.MarkLabel(writeIntLabel);
            context.WriteTypeCode(GroBufTypeCode.Int32);
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObj(); // stack: [&result[index], obj]
            il.Stind(typeof(int)); // result[index] = obj
            context.IncreaseIndexBy4(); // index = index + 4
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