using System;
using System.Linq;
using System.Reflection;

namespace GroBuf.Writers
{
    internal class ClassWriterBuilder : WriterBuilderBase
    {
        public ClassWriterBuilder(Type type)
            : base(type)
        {
        }

        protected override void BuildConstantsInternal(WriterConstantsBuilderContext context)
        {
            foreach(var member in context.GetDataMembers(Type))
            {
                Type memberType;
                switch(member.Member.MemberType)
                {
                case MemberTypes.Property:
                    memberType = ((PropertyInfo)member.Member).PropertyType;
                    break;
                case MemberTypes.Field:
                    memberType = ((FieldInfo)member.Member).FieldType;
                    break;
                default:
                    throw new NotSupportedException("Data member of type " + member.Member.MemberType + " is not supported");
                }
                context.BuildConstants(memberType);
            }
        }

        protected override void WriteNotEmpty(WriterMethodBuilderContext context)
        {
            var il = context.Il;
            var length = context.LocalInt;
            var start = il.DeclareLocal(typeof(int));
            context.LoadIndexByRef(); // stack: [ref index]
            context.LoadIndex(); // stack: [ref index, index]
            il.Dup(); // stack: [ref index, index, index]
            il.Stloc(start); // start = index; stack: [ref index, index]
            il.Ldc_I4(5); // stack: [ref index, index, 5]
            il.Add(); // stack: [ref index, index + 5]
            il.Stind(typeof(int)); // index = index + 5; stack: []

            var dataMembers = context.Context.GetDataMembers(Type);
            var hashCodes = GroBufHelpers.CalcHashAndCheck(dataMembers.Select(member => member.Name));
            var prev = il.DeclareLocal(typeof(int));
            for(int i = 0; i < dataMembers.Length; i++)
            {
                var member = dataMembers[i];

//                context.LoadWriter(member.Member.GetMemberType());

                if(Type.IsValueType)
                    context.LoadObjByRef(); // stack: [ref obj]
                else
                    context.LoadObj(); // stack: [obj]
                Type memberType;
                switch(member.Member.MemberType)
                {
                case MemberTypes.Property:
                    var property = (PropertyInfo)member.Member;
                    var getter = property.GetGetMethod(true);
                    if(getter == null)
                        throw new MissingMethodException(Type.Name, property.Name + "_get");
                    il.Call(getter, Type); // stack: [obj.prop]
                    memberType = property.PropertyType;
                    break;
                case MemberTypes.Field:
                    var field = (FieldInfo)member.Member;
                    il.Ldfld(field); // stack: [obj.field]
                    memberType = field.FieldType;
                    break;
                default:
                    throw new NotSupportedException("Data member of type " + member.Member.MemberType + " is not supported");
                }
                il.Ldc_I4(0); // stack: [obj.prop, false]
                context.LoadResult(); // stack: [obj.prop, false, result]
                context.LoadIndexByRef(); // stack: [obj.prop, false, result, ref index]
                il.Dup(); // stack: [obj.prop, false, result, ref index, ref index]
                context.LoadIndex(); // stack: [obj.prop, false, result, ref index, ref index, index]
                il.Dup(); // stack: [obj.prop, false, result, ref index, ref index, index, index]
                il.Stloc(prev); // prev = index; stack: [obj.prop, false, result, ref index, ref index, index]
                il.Ldc_I4(8); // stack: [obj.prop, false, result, ref index, ref index, index, 8]
                il.Add(); // stack: [obj.prop, false, result, ref index, ref index, index + 8]
                il.Stind(typeof(int)); // index = index + 8; stack: [obj.prop, false, result, ref index]
                context.LoadResultLength(); // stack: [obj.prop, false, result, ref index, resultLength]
                context.CallWriter(memberType); // writers[i](obj.prop, false, result, ref index, ref result, resultLength)
                context.LoadIndex(); // stack: [index]
                il.Ldc_I4(8); // stack: [index, 8]
                il.Sub(); // stack: [index - 8]
                il.Ldloc(prev); // stack: [index - 8, prev]
                var writeHashCodeLabel = il.DefineLabel("writeHashCode");
                il.Bgt(typeof(int), writeHashCodeLabel); // if(index - 8 > prev) goto writeHashCode;
                context.LoadIndexByRef(); // stack: [ref index]
                il.Ldloc(prev); // stack: [ref index, prev]
                il.Stind(typeof(int)); // index = prev;
                var nextLabel = il.DefineLabel("next");
                il.Br(nextLabel); // goto next;

                il.MarkLabel(writeHashCodeLabel);

                context.LoadResult(); // stack: [result]
                il.Ldloc(prev); // stack: [result, prev]
                il.Add(); // stack: [result + prev]
                il.Ldc_I8((long)hashCodes[i]); // stack: [&result[index], prop.Name.HashCode]
                il.Stind(typeof(long)); // *(long*)(result + prev) = prop.Name.HashCode; stack: []

                il.MarkLabel(nextLabel);
            }

            context.LoadIndex(); // stack: [index]
            il.Ldloc(start); // stack: [index, start]
            il.Sub(); // stack: [index - start]
            il.Ldc_I4(5); // stack: [index - start, 5]
            il.Sub(); // stack: [index - start - 5]

            il.Stloc(length); // length = index - start - 5; stack: []

            if(!context.Context.GroBufWriter.Options.HasFlag(GroBufOptions.WriteEmptyObjects))
            {
                var writeLengthLabel = il.DefineLabel("writeLength");
                il.Ldloc(length); // stack: [length]
                il.Brtrue(writeLengthLabel); // if(length != 0) goto writeLength;

                context.LoadIndexByRef(); // stack: [ref index]
                il.Ldloc(start); // stack: [ref index, start]
                il.Stind(typeof(int)); // index = start
                context.WriteNull();

                il.MarkLabel(writeLengthLabel);
            }
            context.LoadResult(); // stack: [result]
            il.Ldloc(start); // stack: [result, start]
            il.Add(); // stack: [result + start]
            il.Dup(); // stack: [result + start, result + start]
            il.Ldc_I4((int)GroBufTypeCode.Object); // stack: [result + start, result + start, TypeCode.Object]
            il.Stind(typeof(byte)); // *(result + start) = TypeCode.Object; stack: [result + start]
            il.Ldc_I4(1); // stack: [result + start, 1]
            il.Add(); // stack: [result + start + 1]
            il.Ldloc(length); // stack: [result + start + 1, length]
            il.Stind(typeof(int)); // *(int*)(result + start + 1) = length
        }
    }
}