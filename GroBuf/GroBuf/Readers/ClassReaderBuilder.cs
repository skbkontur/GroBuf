using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Readers
{
    internal class ClassReaderBuilder<T> : ReaderBuilderWithTwoParams<T, Delegate[], ulong[]>
    {
        public ClassReaderBuilder(IReaderCollection readerCollection)
            : base(readerCollection)
        {
        }

        protected override Tuple<Delegate[], ulong[]> ReadNotEmpty(ReaderBuilderContext<T> context)
        {
            PropertyInfo[] properties;
            ulong[] hashCodes;
            BuildPropertiesTable(Type, out hashCodes, out properties);

            var il = context.Il;
            var end = context.Length;
            var result = context.Result;
            var typeCode = context.TypeCode;

            context.IncreaseIndexBy1();
            context.AssertTypeCode(GroBufTypeCode.Object);

            var type = typeof(T);
            var setters = properties.Select(property => property == null ? null : GetPropertySetter(property)).ToArray();

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

            if(type.IsClass)
            {
                il.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes)); // stack: [new type()]
                il.Emit(OpCodes.Stloc, result); // result = new type(); stack: []
            }
            else
            {
                il.Emit(OpCodes.Ldloca, result); // stack: [ref result]
                il.Emit(OpCodes.Initobj, type); // result = default(type)
            }
            var cycleStartLabel = il.DefineLabel();
            il.MarkLabel(cycleStartLabel);

            il.Emit(OpCodes.Ldc_I4, 9);
            context.AssertLength();

            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Emit(OpCodes.Ldind_I8); // stack: [*(int64*)&data[index] = hashCode]
            context.IncreaseIndexBy8(); // index = index + 8; stack: [*(int64*)&data[index] = hashCode]

            il.Emit(OpCodes.Dup); // stack: [hashCode, hashCode]
            il.Emit(OpCodes.Ldc_I4, properties.Length); // stack: [hashCode, hashCode, hashCodes.Length]
            il.Emit(OpCodes.Conv_U); // stack: [hashCode, hashCode, (U)hashCodes.Length]
            il.Emit(OpCodes.Rem_Un); // stack: [hashCode, hashCode % hashCodes.Length]
            il.Emit(OpCodes.Conv_I4); // stack: [hashCode, (int)(hashCode % hashCodes.Length)]
            var idx = il.DeclareLocal(typeof(int));
            il.Emit(OpCodes.Stloc, idx); // idx = (int)(hashCode % hashCodes.Length); stack: [hashCode]

            context.LoadAdditionalParam(1); // stack: [hashCode, hashCodes]
            il.Emit(OpCodes.Ldloc, idx); // stack: [hashCode, hashCodes, idx]
            il.Emit(OpCodes.Ldelem_I8); // stack: [hashCode, hashCodes[idx]]

            var readPropertyLabel = il.DefineLabel();
            il.Emit(OpCodes.Beq, readPropertyLabel); // if(hashCode == hashCodes[idx]) goto skipData; stack: []

            // Skip data
            context.GoToCurrentLocation(); // stack: [&data[index]]
            il.Emit(OpCodes.Ldind_U1); // stack: [data[index]]
            il.Emit(OpCodes.Stloc, typeCode); // typeCode = data[index]; stack: []
            context.IncreaseIndexBy1(); // index = index + 1
            context.CheckTypeCode();
            context.SkipValue();
            var checkIndexLabel = il.DefineLabel();
            il.Emit(OpCodes.Br, checkIndexLabel); // goto checkIndex

            il.MarkLabel(readPropertyLabel);
            // Read data
            context.LoadAdditionalParam(0); // stack: [setters]
            il.Emit(OpCodes.Ldloc, idx); // stack: [setters, idx]
            il.Emit(OpCodes.Ldelem_Ref); // stack: [setters[idx]]
            il.Emit(type.IsClass ? OpCodes.Ldloc : OpCodes.Ldloca, result); // stack: [setters[idx], {result}]
            context.LoadData(); // stack: [setters[idx], {result}, pinnedData]
            context.LoadIndexByRef(); // stack: [setters[idx], {result}, pinnedData, ref index]
            context.LoadDataLength(); // stack: [setters[idx], {result}, pinnedData, ref index, dataLength]
            var invoke = type.IsClass ? typeof(ClassPropertySetterDelegate).GetMethod("Invoke") : typeof(StructPropertySetterDelegate).GetMethod("Invoke");
            il.Emit(OpCodes.Call, invoke); // setters[idx]({result}, pinnedData, ref index, dataLength); stack: []

            il.MarkLabel(checkIndexLabel);

            context.LoadIndex(); // stack: [index]
            il.Emit(OpCodes.Ldloc, end); // stack: [index, end]
            il.Emit(OpCodes.Blt_Un, cycleStartLabel); // if(index < end) goto cycleStart; stack: []
            il.Emit(OpCodes.Ldloc, result); // stack: [result]

            return new Tuple<Delegate[], ulong[]>(setters, hashCodes);
        }

        private unsafe delegate void ClassPropertySetterDelegate(T obj, byte* pinnedData, ref int index, int dataLength);

        private unsafe delegate void InternalClassPropertySetterDelegate(Delegate readerDelegate, T obj, byte* pinnedData, ref int index, int dataLength);

        private unsafe delegate void StructPropertySetterDelegate(ref T obj, byte* pinnedData, ref int index, int dataLength);

        private unsafe delegate void InternalStructPropertySetterDelegate(Delegate readerDelegate, ref T obj, byte* pinnedData, ref int index, int dataLength);

        private static void BuildPropertiesTable(Type type, out ulong[] hashCodes, out PropertyInfo[] properties)
        {
            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(property => new Tuple<ulong, PropertyInfo>(GroBufHelpers.CalcHash(property.Name), property)).ToArray();
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

        private unsafe Delegate GetPropertySetter(PropertyInfo property)
        {
            return Type.IsClass ? (Delegate)BuildClassPropertySetter(property) : BuildStructPropertySetter(property);
        }

        private unsafe ClassPropertySetterDelegate BuildClassPropertySetter(PropertyInfo property)
        {
            if(!Type.IsClass) throw new InvalidOperationException("Attempt to build class property setter for a value type " + Type);
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void),
                                                  new[] {typeof(Delegate), Type, typeof(byte).MakePointerType(), typeof(int).MakeByRefType(), typeof(int)}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1); // stack: [obj]
            il.Emit(OpCodes.Ldarg_0); // stack: [obj, reader]
            il.Emit(OpCodes.Ldarg_2); // stack: [obj, reader, pinnedData]
            il.Emit(OpCodes.Ldarg_3); // stack: [obj, reader, pinnedData, ref index]
            il.Emit(OpCodes.Ldarg_S, 4); // stack: [obj, reader, pinnedData, ref index, dataLength]
            var reader = GetReader(property.PropertyType);
            il.Emit(OpCodes.Call, reader.GetType().GetMethod("Invoke")); // stack: [obj, reader.Read(pinnedData, ref index, dataLength)]
            il.Emit(OpCodes.Callvirt, property.GetSetMethod()); // obj.Property = reader.Read(pinnedData, ref index)
            il.Emit(OpCodes.Ret);
            var propertySetter = (InternalClassPropertySetterDelegate)dynamicMethod.CreateDelegate(typeof(InternalClassPropertySetterDelegate));
            return (T obj, byte* pinnedData, ref int index, int dataLength) => propertySetter(reader, obj, pinnedData, ref index, dataLength);
        }

        private unsafe StructPropertySetterDelegate BuildStructPropertySetter(PropertyInfo property)
        {
            if(Type.IsClass) throw new InvalidOperationException("Attempt to build struct property setter for a class type " + Type);
            if(Type.IsPrimitive) throw new InvalidOperationException("Attempt to build struct property setter for a primitive type " + Type);
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void),
                                                  new[] {typeof(Delegate), Type.MakeByRefType(), typeof(byte).MakePointerType(), typeof(int).MakeByRefType(), typeof(int)}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1); // stack: [ref obj]
            il.Emit(OpCodes.Ldarg_0); // stack: [ref obj, reader]
            il.Emit(OpCodes.Ldarg_2); // stack: [ref obj, reader, pinnedData]
            il.Emit(OpCodes.Ldarg_3); // stack: [ref obj, reader, pinnedData, ref index]
            il.Emit(OpCodes.Ldarg_S, 4); // stack: [obj, reader, pinnedData, ref index, dataLength]
            var reader = GetReader(property.PropertyType);
            il.Emit(OpCodes.Call, reader.GetType().GetMethod("Invoke")); // stack: [ref obj, reader.Read(pinnedData, ref index, dataLength)]
            il.Emit(OpCodes.Callvirt, property.GetSetMethod()); // obj.Property = reader.Read(pinnedData, ref index)
            il.Emit(OpCodes.Ret);
            var propertySetter = (InternalStructPropertySetterDelegate)dynamicMethod.CreateDelegate(typeof(InternalStructPropertySetterDelegate));
            return (ref T obj, byte* pinnedData, ref int index, int dataLength) => propertySetter(reader, ref obj, pinnedData, ref index, dataLength);
        }
    }
}