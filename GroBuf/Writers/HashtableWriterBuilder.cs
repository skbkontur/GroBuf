using System.Collections;
using System.Reflection;

using GrEmit;

namespace GroBuf.Writers
{
    internal class HashtableWriterBuilder : WriterBuilderBase
    {
        public HashtableWriterBuilder()
            : base(typeof(Hashtable))
        {
        }

        protected override bool CheckEmpty(WriterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            context.LoadObj(); // stack: [obj]
            if(context.Context.GroBufWriter.Options.HasFlag(GroBufOptions.WriteEmptyObjects))
                context.Il.Brtrue(notEmptyLabel); // if(obj != null) goto notEmpty;
            else
            {
                var emptyLabel = context.Il.DefineLabel("empty");
                context.Il.Brfalse(emptyLabel); // if(obj == null) goto empty;
                context.LoadObj(); // stack: [obj]
                context.Il.Ldfld(Type.GetField(PlatformHelpers.HashtableCountFieldName, BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.Count]
                context.Il.Brtrue(notEmptyLabel); // if(obj.Count != 0) goto notEmpty;
                context.Il.MarkLabel(emptyLabel);
            }
            return true;
        }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            context.BuildConstants(typeof(object));
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            context.WriteTypeCode(GroBufTypeCode.Dictionary);
            context.LoadIndex(); // stack: [index]
            var start = context.LocalInt;
            il.Stloc(start); // start = index
            il.Ldc_I4(8); // data length + dict size = 8
            context.AssertLength();
            context.IncreaseIndexBy4(); // index = index + 4
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObj(); // stack: [&result[index], obj]
            il.Ldfld(Type.GetField(PlatformHelpers.HashtableCountFieldName, BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [&result[index], obj.Count]
            il.Dup();
            var count = il.DeclareLocal(typeof(int));
            il.Stloc(count); // count = obj.Count; stack: [&result[index], obj.Count]
            il.Stind(typeof(int)); // *(int*)&result[index] = count; stack: []
            context.IncreaseIndexBy4(); // index = index + 4; stack: []

            var writeDataLengthLabel = il.DefineLabel("writeDataLength");
            il.Ldloc(count); // stack: [count]
            il.Brfalse(writeDataLengthLabel); // if(count == 0) goto writeDataLength; stack: []

            context.LoadObj(); // stack: [obj]
            var bucketType = Type.GetNestedType("bucket", BindingFlags.NonPublic);
            var buckets = il.DeclareLocal(bucketType.MakeArrayType());
            var bucketsField = Type.GetField(PlatformHelpers.HashtableBucketsFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            il.Ldfld(bucketsField); // stack: [obj.buckets]
            il.Stloc(buckets); // buckets = obj.buckets; stack: []
            il.Ldloc(buckets); // stack: [buckets]
            il.Ldlen(); // stack: [buckets.Length]
            il.Stloc(count); // count = buckets.Length; stack: []

            var i = il.DeclareLocal(typeof(int));
            il.Ldc_I4(0); // stack: [0]
            il.Stloc(i); // i = 0; stack: []
            var cycleStartLabel = il.DefineLabel("cycleStart");
            il.MarkLabel(cycleStartLabel);
            il.Ldloc(buckets); // stack: [buckets]
            il.Ldloc(i); // stack: [buckets, i]
            il.Ldelema(bucketType); // stack: [&buckets[i]]
            il.Dup(); // stack: [&buckets[i], &buckets[i]]
            var bucket = il.DeclareLocal(bucketType.MakeByRefType());
            il.Stloc(bucket); // bucket = &buckets[i]; stack: [bucket]
            il.Ldfld(bucketType.GetField("key")); // stack: [bucket.key]
            il.Dup(); // stack: [bucket.key, bucket.key]
            var key = il.DeclareLocal(typeof(object));
            il.Stloc(key); // key = bucket.key; stack: [key]

            var nextLabel = il.DefineLabel("next");
            il.Brfalse(nextLabel); // if(bucket.key == null) goto next; stack: []
            il.Ldloc(key); // stack: [key]
            context.LoadObj(); // stack: [key, obj]
            il.Ldfld(bucketsField); // stack: [key, obj.buckets]
            il.Beq(nextLabel); // if(key == obj.buckets) goto next; stack: []

            il.Ldloc(key); // stack: [obj[i].key]
            il.Ldc_I4(1); // stack: [obj[i].key, true]
            context.LoadResult(); // stack: [obj[i].key, true, result]
            context.LoadIndexByRef(); // stack: [obj[i].key, true, result, ref index]
            context.LoadContext(); // stack: [obj[i].key, true, result, ref index, context]
            context.CallWriter(typeof(object)); // write<object>(obj[i].key, true, result, ref index, context); stack: []

            il.Ldloc(bucket); // stack: [bucket]
            il.Ldfld(bucketType.GetField("val")); // stack: [bucket.val]
            il.Ldc_I4(1); // stack: [obj[i].value, true]
            context.LoadResult(); // stack: [obj[i].value, true, result]
            context.LoadIndexByRef(); // stack: [obj[i].value, true, result, ref index]
            context.LoadContext(); // stack: [obj[i].value, true, result, ref index, context]
            context.CallWriter(typeof(object)); // writer<object>(obj[i].value, true, result, ref index, context); stack: []

            il.MarkLabel(nextLabel);
            il.Ldloc(count); // stack: [ count]
            il.Ldloc(i); // stack: [count, i]
            il.Ldc_I4(1); // stack: [count, i, 1]
            il.Add(); // stack: [count, i + 1]
            il.Dup(); // stack: [count, i + 1, i + 1]
            il.Stloc(i); // i = i + 1; stack: [count, i]
            il.Bgt(cycleStartLabel, false); // if(count > i) goto cycleStart; stack: []

            il.MarkLabel(writeDataLengthLabel);
            context.LoadResult(); // stack: [result]
            il.Ldloc(start); // stack: [result, start]
            il.Add(); // stack: [result + start]
            context.LoadIndex(); // stack: [result + start, index]
            il.Ldloc(start); // stack: [result + start, index, start]
            il.Sub(); // stack: [result + start, index - start]
            il.Ldc_I4(4); // stack: [result + start, index - start, 4]
            il.Sub(); // stack: [result + start, index - start - 4]
            il.Stind(typeof(int)); // *(int*)(result + start) = index - start - 4
        }

        protected override bool IsReference { get { return true; } }
    }
}