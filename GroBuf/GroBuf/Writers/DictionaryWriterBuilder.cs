using System;
using System.Collections.Generic;
using System.Reflection;

using GrEmit;

namespace GroBuf.Writers
{
    internal class DictionaryWriterBuilder : WriterBuilderBase
    {
        public DictionaryWriterBuilder(Type type)
            : base(type)
        {
            if(!(Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                throw new InvalidOperationException("Dictionary expected but was '" + Type + "'");
            keyType = Type.GetGenericArguments()[0];
            valueType = Type.GetGenericArguments()[1];
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
                context.Il.Ldfld(Type.GetField("count", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.Count]
                context.Il.Brtrue(notEmptyLabel); // if(obj.Count != 0) goto notEmpty;
                context.Il.MarkLabel(emptyLabel);
            }
            return true;
        }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            context.BuildConstants(keyType);
            context.BuildConstants(valueType);
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            context.WriteTypeCode(GroBufTypeCode.Dictionary);
            context.LoadIndex(); // stack: [index]
            var start = context.LocalInt;
            il.Stloc(start); // start = index
            context.IncreaseIndexBy4(); // index = index + 4
            context.GoToCurrentLocation(); // stack: [&result[index]]
            context.LoadObj(); // stack: [&result[index], obj]
            il.Ldfld(Type.GetField("count", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [&result[index], obj.Count]
            il.Dup(); // stack: [&result[index], obj.Count, obj.Count]
            var count = il.DeclareLocal(typeof(int));
            il.Stloc(count); // count = obj.Count; stack: [&result[index], obj.Count]
            il.Stind(typeof(int)); // *(int*)&result[index] = count; stack: []
            context.IncreaseIndexBy4(); // index = index + 4; stack: []

            var writeDataLengthLabel = il.DefineLabel("writeDataLength");
            il.Ldloc(count); // stack: [count]
            il.Brfalse(writeDataLengthLabel); // if(count == 0) goto writeDataLength; stack: []

            context.LoadObj(); // stack: [obj]
            var entryType = Type.GetNestedType("Entry", BindingFlags.NonPublic).MakeGenericType(Type.GetGenericArguments());
            var entries = il.DeclareLocal(entryType.MakeArrayType());
            il.Ldfld(Type.GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic)); // stack: [obj.entries]
            il.Stloc(entries); // entries = obj.entries; stack: []

            var i = il.DeclareLocal(typeof(int));
            il.Ldc_I4(0); // stack: [9, 0]
            il.Stloc(i); // i = 0; stack: [9]
            var cycleStartLabel = il.DefineLabel("cycleStart");
            il.MarkLabel(cycleStartLabel);
            il.Ldloc(entries); // stack: [size, entries]
            il.Ldloc(i); // stack: [size, entries, i]
            il.Ldelema(entryType); // stack: [size, &entries[i]]
            il.Dup(); // stack: [size, &entries[i], &entries[i]]
            var entry = il.DeclareLocal(entryType.MakeByRefType());
            il.Stloc(entry); // entry = &entries[i]; stack: [size, entry]
            il.Ldfld(entryType.GetField("hashCode")); // stack: [size, entry.hashCode]
            il.Ldc_I4(0); // stack: [size, entry.hashCode, 0]
            var nextLabel = il.DefineLabel("next");
            il.Blt(typeof(int), nextLabel); // if(entry.hashCode < 0) goto next; stack: [size]
            
            context.LoadWriter(keyType);

            il.Ldloc(entry); // stack: [size, entry]
            il.Ldfld(entryType.GetField("key")); // stack: [size, entry.key]
            il.Ldc_I4(1);
            context.LoadResult(); // stack: [obj[i], true, result]
            context.LoadIndexByRef();
            context.CallWriter(keyType);

            context.LoadWriter(valueType);
            
            il.Ldloc(entry); // stack: [size, entry]
            il.Ldfld(entryType.GetField("value")); // stack: [size, entry.value]
            il.Ldc_I4(1);
            context.LoadResult(); // stack: [obj[i], true, result]
            context.LoadIndexByRef();
            context.CallWriter(valueType);

            il.MarkLabel(nextLabel);
            il.Ldloc(count); // stack: [size, count]
            il.Ldloc(i); // stack: [size, count, i]
            il.Ldc_I4(1); // stack: [size, count, i, 1]
            il.Add(); // stack: [size, count, i + 1]
            il.Dup(); // stack: [size, count, i + 1, i + 1]
            il.Stloc(i); // i = i + 1; stack: [size, count, i]
            il.Bgt(typeof(int), cycleStartLabel); // if(count > i) goto cycleStart; stack: [size]

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

        private readonly Type keyType;
        private readonly Type valueType;
    }
}