using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Writers
{
    internal class ClassWriterBuilder<T> : WriterBuilderBase<T>
    {
        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            var length = context.LocalInt;
            var start = il.DeclareLocal(typeof(int));
            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Emit(OpCodes.Dup); // stack: [ref index, index, index]
            il.Emit(OpCodes.Stloc, start); // start = index; stack: [ref index, index]
            il.Emit(OpCodes.Ldc_I4_5); // stack: [ref index, index, 5]
            il.Emit(OpCodes.Add); // stack: [ref index, index + 5]
            il.Emit(OpCodes.Stind_I4); // index = index + 5; stack: []

            var dataMembers = context.Context.GetDataMembers(Type);
            var prev = il.DeclareLocal(typeof(int));
            foreach(var member in dataMembers)
            {
                if(Type.IsClass)
                    context.LoadObj(); // stack: [obj]
                else
                    context.LoadObjByRef(); // stack: [ref obj]
                Type memberType;
                switch(member.MemberType)
                {
                case MemberTypes.Property:
                    var property = (PropertyInfo)member;
                    var getter = property.GetGetMethod();
                    il.Emit(getter.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getter); // stack: [obj.prop]
                    memberType = property.PropertyType;
                    break;
                case MemberTypes.Field:
                    var field = (FieldInfo)member;
                    il.Emit(OpCodes.Ldfld, field); // stack: [obj.field]
                    memberType = field.FieldType;
                    break;
                default:
                    throw new NotSupportedException("Data member of type " + member.MemberType + " is not supported");
                }
                il.Emit(OpCodes.Ldc_I4_0); // stack: [obj.prop, false]
                context.LoadResultByRef(); // stack: [obj.prop, false, ref result]
                context.LoadIndexByRef(); // stack: [obj.prop, false, ref result, ref index]
                il.Emit(OpCodes.Dup); // stack: [obj.prop, false, ref result, ref index, ref index]
                context.LoadIndex(); // stack: [obj.prop, false, ref result, ref index, ref index, index]
                il.Emit(OpCodes.Dup); // stack: [obj.prop, false, ref result, ref index, ref index, index, index]
                il.Emit(OpCodes.Stloc, prev); // prev = index; stack: [obj.prop, false, ref result, ref index, ref index, index]
                il.Emit(OpCodes.Ldc_I4_8); // stack: [obj.prop, false, ref result, ref index, ref index, index, 8]
                il.Emit(OpCodes.Add); // stack: [obj.prop, false, ref result, ref index, ref index, index + 8]
                il.Emit(OpCodes.Stind_I4); // index = index + 8; stack: [obj.prop, false, ref result, ref index]
                context.LoadPinnedResultByRef(); // stack: [obj.prop, false, ref result, ref index, ref pinnedResult]
                il.Emit(OpCodes.Call, context.Context.GetWriter(memberType)); // writers[i](obj.prop, false, ref result, ref index, ref pinnedResult)
                context.LoadIndex(); // stack: [index]
                il.Emit(OpCodes.Ldc_I4_8); // stack: [index, 8]
                il.Emit(OpCodes.Sub); // stack: [index - 8]
                il.Emit(OpCodes.Ldloc, prev); // stack: [index - 8, prev]
                var writeHashCodeLabel = il.DefineLabel();
                il.Emit(OpCodes.Bgt, writeHashCodeLabel); // if(index - 8 > prev) goto writeHashCode;
                context.LoadIndexByRef(); // stack: [ref index]
                il.Emit(OpCodes.Ldloc, prev); // stack: [ref index, prev]
                il.Emit(OpCodes.Stind_I4); // index = prev;
                var next = il.DefineLabel();
                il.Emit(OpCodes.Br, next); // goto next;

                il.MarkLabel(writeHashCodeLabel);

                context.LoadPinnedResult(); // stack: [pinnedResult]
                il.Emit(OpCodes.Ldloc, prev); // stack: [pinnedResult, prev]
                il.Emit(OpCodes.Add); // stack: [pinnedResult + prev]
                il.Emit(OpCodes.Ldc_I8, (long)GroBufHelpers.CalcHash(member.Name)); // stack: [&result[index], prop.Name.HashCode]
                il.Emit(OpCodes.Stind_I8); // *(long*)(pinnedResult + prev) = prop.Name.HashCode; stack: []

                il.MarkLabel(next);
            }

            context.LoadIndex(); // stack: [index]
            il.Emit(OpCodes.Ldloc, start); // stack: [index, start]
            il.Emit(OpCodes.Sub); // stack: [index - start]
            il.Emit(OpCodes.Ldc_I4_5); // stack: [index - start, 5]
            il.Emit(OpCodes.Sub); // stack: [index - start - 5]

            var writeLengthLabel = il.DefineLabel();
            var allDoneLabel = il.DefineLabel();
            il.Emit(OpCodes.Dup); // stack: [index - start - 5, index - start - 5]
            il.Emit(OpCodes.Stloc, length); // length = index - start - 5; stack: [length]
            il.Emit(OpCodes.Brtrue, writeLengthLabel); // if(length != 0) goto writeLength;

            context.LoadIndexByRef(); // stack: [ref index]
            il.Emit(OpCodes.Ldloc, start); // stack: [ref index, start]
            il.Emit(OpCodes.Stind_I4); // index = start
            context.WriteNull();

            il.MarkLabel(writeLengthLabel);

            context.LoadPinnedResult(); // stack: [pinnedResult]
            il.Emit(OpCodes.Ldloc, start); // stack: [pinnedResult, start]
            il.Emit(OpCodes.Add); // stack: [pinnedResult + start]
            il.Emit(OpCodes.Dup); // stack: [pinnedResult + start, pinnedResult + start]
            il.Emit(OpCodes.Ldc_I4, (int)GroBufTypeCode.Object); // stack: [pinnedResult + start, pinnedResult + start, TypeCode.Object]
            il.Emit(OpCodes.Stind_I1); // *(pinnedResult + start) = TypeCode.Object; stack: [pinnedResult + start]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [pinnedResult + start, 1]
            il.Emit(OpCodes.Add); // stack: [pinnedResult + start + 1]
            il.Emit(OpCodes.Ldloc, length); // stack: [pinnedResult + start + 1, length]
            il.Emit(OpCodes.Stind_I4); // *(int*)(pinnedResult + start + 1) = length
            il.MarkLabel(allDoneLabel);
        }
    }
}