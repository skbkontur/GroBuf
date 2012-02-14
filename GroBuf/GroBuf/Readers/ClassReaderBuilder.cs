using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.Readers
{
    internal class ClassReaderBuilder<T> : ReaderBuilderBase<T>
    {
        protected override void ReadNotEmpty(ReaderMethodBuilderContext context)
        {
            MemberInfo[] dataMembers;
            ulong[] hashCodes;
            BuildMembersTable(context.Context, out hashCodes, out dataMembers);

            var il = context.Il;
            var end = context.Length;
            var typeCode = context.TypeCode;

            var setters = dataMembers.Select(member => member == null ? null : GetMemberSetter(context.Context, member)).ToArray();

            var settersField = context.Context.BuildConstField<IntPtr[]>("setters_" + Type.Name + "_" + Guid.NewGuid(), field => BuildSettersFieldInitializer(context.Context, field, setters));
            var hashCodesField = context.Context.BuildConstField("hashCodes_" + Type.Name + "_" + Guid.NewGuid(), hashCodes);

            context.IncreaseIndexBy1(); // index = index + 1
            context.AssertTypeCode(GroBufTypeCode.Object);

            il.Emit(OpCodes.Ldc_I4_4);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Emit(OpCodes.Ldind_U4); // stack: [(uint)data[index]]
            context.IncreaseIndexBy4(); // index = index + 4; stack: [(uint)data[index]]

            il.Emit(OpCodes.Dup); // stack: [(uint)data[index], (uint)data[index]]
            context.AssertLength(); // stack: [(uint)data[index]]

            context.LoadIndex(); // stack: [(uint)data[index], index]
            il.Emit(OpCodes.Add); // stack: [(uint)data[index] + index]
            il.Emit(OpCodes.Stloc, end); // end = (uint)data[index] + index

            var cycleStartLabel = il.DefineLabel();
            il.MarkLabel(cycleStartLabel);

            il.Emit(OpCodes.Ldc_I4, 9);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Emit(OpCodes.Ldind_I8); // stack: [*(int64*)&data[index] = hashCode]
            context.IncreaseIndexBy8(); // index = index + 8; stack: [*(int64*)&data[index] = hashCode]

            il.Emit(OpCodes.Dup); // stack: [hashCode, hashCode]
            il.Emit(OpCodes.Ldc_I8, (long)dataMembers.Length); // stack: [hashCode, hashCode, (int64)hashCodes.Length]
            il.Emit(OpCodes.Rem_Un); // stack: [hashCode, hashCode % hashCodes.Length]
            il.Emit(OpCodes.Conv_I4); // stack: [hashCode, (int)(hashCode % hashCodes.Length)]
            var idx = il.DeclareLocal(typeof(int));
            il.Emit(OpCodes.Stloc, idx); // idx = (int)(hashCode % hashCodes.Length); stack: [hashCode]

            context.LoadField(hashCodesField); // stack: [hashCode, hashCodes]
            il.Emit(OpCodes.Ldloc, idx); // stack: [hashCode, hashCodes, idx]
            il.Emit(OpCodes.Ldelem_I8); // stack: [hashCode, hashCodes[idx]]

            var skipDataLabel = il.DefineLabel();
            il.Emit(OpCodes.Bne_Un, skipDataLabel); // if(hashCode != hashCodes[idx]) goto skipData; stack: []

            // Read data
            context.LoadData(); // stack: [pinnedData]
            context.LoadIndexByRef(); // stack: [pinnedData, ref index]
            context.LoadDataLength(); // stack: [pinnedData, ref index, dataLength]
            context.LoadResultByRef(); // stack: [pinnedData, ref index, dataLength, ref result]

            context.LoadField(settersField); // stack: [pinnedData, ref index, dataLength, ref result, setters]
            context.Il.Emit(OpCodes.Ldloc, idx); // stack: [pinnedData, ref index, dataLength, ref result, setters, idx]
            context.Il.Emit(OpCodes.Ldelem_I); // stack: [pinnedData, ref index, dataLength, ref result, setters[idx]]
            var parameterTypes = new[] {typeof(byte*), typeof(int).MakeByRefType(), typeof(int), Type.MakeByRefType()};
            context.Il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(void), parameterTypes, null); // setters[idx](pinnedData, ref index, dataLength, ref result); stack: []

            var checkIndexLabel = il.DefineLabel();
            il.Emit(OpCodes.Br, checkIndexLabel); // goto checkIndex

            il.MarkLabel(skipDataLabel);
            // Skip data
            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Emit(OpCodes.Ldind_U1); // stack: [data[index]]
            il.Emit(OpCodes.Stloc, typeCode); // typeCode = data[index]; stack: []
            context.IncreaseIndexBy1(); // index = index + 1
            context.CheckTypeCode();
            context.SkipValue();

            il.MarkLabel(checkIndexLabel);

            context.LoadIndex(); // stack: [index]
            il.Emit(OpCodes.Ldloc, end); // stack: [index, end]
            il.Emit(OpCodes.Blt_Un, cycleStartLabel); // if(index < end) goto cycleStart; stack: []
        }

        private static Action BuildSettersFieldInitializer(ReaderTypeBuilderContext context, FieldInfo field, MethodInfo[] setters)
        {
            var typeBuilder = context.TypeBuilder;
            var method = typeBuilder.DefineMethod(field.Name + "_Init", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldnull); // stack: [null]
            il.Emit(OpCodes.Ldc_I4, setters.Length); // stack: [null, setters.Length]
            il.Emit(OpCodes.Newarr, typeof(IntPtr)); // stack: [null, new IntPtr[setters.Length]]
            il.Emit(OpCodes.Stfld, field); // settersField = new IntPtr[setters.Length]
            il.Emit(OpCodes.Ldnull); // stack: [null]
            il.Emit(OpCodes.Ldfld, field); // stack: [settersField]
            for(int i = 0; i < setters.Length; ++i)
            {
                if(setters[i] == null) continue;
                il.Emit(OpCodes.Dup); // stack: [settersField, settersField]
                il.Emit(OpCodes.Ldc_I4, i); // stack: [settersField, settersField, i]
                il.Emit(OpCodes.Ldftn, setters[i]); // stack: [settersField, settersField, i, setters[i]]
                il.Emit(OpCodes.Stelem_I); // settersField[i] = setters[i]; stack: [settersField]
            }
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);
            return () => typeBuilder.GetMethod(method.Name).Invoke(null, null);
        }

        private void BuildMembersTable(ReaderTypeBuilderContext context, out ulong[] hashCodes, out MemberInfo[] dataMembers)
        {
            var members = context.GetDataMembers(Type);
            var hashes = GroBufHelpers.CalcHashAndCheck(members.Select(member => member.Name));
            var hashSet = new HashSet<uint>();
            for(var x = (uint)members.Length;; ++x)
            {
                hashSet.Clear();
                bool ok = true;
                foreach(var hash in hashes)
                {
                    var item = (uint)(hash % x);
                    if(hashSet.Contains(item))
                    {
                        ok = false;
                        break;
                    }
                    hashSet.Add(item);
                }
                if(!ok) continue;
                hashCodes = new ulong[x];
                dataMembers = new MemberInfo[x];
                for(int i = 0; i < members.Length; i++)
                {
                    var index = (int)(hashes[i] % x);
                    hashCodes[index] = hashes[i];
                    dataMembers[index] = members[i];
                }
                return;
            }
        }

        private MethodInfo GetMemberSetter(ReaderTypeBuilderContext context, MemberInfo member)
        {
            var method = context.TypeBuilder.DefineMethod("Set_" + Type.Name + "_" + member.Name + "_" + Guid.NewGuid(), MethodAttributes.Public | MethodAttributes.Static, typeof(void),
                                                          new[]
                                                              {
                                                                  typeof(byte*), typeof(int).MakeByRefType(), typeof(int), Type.MakeByRefType()
                                                              });
            var il = method.GetILGenerator();

            if(Type.IsClass)
            {
                il.Emit(OpCodes.Ldarg_3); // stack: [ref result]
                il.Emit(OpCodes.Ldind_Ref); // stack: [result]
                var notNullLabel = il.DefineLabel();
                il.Emit(OpCodes.Brtrue, notNullLabel); // if(result != null) goto notNull; stack: []
                il.Emit(OpCodes.Ldarg_3); // stack: [ref result]
                var constructor = Type.GetConstructor(Type.EmptyTypes);
                if(constructor == null)
                    throw new MissingConstructorException(Type);
                il.Emit(OpCodes.Newobj, constructor); // stack: [ref result, new type()]
                il.Emit(OpCodes.Stind_Ref); // result = new type(); stack: []
                il.MarkLabel(notNullLabel);
            }

            il.Emit(OpCodes.Ldarg_0); // stack: [data]
            il.Emit(OpCodes.Ldarg_1); // stack: [data, ref index]
            il.Emit(OpCodes.Ldarg_2); // stack: [data, ref index, dataLength]
            il.Emit(OpCodes.Ldarg_3); // stack: [data, ref index, dataLength, ref result]
            if(Type.IsClass)
                il.Emit(OpCodes.Ldind_Ref); // stack: [data, ref index, dataLength, result]
            switch(member.MemberType)
            {
            case MemberTypes.Field:
                var field = (FieldInfo)member;
                il.Emit(OpCodes.Ldflda, field); // stack: [data, ref index, dataLength, ref result.field]
                il.Emit(OpCodes.Call, context.GetReader(field.FieldType)); // reader(data, ref index, dataLength, ref result.field); stack: []
                break;
            case MemberTypes.Property:
                var property = (PropertyInfo)member;
                var propertyValue = il.DeclareLocal(property.PropertyType);
                var getter = property.GetGetMethod();
                if(getter == null)
                    throw new MissingMethodException(Type.Name, property.Name + "_get");
                il.Emit(getter.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getter); // stack: [ data, ref index, dataLength, result.property]
                il.Emit(OpCodes.Stloc, propertyValue); // propertyValue = result.property; stack: [data, ref index, dataLength]
                il.Emit(OpCodes.Ldloca, propertyValue); // stack: [data, ref index, dataLength, ref propertyValue]
                il.Emit(OpCodes.Call, context.GetReader(property.PropertyType)); // reader(data, ref index, dataLength, ref propertyValue); stack: []
                il.Emit(OpCodes.Ldarg_3); // stack: [ref result]
                if(Type.IsClass)
                    il.Emit(OpCodes.Ldind_Ref); // stack: [result]
                il.Emit(OpCodes.Ldloc, propertyValue); // stack: [result, propertyValue]
                var setter = property.GetSetMethod();
                if(setter == null)
                    throw new MissingMethodException(Type.Name, property.Name + "_set");
                il.Emit(setter.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, setter); // result.property = propertyValue
                break;
            default:
                throw new NotSupportedException("Data member of type '" + member.MemberType + "' is not supported");
            }
            il.Emit(OpCodes.Ret);
            return method;
        }
    }
}