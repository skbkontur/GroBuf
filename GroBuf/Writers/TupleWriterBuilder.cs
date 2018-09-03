using System;

namespace GroBuf.Writers
{
    internal class TupleWriterBuilder : WriterBuilderBase
    {
        public TupleWriterBuilder(Type type)
            : base(type)
        {
            if (!Type.IsTuple())
                throw new InvalidOperationException("A tuple expected but was '" + Type + "'");
        }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            foreach (var argumentType in Type.GetGenericArguments())
                context.BuildConstants(argumentType);
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            context.WriteTypeCode(GroBufTypeCode.Tuple);
            var start = il.DeclareLocal(typeof(int));
            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Dup(); // stack: [ref index, index, index]
            il.Stloc(start); // start = index; stack: [ref index, index]
            il.Ldc_I4(4); // stack: [ref index, index, 4]
            il.Add(); // stack: [ref index, index + 4]
            il.Stind(typeof(int)); // index = index + 4; stack: []

            var genericArguments = Type.GetGenericArguments();
            for (int i = 0; i < genericArguments.Length; ++i)
            {
                var property = Type.GetProperty(i == 7 ? "Rest" : "Item" + (i + 1));
                var getter = property.GetGetMethod();
                context.LoadObj(); // stack: [obj]
                il.Call(getter); // stack: [obj.Item{i}]
                il.Ldc_I4(1); // stack: [obj.Item{i}, true]
                context.LoadResult(); // stack: [obj.Item{i}, true, result]
                context.LoadIndexByRef(); // stack: [obj.Item{i}, true, result, ref index]
                context.LoadContext(); // stack: [obj.Item{i}, true, result, ref index, context]
                context.CallWriter(genericArguments[i]); // writer<i>(obj.Item{i}, true, result, ref index, context); stack: []
            }

            context.LoadResult(); // stack: [result]
            il.Ldloc(start); // stack: [result, start]
            il.Add(); // stack: [result[start]]

            context.LoadIndex(); // stack: [result[start], index]
            il.Ldloc(start); // stack: [result[start], index, start]
            il.Sub(); // stack: [result[start], index - start]
            il.Ldc_I4(5); // stack: [result[start], index - start, 4]
            il.Sub(); // stack: [result[start], index - start - 4 => length]
            il.Stind(typeof(int)); // result[start] = length; stack: []
        }

        protected override bool IsReference => true;
    }
}