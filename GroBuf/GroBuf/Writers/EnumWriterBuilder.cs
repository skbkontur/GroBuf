using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace GroBuf.Writers
{
    internal class EnumWriterBuilder : WriterBuilderBase
    {
        public EnumWriterBuilder(Type type)
            : base(type)
        {
            if(!Type.IsEnum) throw new InvalidOperationException("Enum expected but was " + Type);
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var hashCodes = BuildHashCodesTable();
            var hashCodesField = context.Context.BuildConstField("hashCodes_" + Type.Name + "_" + Guid.NewGuid(), hashCodes);

            context.LoadField(hashCodesField); // stack: [hashCodes]
            context.LoadObj(); // stack: [hashCodes, obj]
            context.Il.Emit(OpCodes.Ldc_I4, hashCodes.Length); // stack: [hashCodes, obj, hashCodes.Length]
            context.Il.Emit(OpCodes.Rem_Un); // stack: [hashCodes, obj % hashCodes.Length]
            context.Il.Emit(OpCodes.Ldelem_I8); // stack: [hashCodes[obj % hashCodes.Length] = hashCode]
            var hashCode = context.Il.DeclareLocal(typeof(ulong));
            context.Il.Emit(OpCodes.Dup); // stack: [hashCode, hashCode]
            context.Il.Emit(OpCodes.Stloc, hashCode); // hashCode = hashCodes[obj % hashCodes.Length]; stack: [hashCode]
            var writeIntLabel = context.Il.DefineLabel();
            context.Il.Emit(OpCodes.Brfalse, writeIntLabel);
            context.WriteTypeCode(GroBufTypeCode.Enum);
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.Il.Emit(OpCodes.Ldloc, hashCode); // stack: [&result[index], hashCode]
            context.Il.Emit(OpCodes.Stind_I8); // *(int64*)&result[index] = hashCode
            context.IncreaseIndexBy8(); // index = index + 8;
            context.Il.Emit(OpCodes.Ret);
            context.Il.MarkLabel(writeIntLabel);
            context.WriteTypeCode(GroBufTypeCode.Int32);
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObj(); // stack: [&result[index], obj]
            context.Il.Emit(OpCodes.Stind_I4); // result[index] = obj
            context.IncreaseIndexBy4(); // index = index + 4
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