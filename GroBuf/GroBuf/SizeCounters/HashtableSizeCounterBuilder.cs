using System.Collections;
using System.Reflection;

using GrEmit;

namespace GroBuf.SizeCounters
{
    internal class HashtableSizeCounterBuilder : SizeCounterBuilderBase
    {
        public HashtableSizeCounterBuilder()
            : base(typeof(Hashtable))
        {
        }

        protected override void BuildConstantsInternal(SizeCounterConstantsBuilderContext context)
        {
            context.BuildConstants(typeof(object));
        }

        protected override bool CheckEmpty(SizeCounterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            context.LoadObj(); // stack: [obj]
            if(context.Context.GroBufWriter.Options.HasFlag(GroBufOptions.WriteEmptyObjects))
                context.Il.Brtrue(notEmptyLabel); // if(obj != null) goto notEmpty;
            else
            {
                var emptyLabel = context.Il.DefineLabel("empty");
                context.Il.Brfalse(emptyLabel); // if(obj == null) goto empty;
                context.LoadObj(); // stack: [obj]
                context.Il.Ldfld(Type.GetField("count", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.Count]
                context.Il.Brtrue(notEmptyLabel); // if(obj.Count != 0) goto notEmpty;
                context.Il.MarkLabel(emptyLabel);
            }
            return true;
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;
            il.Ldc_I4(9); // stack: [9 = size] 9 = type code + data length + dictionary count
            context.LoadObj(); // stack: [size, obj]
            il.Ldfld(Type.GetField("count", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [size, obj.Count]

            var doneLabel = il.DefineLabel("done");
            il.Brfalse(doneLabel); // if(count == 0) goto done; stack: [size]

            context.LoadObj(); // stack: [size, obj]
            var bucketType = Type.GetNestedType("bucket", BindingFlags.NonPublic);
            var buckets = il.DeclareLocal(bucketType.MakeArrayType());
            var bucketsField = Type.GetField("buckets", BindingFlags.Instance | BindingFlags.NonPublic);
            il.Ldfld(bucketsField); // stack: [size, obj.buckets]
            il.Stloc(buckets); // buckets = obj.buckets; stack: [size]
            var length = il.DeclareLocal(typeof(int));
            il.Ldloc(buckets); // stack: [size, buckets]
            il.Ldlen(); // stack: [size, buckets.Length]
            il.Stloc(length); // length = buckets.Length

            var i = il.DeclareLocal(typeof(int));
            il.Ldc_I4(0); // stack: [9, 0]
            il.Stloc(i); // i = 0; stack: [9]
            var cycleStartLabel = il.DefineLabel("cycleStart");
            il.MarkLabel(cycleStartLabel);
            il.Ldloc(buckets); // stack: [size, buckets]
            il.Ldloc(i); // stack: [size, buckets, i]
            il.Ldelema(bucketType); // stack: [size, &buckets[i]]
            il.Dup(); // stack: [size, &entries[i], &buckets[i]]
            var bucket = il.DeclareLocal(bucketType.MakeByRefType());
            il.Stloc(bucket); // bucket = &buckets[i]; stack: [size, bucket]
            il.Ldfld(bucketType.GetField("key", BindingFlags.Public | BindingFlags.Instance)); // stack: [size, bucket.key]
            il.Dup(); // stack: [size, bucket.key, bucket.key]
            var key = il.DeclareLocal(typeof(object));
            il.Stloc(key); // key = bucket.key; stack: [size, key]

            var nextLabel = il.DefineLabel("next");
            il.Brfalse(nextLabel); // if(bucket.key == null) goto next; stack: [size]

            il.Ldloc(key); // stack: [size, key]
            context.LoadObj(); // stack: [size, key, obj]
            il.Ldfld(bucketsField); // stack: [size, key, obj.buckets]
            il.Beq(nextLabel); // if(key == obj.buckets) goto next; stack: [size]

            //            context.LoadSizeCounter(typeof(object));

            il.Ldloc(key); // stack: [size, key]
            il.Ldc_I4(1); // stack: [size, key, true]
            context.LoadContext(); // stack: [size, key, true, context]
            context.CallSizeCounter(typeof(object)); // stack: [size, writer(key, true, context) = keySize]
            il.Add(); // stack: [size + keySize]

            //            context.LoadSizeCounter(typeof(object));

            il.Ldloc(bucket); // stack: [size, bucket]
            il.Ldfld(bucketType.GetField("val", BindingFlags.Public | BindingFlags.Instance)); // stack: [size, bucket.val]
            il.Ldc_I4(1); // stack: [size, bucket.val, true]
            context.LoadContext(); // stack: [size, bucket.val, true, context]
            context.CallSizeCounter(typeof(object)); // stack: [size, writer(bucket.val, true, context) = valueSize]
            il.Add(); // stack: [size + valueSize]

            il.MarkLabel(nextLabel);
            il.Ldloc(length); // stack: [size, length]
            il.Ldloc(i); // stack: [size, length, i]
            il.Ldc_I4(1); // stack: [size, length, i, 1]
            il.Add(); // stack: [size, length, i + 1]
            il.Dup(); // stack: [size, length, i + 1, i + 1]
            il.Stloc(i); // i = i + 1; stack: [size, length, i]
            il.Bgt(cycleStartLabel, false); // if(length > i) goto cycleStart; stack: [size]

            il.MarkLabel(doneLabel);
        }

        protected override bool IsReference { get { return true; } }
    }
}