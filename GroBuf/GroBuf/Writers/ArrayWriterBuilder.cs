using System;

using GrEmit;

namespace GroBuf.Writers
{
    internal class ArrayWriterBuilder : WriterBuilderBase
    {
        public ArrayWriterBuilder(Type type)
            : base(type)
        {
            if(Type != typeof(Array))
            {
                if(!Type.IsArray) throw new InvalidOperationException("An array expected but was '" + Type + "'");
                if(Type.GetArrayRank() != 1) throw new NotSupportedException("Arrays with rank greater than 1 are not supported");
                elementType = Type.GetElementType();
            }
            else elementType = typeof(object);
        }

        protected override bool CheckEmpty(WriterMethodBuilderContext context, GroboIL.Label notEmptyLabel)
        {
            var il = context.Il;
            var emptyLabel = il.DefineLabel("empty");
            context.LoadObj(); // stack: [obj]
            il.Brfalse(emptyLabel); // if(obj == null) goto empty;
            context.LoadObj(); // stack: [obj]
            il.Ldlen(); // stack: [obj.Length]
            il.Brtrue(notEmptyLabel); // if(obj.Length != 0) goto notEmpty;
            il.MarkLabel(emptyLabel);
            return true;
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            context.WriteTypeCode(GroBufTypeCode.Array);
            var doneLabel = il.DefineLabel("done");
            var length = il.DeclareLocal(typeof(int));
            context.LoadObj(); // stack: [obj]
            il.Ldlen(); // stack: [obj.Length]
            il.Stloc(length); // length = obj.Length
            context.LoadIndex(); // stack: [index]
            var start = context.LocalInt;
            il.Stloc(start); // start = index
            context.IncreaseIndexBy4(); // index = index + 4
            context.GoToCurrentLocation(); // stack: [&result[index]]
            il.Ldloc(length); // stack: [&result[index], length]
            il.Stind(typeof(int)); // *(int*)&result[index] = length; stack: []
            context.IncreaseIndexBy4(); // index = index + 4

            var i = il.DeclareLocal(typeof(int));
            il.Ldc_I4(0); // stack: [0]
            il.Stloc(i); // i = 0; stack: []
            var cycleStart = il.DefineLabel("cycleStart");
            il.MarkLabel(cycleStart);
            context.LoadObj(); // stack: [obj]
            il.Ldloc(i); // stack: [obj, i]
            il.Ldelem(elementType);
            il.Ldc_I4(1); // stack: [obj[i], true]
            context.LoadResult(); // stack: [obj[i], true, result]
            context.LoadIndexByRef(); // stack: [obj[i], true, result, ref index]
            il.Call(context.Context.GetWriter(elementType)); // writer(obj[i], true, result, ref index); stack: []
            il.Ldloc(length); // stack: [length]
            il.Ldloc(i); // stack: [length, i]
            il.Ldc_I4(1); // stack: [length, i, 1]
            il.Add(); // stack: [length, i + 1]
            il.Dup(); // stack: [length, i + 1, i + 1]
            il.Stloc(i); // i = i + 1; stack: [length, i]
            il.Bgt(typeof(int), cycleStart); // if(length > i) goto cycleStart; stack: []

            context.LoadResult(); // stack: [result]
            il.Ldloc(start); // stack: [result, start]
            il.Add(); // stack: [result + start]
            context.LoadIndex(); // stack: [result + start, index]
            il.Ldloc(start); // stack: [result + start, index, start]
            il.Sub(); // stack: [result + start, index - start]
            il.Ldc_I4(4); // stack: [result + start, index - start, 4]
            il.Sub(); // stack: [result + start, index - start - 4]
            il.Stind(typeof(int)); // *(int*)(result + start) = index - start - 4

            il.MarkLabel(doneLabel);
        }

        private readonly Type elementType;
    }
}