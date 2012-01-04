using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Readers
{
    internal class ClassReaderBuilder<T> : ReaderBuilderBase<T>
    {
        protected override void ReadNotEmpty(ReaderMethodBuilderContext<T> context)
        {
            PropertyInfo[] properties;
            ulong[] hashCodes;
            BuildPropertiesTable(out hashCodes, out properties);

            var il = context.Il;
            var end = context.Length;
            var result = context.Result;
            var typeCode = context.TypeCode;

            var setters = properties.Select(property => property == null ? null : GetPropertySetter(context.Context, property)).ToArray();

            var settersField = context.Context.BuildConstField<IntPtr[]>("setters_" + Type.Name + "_" + Guid.NewGuid(), field => BuildSettersFieldInitializer(context.Context, field, setters));
            var hashCodesField = context.Context.BuildConstField("hashCodes_" + Type.Name + "_" + Guid.NewGuid(), hashCodes);

            context.IncreaseIndexBy1(); // index = index + 1
            context.AssertTypeCode(GroBufTypeCode.Object);

            il.Emit(OpCodes.Ldc_I4_4);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&result[index]]
            il.Emit(OpCodes.Ldind_U4); // stack: [(uint)result[index]]
            context.IncreaseIndexBy4(); // index = index + 4; stack: [(uint)result[index]]

            il.Emit(OpCodes.Dup); // stack: [(uint)result[index], (uint)result[index]]
            context.AssertLength(); // stack: [(uint)result[index]]

            context.LoadIndex(); // stack: [(uint)result[index], index]
            il.Emit(OpCodes.Add); // stack: [(uint)result[index] + index]
            il.Emit(OpCodes.Stloc, end); // end = (uint)result[index] + index

            if(Type.IsClass)
            {
                il.Emit(OpCodes.Newobj, Type.GetConstructor(Type.EmptyTypes)); // stack: [new type()]
                il.Emit(OpCodes.Stloc, result); // result = new type(); stack: []
            }
            else
            {
                il.Emit(OpCodes.Ldloca, result); // stack: [ref result]
                il.Emit(OpCodes.Initobj, Type); // result = default(type)
            }
            var cycleStartLabel = il.DefineLabel();
            il.MarkLabel(cycleStartLabel);

            il.Emit(OpCodes.Ldc_I4, 9);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Emit(OpCodes.Ldind_I8); // stack: [*(int64*)&data[index] = hashCode]
            context.IncreaseIndexBy8(); // index = index + 8; stack: [*(int64*)&data[index] = hashCode]

            il.Emit(OpCodes.Dup); // stack: [hashCode, hashCode]
            il.Emit(OpCodes.Ldc_I8, (long)properties.Length); // stack: [hashCode, hashCode, (int64)hashCodes.Length]
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
            il.Emit(Type.IsClass ? OpCodes.Ldloc : OpCodes.Ldloca, result); // stack: [{result}]
            context.LoadData(); // stack: [{result}, pinnedData]
            context.LoadIndexByRef(); // stack: [{result}, pinnedData, ref index]
            context.LoadDataLength(); // stack: [{result}, pinnedData, ref index, dataLength]

            context.LoadField(settersField); // stack: [{result}, pinnedData, ref index, dataLength, setters]
            context.Il.Emit(OpCodes.Ldloc, idx); // stack: [{result}, pinnedData, ref index, dataLength, setters, idx]
            context.Il.Emit(OpCodes.Ldelem_I); // stack: [{result}, pinnedData, ref index, dataLength, setters[idx]]
            var parameterTypes = new[] {Type.IsClass ? Type : Type.MakeByRefType(), typeof(byte*), typeof(int).MakeByRefType(), typeof(int)};
            context.Il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(void), parameterTypes, null); // setters[idx]({result, pinnedData, ref index, dataLength}); stack: []

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
            il.Emit(OpCodes.Ldloc, result); // stack: [result]
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

        private void BuildPropertiesTable(out ulong[] hashCodes, out PropertyInfo[] properties)
        {
            var props = Type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(property => property.CanWrite).Select(property => new Tuple<ulong, PropertyInfo>(GroBufHelpers.CalcHash(property.Name), property)).ToArray();
            var hashSet = new HashSet<uint>();
            for(var x = (uint)props.Length;; ++x)
            {
                hashSet.Clear();
                bool ok = true;
                foreach(var t in props)
                {
                    var item = (uint)(t.Item1 % x);
                    if(hashSet.Contains(item))
                    {
                        ok = false;
                        break;
                    }
                    hashSet.Add(item);
                }
                if(!ok) continue;
                hashCodes = new ulong[x];
                properties = new PropertyInfo[x];
                foreach(var t in props)
                {
                    var index = (int)(t.Item1 % x);
                    hashCodes[index] = t.Item1;
                    properties[index] = t.Item2;
                }
                return;
            }
        }

        private MethodInfo GetPropertySetter(ReaderTypeBuilderContext context, PropertyInfo property)
        {
            var method = context.TypeBuilder.DefineMethod("Set_" + Type.Name + "_" + property.Name + "_" + Guid.NewGuid(), MethodAttributes.Public | MethodAttributes.Static, typeof(void),
                                                          new[]
                                                              {
                                                                  Type.IsClass ? Type : Type.MakeByRefType(),
                                                                  typeof(byte*), typeof(int).MakeByRefType(), typeof(int)
                                                              });
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // stack: [{obj}]
            il.Emit(OpCodes.Ldarg_1); // stack: [{obj}, pinnedData]
            il.Emit(OpCodes.Ldarg_2); // stack: [{obj}, pinnedData, ref index]
            il.Emit(OpCodes.Ldarg_3); // stack: [{obj}, pinnedData, ref index, dataLength]
            il.Emit(OpCodes.Call, context.GetReader(property.PropertyType)); // stack: [{obj}, reader(pinnedData, ref index, dataLength)]
            var setter = property.GetSetMethod();
            il.Emit(setter.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, setter); // obj.Property = reader(pinnedData, ref index)
            il.Emit(OpCodes.Ret);
            return method;
        }
    }
}