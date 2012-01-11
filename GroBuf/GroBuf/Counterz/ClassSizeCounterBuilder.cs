using System;
using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.Counterz
{
    internal class ClassSizeCounterBuilder<T> : SizeCounterBuilderBase<T>
    {
        protected override void CountSizeNotEmpty(SizeCounterMethodBuilderContext context)
        {
            var il = context.Il;

            il.Emit(OpCodes.Ldc_I4_0);

            var dataMembers = context.Context.GetDataMembers(Type);
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
                    if(getter == null)
                        throw new MissingMethodException(Type.Name, property.Name + "_get");
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
                il.Emit(OpCodes.Call, context.Context.GetCounter(memberType)); // writers[i](obj.prop, false)
                il.Emit(OpCodes.Dup); // stack: [sum, cur, cur]
                var next = il.DefineLabel();
                il.Emit(OpCodes.Brfalse, next);

                il.Emit(OpCodes.Ldc_I4_8);
                il.Emit(OpCodes.Add);
                il.MarkLabel(next);
                il.Emit(OpCodes.Add);
            }

            var countLength = il.DefineLabel();
            il.Emit(OpCodes.Dup); // stack: [sum, sum]
            il.Emit(OpCodes.Brtrue, countLength);
            il.Emit(OpCodes.Pop);
            context.ReturnForNull();
            il.Emit(OpCodes.Ret);
            il.MarkLabel(countLength);
            il.Emit(OpCodes.Ldc_I4_5);
            il.Emit(OpCodes.Add);
        }
    }
}