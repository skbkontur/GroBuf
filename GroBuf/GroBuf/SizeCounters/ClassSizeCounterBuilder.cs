using System;
using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.SizeCounters
{
    internal class ClassSizeCounterBuilder : SizeCounterBuilderBase
    {
        public ClassSizeCounterBuilder(Type type)
            : base(type)
        {
        }

        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;

            il.Emit(OpCodes.Ldc_I4_0); // stack: [0 = size]

            var dataMembers = context.Context.GetDataMembers(Type);
            foreach(var member in dataMembers)
            {
                if(Type.IsClass)
                    context.LoadObj(); // stack: [size, obj]
                else
                    context.LoadObjByRef(); // stack: [size, ref obj]
                Type memberType;
                switch(member.MemberType)
                {
                case MemberTypes.Property:
                    var property = (PropertyInfo)member;
                    var getter = property.GetGetMethod();
                    if(getter == null)
                        throw new MissingMethodException(Type.Name, property.Name + "_get");
                    il.Emit(getter.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getter); // stack: [size, obj.prop]
                    memberType = property.PropertyType;
                    break;
                case MemberTypes.Field:
                    var field = (FieldInfo)member;
                    il.Emit(OpCodes.Ldfld, field); // stack: [size, obj.field]
                    memberType = field.FieldType;
                    break;
                default:
                    throw new NotSupportedException("Data member of type " + member.MemberType + " is not supported");
                }
                il.Emit(OpCodes.Ldc_I4_0); // stack: [size, obj.member, false]
                il.Emit(OpCodes.Call, context.Context.GetCounter(memberType)); // stack: [size, writers[i](obj.member, false) = memberSize]
                il.Emit(OpCodes.Dup); // stack: [size, memberSize, memberSize]
                var next = il.DefineLabel();
                il.Emit(OpCodes.Brfalse, next); // if(memberSize = 0) goto next; stack: [size, memberSize]

                il.Emit(OpCodes.Ldc_I4_8); // stack: [size, memberSize, 8]
                il.Emit(OpCodes.Add); // stack: [size, memberSize + 8]
                il.MarkLabel(next);
                il.Emit(OpCodes.Add); // stack: [size + curSize]
            }

            var countLength = il.DefineLabel();
            il.Emit(OpCodes.Dup); // stack: [size, size]
            il.Emit(OpCodes.Brtrue, countLength); // if(size != 0) goto countLength; stack: [size]
            il.Emit(OpCodes.Pop); // stack: []
            context.ReturnForNull();
            il.Emit(OpCodes.Ret);
            il.MarkLabel(countLength);
            il.Emit(OpCodes.Ldc_I4_5); // stack: [size, 5]
            il.Emit(OpCodes.Add); // stack: [size + 5]
        }
    }
}