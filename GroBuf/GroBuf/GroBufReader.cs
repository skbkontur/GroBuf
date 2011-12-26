using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf
{
    public class GroBufReader
    {
        // TODO: enum, derived types, decimal
        public T Read<T>(byte[] data)
        {
            int index = 0;
            return Read<T>(data, ref index);
        }

        private unsafe delegate T ReaderDelegate<out T>(byte* pinnedData, ref int index);

        private delegate T PinningReaderDelegate<out T>(byte[] data, ref int index);

        private delegate T InternalPinningReaderDelegate<out T>(Delegate readerDelegate, byte[] data, ref int index);

        private unsafe delegate T InternalReaderDelegate<out T>(Delegate[] readersDelegates, ulong[] hashCodes, byte* pinnedData, ref int index);

        private unsafe delegate void ClassPropertySetterDelegate<in T>(T obj, byte* pinnedData, ref int index);

        private unsafe delegate void InternalClassPropertySetterDelegate<in T>(Delegate readerDelegate, T obj, byte* pinnedData, ref int index);

        private unsafe delegate void StructPropertySetterDelegate<T>(ref T obj, byte* pinnedData, ref int index);

        private unsafe delegate void InternalStructPropertySetterDelegate<T>(Delegate readerDelegate, ref T obj, byte* pinnedData, ref int index);

        private T Read<T>(byte[] data, ref int index)
        {
            return GetPinningReader<T>()(data, ref index);
        }

        private PinningReaderDelegate<T> GetPinningReader<T>()
        {
            var type = typeof(T);
            var pinningReader = (PinningReaderDelegate<T>)pinnedReaders[type];
            if(pinningReader == null)
            {
                lock(pinningReadersLock)
                {
                    pinningReader = (PinningReaderDelegate<T>)pinnedReaders[type];
                    if(pinningReader == null)
                    {
                        pinningReader = BuildPinningReader<T>();
                        pinnedReaders[type] = pinningReader;
                    }
                }
            }
            return pinningReader;
        }

        private unsafe ReaderDelegate<T> GetReader<T>()
        {
            var type = typeof(T);
            var reader = (ReaderDelegate<T>)readers[type];
            if(reader == null)
            {
                lock(readersLock)
                {
                    reader = (ReaderDelegate<T>)readers[type];
                    if(reader == null)
                    {
                        reader = BuildReader<T>();
                        readers[type] = reader;
                    }
                }
            }
            return reader;
        }

        private PinningReaderDelegate<T> BuildPinningReader<T>()
        {
            var type = typeof(T);
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), type, new[] {typeof(Delegate), typeof(byte[]), typeof(int).MakeByRefType()}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            var pinnedData = il.DeclareLocal(typeof(byte).MakeByRefType(), true);
            il.Emit(OpCodes.Ldarg_1); // stack: [data]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [data, 0]
            il.Emit(OpCodes.Ldelema, typeof(byte)); // stack: [&data[0]]
            il.Emit(OpCodes.Stloc, pinnedData); // pinnedData = &data[0]; stack: []
            var readerDelegate = GetReaderDelegate(type, this);
            il.Emit(OpCodes.Ldarg_0); // stack: [readerDelegate]
            il.Emit(OpCodes.Ldloc, pinnedData); // stack: [readerDelegate, pinnedData]
            il.Emit(OpCodes.Ldarg_2); // stack: [readerDelegate, pinnedData, ref index]
            il.Emit(OpCodes.Call, readerDelegate.GetType().GetMethod("Invoke")); // reader.Read<T>(pinnedData, ref index); stack: [result]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [result, 0]
            il.Emit(OpCodes.Conv_U); // stack: [result, (U)0]
            il.Emit(OpCodes.Stloc, pinnedData); // pinnedData = null; stack: [result]
            il.Emit(OpCodes.Ret);

            var pinningReader = (InternalPinningReaderDelegate<T>)dynamicMethod.CreateDelegate(typeof(InternalPinningReaderDelegate<T>));
            return (byte[] data, ref int index) => pinningReader(readerDelegate, data, ref index);
        }

        private unsafe ClassPropertySetterDelegate<T> BuildClassPropertySetter<T>(PropertyInfo property)
        {
            var type = typeof(T);
            if(!type.IsClass) throw new InvalidOperationException("Attempt to build class property setter for a value type " + type);
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void),
                                                  new[] {typeof(Delegate), type, typeof(byte).MakePointerType(), typeof(int).MakeByRefType()}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1); // stack: [obj]
            il.Emit(OpCodes.Ldarg_0); // stack: [obj, reader]
            il.Emit(OpCodes.Ldarg_2); // stack: [obj, reader, pinnedData]
            il.Emit(OpCodes.Ldarg_3); // stack: [obj, reader, pinnedData, ref index]
            var readerDelegate = GetReaderDelegate(property.PropertyType, this);
            il.Emit(OpCodes.Call, readerDelegate.GetType().GetMethod("Invoke")); // stack: [obj, reader.Read(pinnedData, ref index)]
            il.Emit(OpCodes.Callvirt, property.GetSetMethod()); // obj.Property = reader.Read(pinnedData, ref index)
            il.Emit(OpCodes.Ret);
            var propertySetter = (InternalClassPropertySetterDelegate<T>)dynamicMethod.CreateDelegate(typeof(InternalClassPropertySetterDelegate<T>));
            return (T obj, byte* pinnedData, ref int index) => propertySetter(readerDelegate, obj, pinnedData, ref index);
        }

        private unsafe StructPropertySetterDelegate<T> BuildStructPropertySetter<T>(PropertyInfo property)
        {
            var type = typeof(T);
            if(type.IsClass) throw new InvalidOperationException("Attempt to build struct property setter for a class type " + type);
            if(type.IsPrimitive) throw new InvalidOperationException("Attempt to build struct property setter for a primitive type " + type);
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void),
                                                  new[] {typeof(Delegate), type.MakeByRefType(), typeof(byte).MakePointerType(), typeof(int).MakeByRefType()}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1); // stack: [ref obj]
            il.Emit(OpCodes.Ldarg_0); // stack: [ref obj, reader]
            il.Emit(OpCodes.Ldarg_2); // stack: [ref obj, reader, pinnedData]
            il.Emit(OpCodes.Ldarg_3); // stack: [ref obj, reader, pinnedData, ref index]
            var readerDelegate = GetReaderDelegate(property.PropertyType, this);
            il.Emit(OpCodes.Call, readerDelegate.GetType().GetMethod("Invoke")); // stack: [ref obj, reader.Read(pinnedData, ref index)]
            il.Emit(OpCodes.Callvirt, property.GetSetMethod()); // obj.Property = reader.Read(pinnedData, ref index)
            il.Emit(OpCodes.Ret);
            var propertySetter = (InternalStructPropertySetterDelegate<T>)dynamicMethod.CreateDelegate(typeof(InternalStructPropertySetterDelegate<T>));
            return (ref T obj, byte* pinnedData, ref int index) => propertySetter(readerDelegate, ref obj, pinnedData, ref index);
        }

        private unsafe Delegate GetPropertySetter<T>(PropertyInfo property)
        {
            return typeof(T).IsClass ? (Delegate)BuildClassPropertySetter<T>(property) : BuildStructPropertySetter<T>(property);
        }

        private unsafe ReaderDelegate<T> BuildReader<T>()
        {
            var type = typeof(T);
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), type,
                                                  new[] {typeof(Delegate[]), typeof(ulong[]), typeof(byte).MakePointerType(), typeof(int).MakeByRefType()}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            var notNullLabel = il.DefineLabel();

            EmitGoToCurrentLocation(il); // stack: [&data[index]]
            il.Emit(OpCodes.Ldind_I4); // stack: [length]
            il.Emit(OpCodes.Dup); // stack: [length, length]
            var length = il.DeclareLocal(typeof(int));
            il.Emit(OpCodes.Stloc, length); // stack: [length]
            il.Emit(OpCodes.Brtrue, notNullLabel); // if(length != 0) goto notNull
            var result = il.DeclareLocal(type);
            EmitIncreaseIndexBy4(il); // index = index + 4
            il.Emit(OpCodes.Ldloca, result); // stack: [&result]
            il.Emit(OpCodes.Initobj, type); // result = default(T)
            il.Emit(OpCodes.Ldloc, result); // stack: [result]
            il.Emit(OpCodes.Ret);

            il.MarkLabel(notNullLabel);
            Delegate[] delegates = null;
            ulong[] hashCodes = null;
            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                EmitNullableValueReader(type, il, out delegates);
            else if(type == typeof(DateTime))
                EmitDateTimeValueReader(type, il, out delegates);
            else
            {
                EmitIncreaseIndexBy4(il); // index = index + 4
                if(type == typeof(string))
                    EmitStringValueReader(il, length);
                else if(type == typeof(Guid))
                    EmitGuidValueReader(il, result);
                else if(type.IsPrimitive)
                    EmitPrimitiveValueReader(type, il);
                else if(type.IsEnum)
                {
                    // TODO
                }
                else
                {
                    if(!type.IsArray)
                    {
                        PropertyInfo[] properties;
                        BuildPropertiesTable(type, out hashCodes, out properties);
                        EmitPropertiesReader<T>(il, length, properties, out delegates);
                    }
                    else
                    {
                        EmitGoToCurrentLocation(il); // stack: [&data[index]]
                        il.Emit(OpCodes.Ldind_I4); // stack: [array length]
                        EmitIncreaseIndexBy4(il); // index = index + 4; stack: [array length]
                        il.Emit(OpCodes.Stloc, length); // length = array length; stack: []
                        EmitArrayReader(type, il, length, out delegates);
                    }
                }
            }
            il.Emit(OpCodes.Ret);

            var reader = (InternalReaderDelegate<T>)dynamicMethod.CreateDelegate(typeof(InternalReaderDelegate<T>));
            return (byte* pinnedData, ref int index) => reader(delegates, hashCodes, pinnedData, ref index);
        }

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

        private void EmitArrayReader(Type type, ILGenerator il, LocalBuilder length, out Delegate[] delegates)
        {
            il.Emit(OpCodes.Ldloc, length); // stack: [length]
            var elementType = type.GetElementType();
            il.Emit(OpCodes.Newarr, elementType); // stack: [new type[length] = result]
            il.Emit(OpCodes.Ldloc, length); // stack: [result, length]
            var allDoneLabel = il.DefineLabel();
            il.Emit(OpCodes.Brfalse, allDoneLabel); // if(length == 0) goto allDone; stack: [result]
            var i = il.DeclareLocal(typeof(int));
            il.Emit(OpCodes.Ldc_I4_0); // stack: [result, 0]
            il.Emit(OpCodes.Stloc, i); // i = 0; stack: [result]
            var cycleStart = il.DefineLabel();
            il.MarkLabel(cycleStart);
            il.Emit(OpCodes.Dup); // stack: [result, result]
            il.Emit(OpCodes.Ldloc, i); // stack: [result, result, i]
            var readerDelegate = GetReaderDelegate(elementType, this);
            delegates = new[] {readerDelegate};

            if(elementType.IsValueType && !elementType.IsPrimitive) // struct
                il.Emit(OpCodes.Ldelema, elementType);

            il.Emit(OpCodes.Ldarg_0); // stack: [result, {result[i]}, delegates]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [result, {result[i]}, delegates, 0]
            il.Emit(OpCodes.Ldelem_Ref); // stack: [result, {result[i]}, delegates[0]]
            il.Emit(OpCodes.Ldarg_2); // stack: [result, {result[i]}, delegates[0], pinnedData]
            il.Emit(OpCodes.Ldarg_3); // stack: [result, {result[i]}, delegates[0], pinnedData, ref index]
            il.Emit(OpCodes.Call, readerDelegate.GetType().GetMethod("Invoke")); // reader.Read<elementType>(pinnedData, ref index); stack: [result, {result[i]}, item]
            EmitArrayItemSetter(elementType, il); // result[i] = item; stack: [result]
            il.Emit(OpCodes.Ldloc, i); // stack: [result, i]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [result, i, 1]
            il.Emit(OpCodes.Add); // stack: [result, i + 1]
            il.Emit(OpCodes.Dup); // stack: [result, i + 1, i + 1]
            il.Emit(OpCodes.Stloc, i); // i = i + 1; stack: [result, i]
            il.Emit(OpCodes.Ldloc, length); // stack: [result, i, length]
            il.Emit(OpCodes.Blt, cycleStart); // if(i < length) goto cycleStart
            il.MarkLabel(allDoneLabel); // stack: [result]
        }

        private static void EmitArrayItemSetter(Type elementType, ILGenerator il)
        {
            if(elementType.IsClass) // class
                il.Emit(OpCodes.Stelem_Ref);
            else if(!elementType.IsPrimitive) // struct
                il.Emit(OpCodes.Stobj, elementType);
            else
            {
                // Primitive
                switch(Type.GetTypeCode(elementType))
                {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                    il.Emit(OpCodes.Stelem_I1);
                    break;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    il.Emit(OpCodes.Stelem_I2);
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    il.Emit(OpCodes.Stelem_I4);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    il.Emit(OpCodes.Stelem_I8);
                    break;
                case TypeCode.Single:
                    il.Emit(OpCodes.Stelem_R4);
                    break;
                case TypeCode.Double:
                    il.Emit(OpCodes.Stelem_R8);
                    break;
                default:
                    throw new NotSupportedException();
                }
            }
        }

        private void EmitPropertiesReader<T>(ILGenerator il, LocalBuilder end, PropertyInfo[] properties, out Delegate[] delegates)
        {
            var type = typeof(T);
            delegates = properties.Select(property => property == null ? null : GetPropertySetter<T>(property)).ToArray();
            il.Emit(OpCodes.Ldloc, end); // stack: [end]
            il.Emit(OpCodes.Ldarg_3); // stack: [end, ref index]
            il.Emit(OpCodes.Ldind_I4); // stack: [end, index]
            il.Emit(OpCodes.Add); // stack: [end + index]
            il.Emit(OpCodes.Stloc, end); // end = end + index
            var result = il.DeclareLocal(type);
            if(type.IsClass)
            {
                il.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes)); // stack: [new type()]
                il.Emit(OpCodes.Dup); // stack: [new type(), new type()]
                il.Emit(OpCodes.Stloc, result); // result = new type(); stack: [result]
            }
            else
            {
                il.Emit(OpCodes.Ldloca, result); // stack: [ref result]
                il.Emit(OpCodes.Initobj, type); // result = default(type)
                il.Emit(OpCodes.Ldloc, result); // stack: [result]
            }
            var cycleStartLabel = il.DefineLabel();
            il.MarkLabel(cycleStartLabel);

            EmitGoToCurrentLocation(il); // stack: [result, &data[index]]
            il.Emit(OpCodes.Ldind_I4); // stack: [result, data[index] = valueLength]
            il.Emit(OpCodes.Ldarg_3); // stack: [result, valueLength, ref index]
            il.Emit(OpCodes.Ldind_I4); // stack: [result, valueLength, index]
            il.Emit(OpCodes.Add); // stack: [result, valueLength + index]
            il.Emit(OpCodes.Ldc_I4_4); // stack: [result, valueLength + index, 4]
            il.Emit(OpCodes.Add); // stack: [result, valueLength + index + 4]
            var next = il.DeclareLocal(typeof(int));
            il.Emit(OpCodes.Dup); // stack: [result, valueLength + index + 4, valueLength + index + 4]
            il.Emit(OpCodes.Ldc_I4_8); // stack: [result, valueLength + index + 4, valueLength + index + 4, 8]
            il.Emit(OpCodes.Add); // stack: [result, valueLength + index + 4, valueLength + index + 4 + 8]
            il.Emit(OpCodes.Stloc, next); // next = valueLength + index + 4 + 8; stack: [result, valueLength + index + 4]

            il.Emit(OpCodes.Ldarg_2); // stack: [result, valueLength + index + 4, pinnedData]
            il.Emit(OpCodes.Add); // stack: [result, &data[valueLength + index + 4]]
            il.Emit(OpCodes.Ldind_I8); // stack: [result, data[valueLength + index + 4] = hashCode]
            il.Emit(OpCodes.Dup); // stack: [result, hashCode, hashCode]
            il.Emit(OpCodes.Ldc_I4, properties.Length); // stack: [result, hashCode, hashCode, hashCodes.Length]
            il.Emit(OpCodes.Conv_U); // stack: [result, hashCode, hashCode, (U)hashCodes.Length]
            il.Emit(OpCodes.Rem_Un); // stack: [result, hashCode, hashCode % hashCodes.Length]
            il.Emit(OpCodes.Conv_I4); // stack: [result, hashCode, (int)(hashCode % hashCodes.Length)]
            var idx = il.DeclareLocal(typeof(int));
            il.Emit(OpCodes.Stloc, idx); // idx = (int)(hashCode % hashCodes.Length); stack: [result, hashCode]
            il.Emit(OpCodes.Ldarg_1); // stack: [result, hashCode, hashCodes]
            il.Emit(OpCodes.Ldloc, idx); // stack: [result, hashCode, hashCodes, idx]
            il.Emit(OpCodes.Ldelem_I8); // stack: [result, hashCode, hashCodes[idx]]
            var moveIndexLabel = il.DefineLabel();
            il.Emit(OpCodes.Bne_Un, moveIndexLabel); // if(hashCode != hashCodes[idx]) goto moveIndex; stack: [result]

            il.Emit(OpCodes.Ldarg_0); // stack: [result, setters]
            il.Emit(OpCodes.Ldloc, idx); // stack: [result, setters, idx]
            il.Emit(OpCodes.Ldelem_Ref); // stack: [result, setters[idx]]
            il.Emit(type.IsClass ? OpCodes.Ldloc : OpCodes.Ldloca, result); // stack: [result, setters[idx], {result}]
            il.Emit(OpCodes.Ldarg_2); // stack: [result, setters[idx], {result}, pinnedData]
            il.Emit(OpCodes.Ldarg_3); // stack: [result, setters[idx], {result}, pinnedData, ref index]
            var invoke = type.IsClass ? typeof(ClassPropertySetterDelegate<T>).GetMethod("Invoke") : typeof(StructPropertySetterDelegate<T>).GetMethod("Invoke");
            il.Emit(OpCodes.Call, invoke); // setters[idx]({result}, pinnedData, ref index); stack: [result]

            il.MarkLabel(moveIndexLabel);
            il.Emit(OpCodes.Ldarg_3); // stack: [result, ref index]
            il.Emit(OpCodes.Ldloc, next); // stack: [result, ref index, next]
            il.Emit(OpCodes.Stind_I4); // index = next; stack: [result]
            il.Emit(OpCodes.Ldloc, next); // stack: [result, next]
            il.Emit(OpCodes.Ldloc, end); // stack: [result, next, end]
            il.Emit(OpCodes.Blt, cycleStartLabel); // if(next < end) goto cycleStart; stack: [result]
        }

        private static void EmitIncreaseIndexBy8(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_3); // stack: [ref index]
            il.Emit(OpCodes.Dup); // stack: [ref index, ref index]
            il.Emit(OpCodes.Ldind_I4); // stack: [ref index, index]
            il.Emit(OpCodes.Ldc_I4_8); // stack: [ref index, index, 8]
            il.Emit(OpCodes.Add); // stack: [ref index, index + 8]
            il.Emit(OpCodes.Stind_I4); // index = index + 8
        }

        private static void EmitIncreaseIndexBy4(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_3); // stack: [ref index]
            il.Emit(OpCodes.Dup); // stack: [ref index, ref index]
            il.Emit(OpCodes.Ldind_I4); // stack: [ref index, index]
            il.Emit(OpCodes.Ldc_I4_4); // stack: [ref index, index, 4]
            il.Emit(OpCodes.Add); // stack: [ref index, index + 4]
            il.Emit(OpCodes.Stind_I4); // index = index + 4
        }

        private static void EmitIncreaseIndexBy1(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_3); // stack: [ref index]
            il.Emit(OpCodes.Dup); // stack: [ref index, ref index]
            il.Emit(OpCodes.Ldind_I4); // stack: [ref index, index]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [ref index, index, 1]
            il.Emit(OpCodes.Add); // stack: [ref index, index + 1]
            il.Emit(OpCodes.Stind_I4); // index = index + 1
        }

        private static void EmitIncreaseIndexBy2(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_3); // stack: [ref index]
            il.Emit(OpCodes.Dup); // stack: [ref index, ref index]
            il.Emit(OpCodes.Ldind_I4); // stack: [ref index, index]
            il.Emit(OpCodes.Ldc_I4_2); // stack: [ref index, index, 2]
            il.Emit(OpCodes.Add); // stack: [ref index, index + 2]
            il.Emit(OpCodes.Stind_I4); // index = index + 2
        }

        private static void EmitGoToCurrentLocation(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_2); // stack: [pinnedData]
            il.Emit(OpCodes.Ldarg_3); // stack: [pinnedData, ref index]
            il.Emit(OpCodes.Ldind_I4); // stack: [pinnedData, index]
            il.Emit(OpCodes.Add); // stack: [pinnedData + index]
        }

        private void EmitDateTimeValueReader(Type type, ILGenerator il, out Delegate[] delegates)
        {
            il.Emit(OpCodes.Ldarg_0); // stack: [delegates]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [delegates, 0]
            il.Emit(OpCodes.Ldelem_Ref); // stack: [delegates[0]]
            il.Emit(OpCodes.Ldarg_2); // stack: [delegates[0], pinnedData]
            il.Emit(OpCodes.Ldarg_3); // stack: [delegates[0], pinnedData, ref index]
            var readerDelegate = GetReaderDelegate(typeof(long), this);
            delegates = new[] {readerDelegate};
            il.Emit(OpCodes.Call, readerDelegate.GetType().GetMethod("Invoke")); // stack: [reader<long>(pinnedData, ref index)]
            il.Emit(OpCodes.Ldc_I4, (int)DateTimeKind.Utc);
            il.Emit(OpCodes.Newobj, type.GetConstructor(new[] {typeof(long), typeof(DateTimeKind)})); // stack: new DateTime(reader<long>(pinnedData, ref index), DateTimeKind.Utc)
        }

        private void EmitNullableValueReader(Type type, ILGenerator il, out Delegate[] delegates)
        {
            il.Emit(OpCodes.Ldarg_0); // stack: [delegates]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [delegates, 0]
            il.Emit(OpCodes.Ldelem_Ref); // stack: [delegates[0]]
            il.Emit(OpCodes.Ldarg_2); // stack: [delegates[0], pinnedData]
            il.Emit(OpCodes.Ldarg_3); // stack: [delegates[0], pinnedData, ref index]
            var elementType = type.GetGenericArguments()[0];
            var readerDelegate = GetReaderDelegate(elementType, this);
            delegates = new[] {readerDelegate};
            il.Emit(OpCodes.Call, readerDelegate.GetType().GetMethod("Invoke")); // stack: [reader<elementType>(pinnedData, ref index)]
            il.Emit(OpCodes.Newobj, type.GetConstructor(new[] {elementType})); // stack: new type(reader<elementType>(pinnedData, ref index))
        }

        private static void EmitPrimitiveValueReader(Type type, ILGenerator il)
        {
            EmitGoToCurrentLocation(il); // stack: [&data[index]]
            switch(Type.GetTypeCode(type))
            {
            case TypeCode.Boolean:
            case TypeCode.SByte:
                il.Emit(OpCodes.Ldind_I1);
                EmitIncreaseIndexBy1(il);
                break;
            case TypeCode.Byte:
                il.Emit(OpCodes.Ldind_U1);
                EmitIncreaseIndexBy1(il);
                break;
            case TypeCode.Int16:
                il.Emit(OpCodes.Ldind_I2);
                EmitIncreaseIndexBy2(il);
                break;
            case TypeCode.Int32:
                il.Emit(OpCodes.Ldind_I4);
                EmitIncreaseIndexBy4(il);
                break;
            case TypeCode.Int64:
            case TypeCode.UInt64:
                il.Emit(OpCodes.Ldind_I8);
                EmitIncreaseIndexBy8(il);
                break;
            case TypeCode.UInt16:
                il.Emit(OpCodes.Ldind_U2);
                EmitIncreaseIndexBy2(il);
                break;
            case TypeCode.UInt32:
                il.Emit(OpCodes.Ldind_U4);
                EmitIncreaseIndexBy4(il);
                break;
            case TypeCode.Single:
                il.Emit(OpCodes.Ldind_R4);
                EmitIncreaseIndexBy4(il);
                break;
            case TypeCode.Double:
                il.Emit(OpCodes.Ldind_R8);
                EmitIncreaseIndexBy8(il);
                break;
            default:
                throw new NotSupportedException();
            }
        }

        private static void EmitGuidValueReader(ILGenerator il, LocalBuilder result)
        {
            il.Emit(OpCodes.Ldloca, result); // stack: [&result]
            il.Emit(OpCodes.Dup); // stack: [&result, &result]
            EmitGoToCurrentLocation(il); // stack: [&result, &result, &data[index]]
            il.Emit(OpCodes.Ldind_I8); // stack: [&result, &result, (int64)data[index]]
            il.Emit(OpCodes.Stind_I8); // *result = (int64)data[index]; stack: [&result]
            EmitIncreaseIndexBy8(il); // index = index + 8
            il.Emit(OpCodes.Ldc_I4_8); // stack: [&result, 8]
            il.Emit(OpCodes.Add); // stack: [&result + 8]
            EmitGoToCurrentLocation(il); // stack: [&result + 8, &data[index]]
            il.Emit(OpCodes.Ldind_I8); // stack: [&result + 8, (int64)data[index]]
            il.Emit(OpCodes.Stind_I8); // *(&result + 8) = (int64)data[index]; stack: []
            il.Emit(OpCodes.Ldloc, result); // stack: [result]
        }

        private static void EmitStringValueReader(ILGenerator il, LocalBuilder length)
        {
            EmitGoToCurrentLocation(il); // stack: [&data[index]]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [&data[index], 0]
            il.Emit(OpCodes.Ldloc, length); // stack: [&data[index], 0, length]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [&data[index], 0, length, 1]
            il.Emit(OpCodes.Shr); // stack: [&data[index], 0, length >> 1]
            il.Emit(OpCodes.Newobj, typeof(string).GetConstructor(new[] {typeof(char*), typeof(int), typeof(int)})); // stack: [new string(&data[index], 0, length >> 1)]
            il.Emit(OpCodes.Ldarg_3); // stack: [new string(&data[index], 0, length >> 1), ref index]
            il.Emit(OpCodes.Dup); // stack: [new string(&data[index], 0, length >> 1), ref index, ref index]
            il.Emit(OpCodes.Ldind_I4); // stack: [new string(&data[index], 0, length >> 1), ref index, index]
            il.Emit(OpCodes.Ldloc, length); // stack: [new string(&data[index], 0, length >> 1), ref index, index, length]
            il.Emit(OpCodes.Add); // stack: [new string(&data[index], 0, length >> 1), ref index, index + length]
            il.Emit(OpCodes.Stind_I4); // index = index + length; stack: [new string(&data[index], 0, length >> 1)]
        }

        private static unsafe Delegate GetReaderDelegate(Type type, GroBufReader reader)
        {
            if(getReaderMethod == null)
                getReaderMethod = ((MethodCallExpression)((Expression<Action<GroBufReader>>)(grawReader => grawReader.GetReader<int>())).Body).Method.GetGenericMethodDefinition();
            return ((Delegate)getReaderMethod.MakeGenericMethod(new[] {type}).Invoke(reader, new object[0]));
        }

        private static MethodInfo getReaderMethod;

        private readonly Hashtable readers = new Hashtable();
        private readonly object readersLock = new object();

        private readonly Hashtable pinnedReaders = new Hashtable();
        private readonly object pinningReadersLock = new object();
    }
}