using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf
{
    internal class GroBufReaderOld
    {
        // TODO: enum, derived types, decimal
        public T Read<T>(byte[] data)
        {
            int index = 0;
            var result = Read<T>(data, ref index);
            if(index < data.Length)
                throw new DataCorruptedException("Encountered extra data");
            return result;
        }

        private unsafe delegate T ReaderDelegate<out T>(byte* pinnedData, ref int index, int dataLength);

        private delegate T PinningReaderDelegate<out T>(byte[] data, ref int index);

        private delegate T InternalPinningReaderDelegate<out T>(Delegate readerDelegate, byte[] data, ref int index);

        private unsafe delegate T InternalReaderDelegate<out T>(Delegate[] readersDelegates, ulong[] hashCodes, byte* pinnedData, ref int index, int dataLength);

        private unsafe delegate void ClassPropertySetterDelegate<in T>(T obj, byte* pinnedData, ref int index, int dataLength);

        private unsafe delegate void InternalClassPropertySetterDelegate<in T>(Delegate readerDelegate, T obj, byte* pinnedData, ref int index, int dataLength);

        private unsafe delegate void StructPropertySetterDelegate<T>(ref T obj, byte* pinnedData, ref int index, int dataLength);

        private unsafe delegate void InternalStructPropertySetterDelegate<T>(Delegate readerDelegate, ref T obj, byte* pinnedData, ref int index, int dataLength);

        private unsafe delegate T PrimitiveValueReaderDelegate<out T>(byte* buf);

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

        private Delegate[] GetPrimitiveReaders<T>()
        {
            var type = typeof(T);
            var primitiveReaders = (Delegate[])primitiveValueReaders[type];
            if(primitiveReaders == null)
            {
                lock(primitiveValueReadersLock)
                {
                    primitiveReaders = (Delegate[])primitiveValueReaders[type];
                    if(primitiveReaders == null)
                    {
                        primitiveReaders = BuildPrimitiveValueReaders<T>();
                        primitiveValueReaders[type] = primitiveReaders;
                    }
                }
            }
            return primitiveReaders;
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

        private unsafe Delegate[] BuildPrimitiveValueReaders<T>()
        {
            var result = new Delegate[256];
            var defaultReader = BuildDefaultValueReader<T>();
            for(int i = 0; i < 256; ++i)
                result[i] = defaultReader;
            foreach(var typeCode in new[]
                {
                    GroBufTypeCode.Int8, GroBufTypeCode.UInt8,
                    GroBufTypeCode.Int16, GroBufTypeCode.UInt16,
                    GroBufTypeCode.Int32, GroBufTypeCode.UInt32,
                    GroBufTypeCode.Int64, GroBufTypeCode.UInt64,
                    GroBufTypeCode.Single, GroBufTypeCode.Double
                })
                result[(int)typeCode] = BuildPrimitiveValueReader<T>(typeCode);
            return result;
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
            il.Emit(OpCodes.Ldarg_1); // stack: [readerDelegate, pinnedData, ref index, data]
            il.Emit(OpCodes.Ldlen); // stack: [readerDelegate, pinnedData, ref index, data.Length]
            il.Emit(OpCodes.Call, readerDelegate.GetType().GetMethod("Invoke")); // reader.Read<T>(pinnedData, ref index, data.Length); stack: [result]
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
                                                  new[] {typeof(Delegate), type, typeof(byte).MakePointerType(), typeof(int).MakeByRefType(), typeof(int)}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1); // stack: [obj]
            il.Emit(OpCodes.Ldarg_0); // stack: [obj, reader]
            il.Emit(OpCodes.Ldarg_2); // stack: [obj, reader, pinnedData]
            il.Emit(OpCodes.Ldarg_3); // stack: [obj, reader, pinnedData, ref index]
            il.Emit(OpCodes.Ldarg_S, 4); // stack: [obj, reader, pinnedData, ref index, dataLength]
            var readerDelegate = GetReaderDelegate(property.PropertyType, this);
            il.Emit(OpCodes.Call, readerDelegate.GetType().GetMethod("Invoke")); // stack: [obj, reader.Read(pinnedData, ref index, dataLength)]
            il.Emit(OpCodes.Callvirt, property.GetSetMethod()); // obj.Property = reader.Read(pinnedData, ref index)
            il.Emit(OpCodes.Ret);
            var propertySetter = (InternalClassPropertySetterDelegate<T>)dynamicMethod.CreateDelegate(typeof(InternalClassPropertySetterDelegate<T>));
            return (T obj, byte* pinnedData, ref int index, int dataLength) => propertySetter(readerDelegate, obj, pinnedData, ref index, dataLength);
        }

        private unsafe StructPropertySetterDelegate<T> BuildStructPropertySetter<T>(PropertyInfo property)
        {
            var type = typeof(T);
            if(type.IsClass) throw new InvalidOperationException("Attempt to build struct property setter for a class type " + type);
            if(type.IsPrimitive) throw new InvalidOperationException("Attempt to build struct property setter for a primitive type " + type);
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void),
                                                  new[] {typeof(Delegate), type.MakeByRefType(), typeof(byte).MakePointerType(), typeof(int).MakeByRefType(), typeof(int)}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1); // stack: [ref obj]
            il.Emit(OpCodes.Ldarg_0); // stack: [ref obj, reader]
            il.Emit(OpCodes.Ldarg_2); // stack: [ref obj, reader, pinnedData]
            il.Emit(OpCodes.Ldarg_3); // stack: [ref obj, reader, pinnedData, ref index]
            il.Emit(OpCodes.Ldarg_S, 4); // stack: [obj, reader, pinnedData, ref index, dataLength]
            var readerDelegate = GetReaderDelegate(property.PropertyType, this);
            il.Emit(OpCodes.Call, readerDelegate.GetType().GetMethod("Invoke")); // stack: [ref obj, reader.Read(pinnedData, ref index, dataLength)]
            il.Emit(OpCodes.Callvirt, property.GetSetMethod()); // obj.Property = reader.Read(pinnedData, ref index)
            il.Emit(OpCodes.Ret);
            var propertySetter = (InternalStructPropertySetterDelegate<T>)dynamicMethod.CreateDelegate(typeof(InternalStructPropertySetterDelegate<T>));
            return (ref T obj, byte* pinnedData, ref int index, int dataLength) => propertySetter(readerDelegate, ref obj, pinnedData, ref index, dataLength);
        }

        private unsafe Delegate GetPropertySetter<T>(PropertyInfo property)
        {
            return typeof(T).IsClass ? (Delegate)BuildClassPropertySetter<T>(property) : BuildStructPropertySetter<T>(property);
        }

        private unsafe ReaderDelegate<T> BuildReader<T>()
        {
            var type = typeof(T);
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), type,
                                                  new[] {typeof(Delegate[]), typeof(ulong[]), typeof(byte).MakePointerType(), typeof(int).MakeByRefType(), typeof(int)}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            var typeCode = il.DeclareLocal(typeof(int));
            var result = il.DeclareLocal(type);
            var length = il.DeclareLocal(typeof(uint));
            var notNullLabel = il.DefineLabel();

            il.Emit(OpCodes.Ldc_I4_1);
            EmitAssertLength(il);

            EmitGoToCurrentLocation(il); // stack: [&data[index]]
            il.Emit(OpCodes.Ldind_U1); // stack: [data[index]]
            il.Emit(OpCodes.Dup); // stack: [data[index], data[index]]
            il.Emit(OpCodes.Stloc, typeCode); // typeCode = data[index]; stack: [typeCode]

            il.Emit(OpCodes.Brtrue, notNullLabel); // if(typeCode != 0) goto notNull;

            EmitIncreaseIndexBy1(il); // index = index + 1
            EmitReturnDefaultValue(type, il, result);

            il.MarkLabel(notNullLabel);

            EmitCheckTypeCode(il, typeCode);

            Delegate[] delegates = null;
            ulong[] hashCodes = null;
            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                EmitNullableValueReader(type, il, out delegates);
            else if(type == typeof(DateTime))
                EmitDateTimeValueReader(type, il, typeCode, result, out delegates);
            else
            {
                EmitIncreaseIndexBy1(il); // index = index + 1
                if(type == typeof(string))
                    EmitStringValueReader(il, typeCode, result, length);
                else if(type == typeof(Guid))
                    EmitGuidValueReader(il, typeCode, result);
                else if(type.IsPrimitive)
                    EmitPrimitiveValueReader<T>(il, typeCode, out delegates);
                else if(type.IsEnum)
                {
                    // TODO
                }
                else
                {
                    if(type.IsArray)
                        EmitArrayReader(type, il, typeCode, result, length, out delegates);
                    else
                    {
                        PropertyInfo[] properties;
                        BuildPropertiesTable(type, out hashCodes, out properties);
                        EmitPropertiesReader<T>(il, typeCode, result, length, properties, out delegates);
                    }
                }
            }
            il.Emit(OpCodes.Ret);

            var reader = (InternalReaderDelegate<T>)dynamicMethod.CreateDelegate(typeof(InternalReaderDelegate<T>));
            return (byte* pinnedData, ref int index, int dataLength) => reader(delegates, hashCodes, pinnedData, ref index, dataLength);
        }

        static void LoadData(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_2);
        }

        static void LoadIndexByRef(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_3);
        }

        static void LoadIndex(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Ldind_I4);
        }

        static void LoadDataLength(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_S, 4);
        }

        private static void EmitCheckTypeCode(ILGenerator il, LocalBuilder typeCode)
        {
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldfld, typeof(GroBufHelpers).GetField("Lengths", BindingFlags.Static | BindingFlags.Public));
            il.Emit(OpCodes.Ldloc, typeCode); // stack: [lengths, typeCode]
            il.Emit(OpCodes.Ldelem_I4); // stack: [lengths[typeCode]]
            var okLabel = il.DefineLabel();
            il.Emit(OpCodes.Brtrue, okLabel); // if(lengths[typeCode] != 0) goto ok;
            il.Emit(OpCodes.Ldstr, "Unknown type code");
            il.Emit(OpCodes.Newobj, typeof(DataCorruptedException).GetConstructor(new[] {typeof(string)}));
            il.Emit(OpCodes.Throw);
            il.MarkLabel(okLabel);
        }

        private static void EmitReturnDefaultValue(Type type, ILGenerator il, LocalBuilder result)
        {
            il.Emit(OpCodes.Ldloca, result); // stack: [&result]
            il.Emit(OpCodes.Initobj, type); // result = default(T)
            il.Emit(OpCodes.Ldloc, result); // stack: [result]
            il.Emit(OpCodes.Ret);
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

        private void EmitArrayReader(Type type, ILGenerator il, LocalBuilder typeCode, LocalBuilder result, LocalBuilder length, out Delegate[] delegates)
        {
            EmitAssertTypeCode(il, typeCode, result, GroBufTypeCode.Array);

            il.Emit(OpCodes.Ldc_I4_4);
            EmitAssertLength(il);

            EmitGoToCurrentLocation(il); // stack: [&data[index]]
            il.Emit(OpCodes.Ldind_U4); // stack: [data length]
            EmitIncreaseIndexBy4(il); // index = index + 4; stack: [data length]

            EmitAssertLength(il);
            il.Emit(OpCodes.Ldc_I4_4);
            EmitAssertLength(il);

            EmitGoToCurrentLocation(il); // stack: [&data[index]]
            il.Emit(OpCodes.Ldind_U4); // stack: [array length]
            EmitIncreaseIndexBy4(il); // index = index + 4; stack: [array length]
            il.Emit(OpCodes.Stloc, length); // length = array length; stack: []

            il.Emit(OpCodes.Ldloc, length); // stack: [length]
            var elementType = type.GetElementType();
            il.Emit(OpCodes.Newarr, elementType); // stack: [new type[length] = result]
            il.Emit(OpCodes.Ldloc, length); // stack: [result, length]
            var allDoneLabel = il.DefineLabel();
            il.Emit(OpCodes.Brfalse, allDoneLabel); // if(length == 0) goto allDone; stack: [result]
            var i = il.DeclareLocal(typeof(uint));
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
            LoadData(il); // stack: [result, {result[i]}, delegates[0], pinnedData]
            LoadIndexByRef(il); // stack: [result, {result[i]}, delegates[0], pinnedData, ref index]
            LoadDataLength(il); // stack: [result, {result[i]}, delegates[0], pinnedData, ref index, dataLength]
            il.Emit(OpCodes.Call, readerDelegate.GetType().GetMethod("Invoke")); // reader.Read<elementType>(pinnedData, ref index, dataLength); stack: [result, {result[i]}, item]
            EmitArrayItemSetter(elementType, il); // result[i] = item; stack: [result]
            il.Emit(OpCodes.Ldloc, i); // stack: [result, i]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [result, i, 1]
            il.Emit(OpCodes.Add); // stack: [result, i + 1]
            il.Emit(OpCodes.Dup); // stack: [result, i + 1, i + 1]
            il.Emit(OpCodes.Stloc, i); // i = i + 1; stack: [result, i]
            il.Emit(OpCodes.Ldloc, length); // stack: [result, i, length]
            il.Emit(OpCodes.Blt_Un, cycleStart); // if(i < length) goto cycleStart
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

        private void EmitPropertiesReader<T>(ILGenerator il, LocalBuilder typeCode, LocalBuilder result, LocalBuilder end, PropertyInfo[] properties, out Delegate[] delegates)
        {
            EmitAssertTypeCode(il, typeCode, result, GroBufTypeCode.Object);

            var type = typeof(T);
            delegates = properties.Select(property => property == null ? null : GetPropertySetter<T>(property)).ToArray();

            il.Emit(OpCodes.Ldc_I4_4);
            EmitAssertLength(il);

            EmitGoToCurrentLocation(il); // stack: [&result[index]]
            il.Emit(OpCodes.Ldind_U4); // stack: [(uint)result[index]]
            EmitIncreaseIndexBy4(il); // index = index + 4; stack: [(uint)result[index]]

            il.Emit(OpCodes.Dup); // stack: [(uint)result[index], (uint)result[index]]
            EmitAssertLength(il); // stack: [(uint)result[index]]

            LoadIndex(il); // stack: [(uint)result[index], index]
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
            EmitAssertLength(il);

            EmitGoToCurrentLocation(il); // stack: [&data[index]]
            il.Emit(OpCodes.Ldind_I8); // stack: [*(int64*)&data[index] = hashCode]
            EmitIncreaseIndexBy8(il); // index = index + 8; stack: [*(int64*)&data[index] = hashCode]

            il.Emit(OpCodes.Dup); // stack: [hashCode, hashCode]
            il.Emit(OpCodes.Ldc_I4, properties.Length); // stack: [hashCode, hashCode, hashCodes.Length]
            il.Emit(OpCodes.Conv_U); // stack: [hashCode, hashCode, (U)hashCodes.Length]
            il.Emit(OpCodes.Rem_Un); // stack: [hashCode, hashCode % hashCodes.Length]
            il.Emit(OpCodes.Conv_I4); // stack: [hashCode, (int)(hashCode % hashCodes.Length)]
            var idx = il.DeclareLocal(typeof(int));
            il.Emit(OpCodes.Stloc, idx); // idx = (int)(hashCode % hashCodes.Length); stack: [hashCode]

            il.Emit(OpCodes.Ldarg_1); // stack: [hashCode, hashCodes]
            il.Emit(OpCodes.Ldloc, idx); // stack: [hashCode, hashCodes, idx]
            il.Emit(OpCodes.Ldelem_I8); // stack: [hashCode, hashCodes[idx]]

            var readPropertyLabel = il.DefineLabel();
            il.Emit(OpCodes.Beq, readPropertyLabel); // if(hashCode == hashCodes[idx]) goto skipData; stack: []

            // Skip data
            EmitGoToCurrentLocation(il); // stack: [&data[index]]
            il.Emit(OpCodes.Ldind_U1); // stack: [data[index]]
            il.Emit(OpCodes.Stloc, typeCode); // typeCode = data[index]; stack: []
            EmitIncreaseIndexBy1(il); // index = index + 1
            EmitCheckTypeCode(il, typeCode);
            EmitSkipValue(il, typeCode);
            var checkIndexLabel = il.DefineLabel();
            il.Emit(OpCodes.Br, checkIndexLabel); // goto checkIndex

            il.MarkLabel(readPropertyLabel);
            // Read data
            il.Emit(OpCodes.Ldarg_0); // stack: [setters]
            il.Emit(OpCodes.Ldloc, idx); // stack: [setters, idx]
            il.Emit(OpCodes.Ldelem_Ref); // stack: [setters[idx]]
            il.Emit(type.IsClass ? OpCodes.Ldloc : OpCodes.Ldloca, result); // stack: [setters[idx], {result}]
            LoadData(il); // stack: [setters[idx], {result}, pinnedData]
            LoadIndexByRef(il); // stack: [setters[idx], {result}, pinnedData, ref index]
            LoadDataLength(il); // stack: [setters[idx], {result}, pinnedData, ref index, dataLength]
            var invoke = type.IsClass ? typeof(ClassPropertySetterDelegate<T>).GetMethod("Invoke") : typeof(StructPropertySetterDelegate<T>).GetMethod("Invoke");
            il.Emit(OpCodes.Call, invoke); // setters[idx]({result}, pinnedData, ref index, dataLength); stack: []

            il.MarkLabel(checkIndexLabel);

            LoadIndex(il); // stack: [index]
            il.Emit(OpCodes.Ldloc, end); // stack: [index, end]
            il.Emit(OpCodes.Blt_Un, cycleStartLabel); // if(index < end) goto cycleStart; stack: []
            il.Emit(OpCodes.Ldloc, result); // stack: [result]
        }

        private static void EmitIncreaseIndexBy8(ILGenerator il)
        {
            LoadIndexByRef(il); // stack: [ref index]
            LoadIndex(il); // stack: [ref index, index]
            il.Emit(OpCodes.Ldc_I4_8); // stack: [ref index, index, 8]
            il.Emit(OpCodes.Add); // stack: [ref index, index + 8]
            il.Emit(OpCodes.Stind_I4); // index = index + 8
        }

        private static void EmitIncreaseIndexBy4(ILGenerator il)
        {
            LoadIndexByRef(il); // stack: [ref index]
            LoadIndex(il); // stack: [ref index, index]
            il.Emit(OpCodes.Ldc_I4_4); // stack: [ref index, index, 4]
            il.Emit(OpCodes.Add); // stack: [ref index, index + 4]
            il.Emit(OpCodes.Stind_I4); // index = index + 4
        }

        private static void EmitIncreaseIndexBy1(ILGenerator il)
        {
            LoadIndexByRef(il); // stack: [ref index]
            LoadIndex(il); // stack: [ref index, index]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [ref index, index, 1]
            il.Emit(OpCodes.Add); // stack: [ref index, index + 1]
            il.Emit(OpCodes.Stind_I4); // index = index + 1
        }

        private static void EmitIncreaseIndexBy2(ILGenerator il)
        {
            LoadIndexByRef(il); // stack: [ref index]
            LoadIndex(il); // stack: [ref index, index]
            il.Emit(OpCodes.Ldc_I4_2); // stack: [ref index, index, 2]
            il.Emit(OpCodes.Add); // stack: [ref index, index + 2]
            il.Emit(OpCodes.Stind_I4); // index = index + 2
        }

        private static void EmitGoToCurrentLocation(ILGenerator il)
        {
            LoadData(il); // stack: [pinnedData]
            LoadIndex(il); // stack: [pinnedData, index]
            il.Emit(OpCodes.Add); // stack: [pinnedData + index]
        }

        private void EmitDateTimeValueReader(Type type, ILGenerator il, LocalBuilder typeCode, LocalBuilder result, out Delegate[] delegates)
        {
            EmitAssertTypeCode(il, typeCode, result, GroBufTypeCode.Int64); // Assert typeCode == TypeCode.Int64

            il.Emit(OpCodes.Ldarg_0); // stack: [delegates]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [delegates, 0]
            il.Emit(OpCodes.Ldelem_Ref); // stack: [delegates[0]]
            LoadData(il); // stack: [delegates[0], pinnedData]
            LoadIndexByRef(il); // stack: [delegates[0], pinnedData, ref index]
            LoadDataLength(il); // stack: [delegates[0], pinnedData, ref index, dataLength]
            var readerDelegate = GetReaderDelegate(typeof(long), this);
            delegates = new[] {readerDelegate};
            il.Emit(OpCodes.Call, readerDelegate.GetType().GetMethod("Invoke")); // stack: [reader<long>(pinnedData, ref index, dataLength)]
            il.Emit(OpCodes.Ldc_I4, (int)DateTimeKind.Utc);
            il.Emit(OpCodes.Newobj, type.GetConstructor(new[] {typeof(long), typeof(DateTimeKind)})); // stack: new DateTime(reader<long>(pinnedData, ref index), DateTimeKind.Utc)
        }

        private void EmitNullableValueReader(Type type, ILGenerator il, out Delegate[] delegates)
        {
            il.Emit(OpCodes.Ldarg_0); // stack: [delegates]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [delegates, 0]
            il.Emit(OpCodes.Ldelem_Ref); // stack: [delegates[0]]
            LoadData(il); // stack: [delegates[0], pinnedData]
            LoadIndexByRef(il); // stack: [delegates[0], pinnedData, ref index]
            LoadDataLength(il); // stack: [delegates[0], pinnedData, ref index, dataLength]
            var elementType = type.GetGenericArguments()[0];
            var readerDelegate = GetReaderDelegate(elementType, this);
            delegates = new[] {readerDelegate};
            il.Emit(OpCodes.Call, readerDelegate.GetType().GetMethod("Invoke")); // stack: [reader<elementType>(pinnedData, ref index, dataLength)]
            il.Emit(OpCodes.Newobj, type.GetConstructor(new[] {elementType})); // stack: new type(reader<elementType>(pinnedData, ref index, dataLength))
        }

        private PrimitiveValueReaderDelegate<T> BuildDefaultValueReader<T>()
        {
            var type = typeof(T);
            if(!type.IsPrimitive) throw new InvalidOperationException("Attempt to build primitive value reader for a non-primitive type " + type);
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), type, new[] {typeof(byte).MakePointerType()}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            var result = il.DeclareLocal(type);
            il.Emit(OpCodes.Ldloca, result);
            il.Emit(OpCodes.Initobj, type);
            il.Emit(OpCodes.Ldloc, result);
            il.Emit(OpCodes.Ret);
            return (PrimitiveValueReaderDelegate<T>)dynamicMethod.CreateDelegate(typeof(PrimitiveValueReaderDelegate<T>));
        }

        private PrimitiveValueReaderDelegate<T> BuildPrimitiveValueReader<T>(GroBufTypeCode typeCode)
        {
            var type = typeof(T);
            if (!type.IsPrimitive) throw new InvalidOperationException("Attempt to build primitive value reader for a non-primitive type " + type);
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), type, new[] { typeof(byte).MakePointerType() }, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            var expectedTypeCode = GroBufHelpers.GetTypeCode(type);
            il.Emit(OpCodes.Ldarg_0); // stack: [address]
            EmitReadPrimitiveValue(il, typeCode); // stack: [value]
            if (type == typeof(bool))
            {
                il.Emit(OpCodes.Ldc_I4_0); // stack: [0, value]
                il.Emit(OpCodes.Ceq); // stack: [value == 0]
                il.Emit(OpCodes.Ldc_I4_1); // stack: [value == 0, 1]
                il.Emit(OpCodes.Xor); // stack: [value != 0]
            }
            else
                EmitConvertValue(il, typeCode, expectedTypeCode);
            il.Emit(OpCodes.Ret);
            return (PrimitiveValueReaderDelegate<T>)dynamicMethod.CreateDelegate(typeof(PrimitiveValueReaderDelegate<T>));
        }

        private static void EmitConvertValue(ILGenerator il, GroBufTypeCode typeCode, GroBufTypeCode expectedTypeCode)
        {
            if(expectedTypeCode == typeCode)
                return;
            switch(expectedTypeCode)
            {
            case GroBufTypeCode.Int8:
                il.Emit(OpCodes.Conv_I1);
                break;
            case GroBufTypeCode.UInt8:
                il.Emit(OpCodes.Conv_U1);
                break;
            case GroBufTypeCode.Int16:
                il.Emit(OpCodes.Conv_I2);
                break;
            case GroBufTypeCode.UInt16:
                il.Emit(OpCodes.Conv_U2);
                break;
            case GroBufTypeCode.Int32:
                if(typeCode == GroBufTypeCode.Int64 || typeCode == GroBufTypeCode.UInt64 || typeCode == GroBufTypeCode.Double || typeCode == GroBufTypeCode.Single)
                    il.Emit(OpCodes.Conv_I4);
                break;
            case GroBufTypeCode.UInt32:
                if(typeCode == GroBufTypeCode.Int64 || typeCode == GroBufTypeCode.UInt64 || typeCode == GroBufTypeCode.Double || typeCode == GroBufTypeCode.Single)
                    il.Emit(OpCodes.Conv_U4);
                break;
            case GroBufTypeCode.Int64:
                if (typeCode != GroBufTypeCode.UInt64)
                {
                    if (typeCode == GroBufTypeCode.UInt8 || typeCode == GroBufTypeCode.UInt16 || typeCode == GroBufTypeCode.UInt32)
                        il.Emit(OpCodes.Conv_U8);
                    else
                        il.Emit(OpCodes.Conv_I8);
                }
                break;
            case GroBufTypeCode.UInt64:
                if (typeCode != GroBufTypeCode.Int64)
                {
                    if(typeCode == GroBufTypeCode.Int8 || typeCode == GroBufTypeCode.Int16 || typeCode == GroBufTypeCode.Int32)
                        il.Emit(OpCodes.Conv_I8);
                    else
                        il.Emit(OpCodes.Conv_U8);
                }
                break;
            case GroBufTypeCode.Single:
                if(typeCode == GroBufTypeCode.UInt64 || typeCode == GroBufTypeCode.UInt32)
                    il.Emit(OpCodes.Conv_R_Un);
                il.Emit(OpCodes.Conv_R4);
                break;
            case GroBufTypeCode.Double:
                if (typeCode == GroBufTypeCode.UInt64 || typeCode == GroBufTypeCode.UInt32)
                    il.Emit(OpCodes.Conv_R_Un);
                il.Emit(OpCodes.Conv_R8);
                break;
            default:
                throw new NotSupportedException();
            }
        }

        private static void DebugWrite(ILGenerator il, Type type)
        {
            il.EmitWriteLine(type.Name);
            var temp = il.DeclareLocal(type);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Stloc, temp);
            il.EmitWriteLine(temp);
        }

        private static void EmitReadPrimitiveValue(ILGenerator il, GroBufTypeCode typeCode)
        {
            switch(typeCode)
            {
            case GroBufTypeCode.Int8:
                il.Emit(OpCodes.Ldind_I1);
                break;
            case GroBufTypeCode.UInt8:
                il.Emit(OpCodes.Ldind_U1);
                break;
            case GroBufTypeCode.Int16:
                il.Emit(OpCodes.Ldind_I2);
                break;
            case GroBufTypeCode.UInt16:
                il.Emit(OpCodes.Ldind_U2);
                break;
            case GroBufTypeCode.Int32:
                il.Emit(OpCodes.Ldind_I4);
                break;
            case GroBufTypeCode.UInt32:
                il.Emit(OpCodes.Ldind_U4);
                break;
            case GroBufTypeCode.Int64:
                il.Emit(OpCodes.Ldind_I8);
                break;
            case GroBufTypeCode.UInt64:
                il.Emit(OpCodes.Ldind_I8);
                break;
            case GroBufTypeCode.Single:
                il.Emit(OpCodes.Ldind_R4);
                break;
            case GroBufTypeCode.Double:
                il.Emit(OpCodes.Ldind_R8);
                break;
            default:
                throw new NotSupportedException();
            }
        }

        private void EmitPrimitiveValueReader<T>(ILGenerator il, LocalBuilder typeCode, out Delegate[] delegates)
        {
            delegates = GetPrimitiveReaders<T>();

            il.Emit(OpCodes.Ldarg_0); // stack: [delegates]
            il.Emit(OpCodes.Ldloc, typeCode); // stack: [delegates, typeCode]
            il.Emit(OpCodes.Ldelem_Ref); // stack: [delegates[typeCode]]
            EmitGoToCurrentLocation(il); // stack: [delegates[typeCode], &data[index]]
            EmitSkipValue(il, typeCode);
            il.Emit(OpCodes.Call, typeof(PrimitiveValueReaderDelegate<T>).GetMethod("Invoke")); // delegates[typeCode](&data[index]); stack: [result]
        }

        private static void EmitSkipValue(ILGenerator il, LocalBuilder typeCode)
        {
            LoadIndexByRef(il); // stack: [ref index]
            LoadIndex(il); // stack: [ref index, index]

            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldfld, typeof(GroBufHelpers).GetField("Lengths", BindingFlags.Static | BindingFlags.Public));

            il.Emit(OpCodes.Ldloc, typeCode); // stack: [ref index, index, lengths, typeCode]
            il.Emit(OpCodes.Ldelem_I4); // stack: [ref index, index, lengths[typeCode]]
            il.Emit(OpCodes.Dup); // stack: [ref index, index, lengths[typeCode], lengths[typeCode]]
            il.Emit(OpCodes.Ldc_I4_M1); // stack: [ref index, index, lengths[typeCode], lengths[typeCode], -1]
            var increaseLabel = il.DefineLabel();
            il.Emit(OpCodes.Bne_Un, increaseLabel); // if(lengths[typeCode] != -1) goto increase;

            il.Emit(OpCodes.Ldc_I4_4);
            EmitAssertLength(il);
            il.Emit(OpCodes.Pop); // stack: [ref index, index]
            il.Emit(OpCodes.Dup); // stack: [ref index, index, index]
            LoadData(il); // stack: [ref index, index, index, pinnedData]
            il.Emit(OpCodes.Add); // stack: [ref index, index, index + pinnedData]
            il.Emit(OpCodes.Ldind_U4); // stack: [ref index, index, *(uint*)(pinnedData + index)]
            il.Emit(OpCodes.Ldc_I4_4); // stack: [ref index, index, *(uint*)(pinnedData + index), 4]
            il.Emit(OpCodes.Add); // stack: [ref index, *(uint*)(pinnedData + index) + 4]

            il.MarkLabel(increaseLabel);
            il.Emit(OpCodes.Dup); // stack: [ref index, length, length]
            EmitAssertLength(il); // stack: [ref index, length]
            il.Emit(OpCodes.Add); // stack: [ref index, index + length]
            il.Emit(OpCodes.Stind_I4); // index = index + length
        }

        private static void EmitGuidValueReader(ILGenerator il, LocalBuilder typeCode, LocalBuilder result)
        {
            EmitAssertTypeCode(il, typeCode, result, GroBufTypeCode.Guid); // Assert typeCode == TypeCode.Guid

            il.Emit(OpCodes.Ldc_I4, 16);
            EmitAssertLength(il);
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
            EmitIncreaseIndexBy8(il); // index = index + 8
        }

        private static void EmitAssertTypeCode(ILGenerator il, LocalBuilder typeCode, LocalBuilder result, GroBufTypeCode expectedTypeCode)
        {
            il.Emit(OpCodes.Ldloc, typeCode); // stack: [typeCode]
            il.Emit(OpCodes.Ldc_I4, (int)expectedTypeCode); // stack: [typeCode, expectedTypeCode]

            var label = il.DefineLabel();
            il.Emit(OpCodes.Beq, label);

            EmitSkipValue(il, typeCode);
            EmitReturnDefaultValue(typeof(string), il, result);

            il.MarkLabel(label);
        }

        private static void EmitStringValueReader(ILGenerator il, LocalBuilder typeCode, LocalBuilder result, LocalBuilder length)
        {
            EmitAssertTypeCode(il, typeCode, result, GroBufTypeCode.String); // Assert typeCode == TypeCode.String

            il.Emit(OpCodes.Ldc_I4_4);
            EmitAssertLength(il);

            EmitGoToCurrentLocation(il); // stack: [&data[index]]
            il.Emit(OpCodes.Ldind_U4); // stack: [(uint)data[index]]
            il.Emit(OpCodes.Dup); // stack: [(uint)data[index], (uint)data[index]]
            il.Emit(OpCodes.Stloc, length); // length = (uint)data[index]; stack: [length]
            EmitIncreaseIndexBy4(il); // index = index + 4; stack: [length]

            EmitAssertLength(il);

            EmitGoToCurrentLocation(il); // stack: [&data[index]]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [&data[index], 0]
            il.Emit(OpCodes.Ldloc, length); // stack: [&data[index], 0, length]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [&data[index], 0, length, 1]
            il.Emit(OpCodes.Shr_Un); // stack: [&data[index], 0, length >> 1]
            il.Emit(OpCodes.Newobj, typeof(string).GetConstructor(new[] {typeof(char*), typeof(int), typeof(int)})); // stack: [new string(&data[index], 0, length >> 1)]
            LoadIndexByRef(il); // stack: [new string(&data[index], 0, length >> 1), ref index]
            LoadIndex(il); // stack: [new string(&data[index], 0, length >> 1), ref index, index]
            il.Emit(OpCodes.Ldloc, length); // stack: [new string(&data[index], 0, length >> 1), ref index, index, length]
            il.Emit(OpCodes.Add); // stack: [new string(&data[index], 0, length >> 1), ref index, index + length]
            il.Emit(OpCodes.Stind_I4); // index = index + length; stack: [new string(&data[index], 0, length >> 1)]
        }

        private static void EmitAssertLength(ILGenerator il)
        {
            LoadIndex(il); // stack: [length, index]
            il.Emit(OpCodes.Add); // stack: [length + index]
            LoadDataLength(il); // stack: [length + index, dataLength]
            var label = il.DefineLabel();
            il.Emit(OpCodes.Ble_Un, label);
            il.Emit(OpCodes.Ldstr, "Index out of bounds");
            il.Emit(OpCodes.Newobj, typeof(DataCorruptedException).GetConstructor(new[] {typeof(string)}));
            il.Emit(OpCodes.Throw);
            il.MarkLabel(label);
        }

        private static unsafe Delegate GetReaderDelegate(Type type, GroBufReaderOld reader)
        {
            if(getReaderMethod == null)
                getReaderMethod = ((MethodCallExpression)((Expression<Action<GroBufReaderOld>>)(grawReader => grawReader.GetReader<int>())).Body).Method.GetGenericMethodDefinition();
            return ((Delegate)getReaderMethod.MakeGenericMethod(new[] {type}).Invoke(reader, new object[0]));
        }

        private static MethodInfo getReaderMethod;

        private readonly Hashtable readers = new Hashtable();
        private readonly object readersLock = new object();

        private readonly Hashtable pinnedReaders = new Hashtable();
        private readonly object pinningReadersLock = new object();

        private readonly Hashtable primitiveValueReaders = new Hashtable();
        private readonly object primitiveValueReadersLock = new object();
    }
}