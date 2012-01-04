using System;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace SKBKontur.GroBuf
{
    internal class GroBufWriterOld
    {
        // TODO: enum, derived types, decimal
        public byte[] Write<T>(T obj)
        {
            var buf = new byte[4096];
            int index = 0;
            Write(obj, true, ref buf, ref index);
            var result = new byte[index];
            // TODO
            Array.Copy(buf, result, index);
            return result;
        }

        internal unsafe delegate void WriterDelegate<in T>(T obj, bool writeEmpty, ref byte[] result, ref int index, ref byte* pinnedResult);

        private delegate void PinningWriterDelegate<in T>(T obj, bool writeEmpty, ref byte[] result, ref int index);

        private delegate void InternalPinningWriterDelegate<in T>(Delegate writerDelegate, T obj, bool writeEmpty, ref byte[] result, ref int index);

        private unsafe delegate void InternalWriterDelegate<in T>(Delegate[] writersDelegates, T obj, bool writeEmpty, ref byte[] result, ref int index, ref byte* pinnedResult);

        private unsafe delegate void InternalEnumWriterDelegate<in T>(ulong[] hashCodes, T obj, bool writeEmpty, ref byte[] result, ref int index, ref byte* pinnedResult);

        private void Write<T>(T obj, bool writeEmpty, ref byte[] buf, ref int index)
        {
            GetPinningWriter<T>()(obj, writeEmpty, ref buf, ref index);
        }

        private PinningWriterDelegate<T> GetPinningWriter<T>()
        {
            var type = typeof(T);
            var pinningWriter = (PinningWriterDelegate<T>)pinningWriters[type];
            if(pinningWriter == null)
            {
                lock(pinningWritersLock)
                {
                    pinningWriter = (PinningWriterDelegate<T>)pinningWriters[type];
                    if(pinningWriter == null)
                    {
                        pinningWriter = BuildPinningWriter<T>();
                        pinningWriters[type] = pinningWriter;
                    }
                }
            }
            return pinningWriter;
        }

        private unsafe WriterDelegate<T> GetWriter<T>()
        {
            var type = typeof(T);
            var writer = (WriterDelegate<T>)writers[type];
            if(writer == null)
            {
                lock(writersLock)
                {
                    writer = (WriterDelegate<T>)writers[type];
                    if(writer == null)
                    {
                        writer = BuildWriter<T>();
                        writers[type] = writer;
                    }
                }
            }
            return writer;
        }

        private PinningWriterDelegate<T> BuildPinningWriter<T>()
        {
            var type = typeof(T);
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void),
                                                  new[] {typeof(Delegate), type, typeof(bool), typeof(byte[]).MakeByRefType(), typeof(int).MakeByRefType()}, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            var pinnedResult = il.DeclareLocal(typeof(byte).MakeByRefType(), true);
            il.Emit(OpCodes.Ldarg_3); // stack: [ref result]
            il.Emit(OpCodes.Ldind_Ref); // stack: [result]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [result, 0]
            il.Emit(OpCodes.Ldelema, typeof(byte)); // stack: [&result[0]]
            il.Emit(OpCodes.Stloc, pinnedResult); // pinnedResult = &result[0]; stack: []
            var writerDelegate = GetWriterDelegate(type);
            il.Emit(OpCodes.Ldarg_0); // stack: [writerDelegate]
            il.Emit(OpCodes.Ldarg_1); // stack: [writerDelegate, obj]
            il.Emit(OpCodes.Ldarg_2); // stack: [writerDelegate, obj, writeEmpty]
            il.Emit(OpCodes.Ldarg_3); // stack: [writerDelegate, obj, writeEmpty, ref result]
            il.Emit(OpCodes.Ldarg_S, 4); // stack: [writerDelegate, obj, writeEmpty, ref result, ref index]
            il.Emit(OpCodes.Ldloca, pinnedResult); // stack: [writerDelegate, obj, writeEmpty, ref result, ref index, ref pinnedResult]
            il.Emit(OpCodes.Call, writerDelegate.GetType().GetMethod("Invoke")); // writer.write<T>(obj, writeEmpty, ref result, ref index, ref pinnedResult); stack: []
            il.Emit(OpCodes.Ldc_I4_0); // stack: [0]
            il.Emit(OpCodes.Conv_U); // stack: [(uint)0]
            il.Emit(OpCodes.Stloc, pinnedResult); // pinnedResult = null
            il.Emit(OpCodes.Ret);

            var pinningWriter = (InternalPinningWriterDelegate<T>)dynamicMethod.CreateDelegate(typeof(InternalPinningWriterDelegate<T>));
            return (T obj, bool writeEmpty, ref byte[] result, ref int index) => pinningWriter(writerDelegate, obj, writeEmpty, ref result, ref index);
        }

        private WriterDelegate<T> BuildEnumWriter<T>()
        {
            var type = typeof(T);
            if(!type.IsEnum) throw new InvalidOperationException("Expected enum but was " + type);
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void),
                                                  new[]
                                                      {
                                                          typeof(ulong[]), type, typeof(bool), typeof(byte[]).MakeByRefType(),
                                                          typeof(int).MakeByRefType(), typeof(byte).MakePointerType().MakeByRefType()
                                                      }, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            var retLabel = il.DefineLabel();
            var temp1 = il.DeclareLocal(typeof(int));
            var temp2 = il.DeclareLocal(typeof(int));
            var pinned = il.DeclareLocal(typeof(byte).MakeByRefType(), true);
            il.Emit(OpCodes.Ldc_I4, 12);
            EmitEnsureSize(il, temp1, temp2, pinned);
            // TODO
            return null;
        }

        private static void LoadObj(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_1);
        }

        private static void LoadObjByRef(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarga_S, 1);
        }

        private static void LoadWriteEmpty(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_2);
        }

        private static void LoadResultByRef(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_3);
        }

        private static void LoadIndexByRef(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_S, 4);
        }

        private static void LoadIndex(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_S, 4);
            il.Emit(OpCodes.Ldind_I4);
        }

        private static void LoadPinnedResultByRef(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_S, 5);
        }

        private static void LoadPinnedResult(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_S, 5);
            il.Emit(OpCodes.Ldind_I);
        }

        private unsafe WriterDelegate<T> BuildWriter<T>()
        {
            var type = typeof(T);
            if(type.IsEnum)
                return BuildEnumWriter<T>();
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void),
                                                  new[]
                                                      {
                                                          typeof(Delegate[]), type, typeof(bool), typeof(byte[]).MakeByRefType(),
                                                          typeof(int).MakeByRefType(), typeof(byte).MakePointerType().MakeByRefType()
                                                      }, GetType(), true);
            var il = dynamicMethod.GetILGenerator();
            var notNullLabel = il.DefineLabel();
            var temp1 = il.DeclareLocal(typeof(int));
            var temp2 = il.DeclareLocal(typeof(int));
            var pinned = il.DeclareLocal(typeof(byte).MakeByRefType(), true);
            bool isNullable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
            if(type.IsClass || isNullable) // Only classes & nullables can be null
            {
                // Check if obj equals to null
                if(type == typeof(string))
                {
                    LoadObj(il); // stack: [obj]
                    il.Emit(OpCodes.Call, GetIsNullOrEmptyMethod()); // stack: [string.isNullOrEmpty(obj)]
                    il.Emit(OpCodes.Brfalse, notNullLabel); // if(!string.isNullOrEmpty(obj)) goto notNull;
                }
                else if(isNullable)
                {
                    // Nullable
                    LoadObjByRef(il); // stack: [&obj]
                    il.Emit(OpCodes.Call, type.GetProperty("HasValue").GetGetMethod()); // stack: obj.HasValue
                    il.Emit(OpCodes.Brtrue, notNullLabel); // if(obj.HasValue) goto notNull;
                }
                else
                {
                    // class
                    LoadObj(il); // stack: [obj]
                    il.Emit(OpCodes.Brtrue, notNullLabel); // if(obj != null) goto notNull;
                }
                LoadWriteEmpty(il); // stack: [writeEmpty]

                var retLabel = il.DefineLabel();
                il.Emit(OpCodes.Brfalse, retLabel); // if(!writeEmpty) goto ret;
                EmitNullWriter(il, temp1, temp2, pinned);
                il.MarkLabel(retLabel);
                il.Emit(OpCodes.Ret);
            }
            il.MarkLabel(notNullLabel);

            Delegate[] delegates = null;
            if(isNullable)
                EmitNullableValueWriter(type, il, out delegates);
            else if(type == typeof(string))
                EmitStringValueWriter(il, temp1, temp2, pinned);
            else if(type.IsPrimitive)
                EmitPrimitiveValueWriter(type, il, temp1, temp2, pinned);
            else if(type == typeof(DateTime))
                EmitDateTimeValueWriter(type, il, out delegates);
            else if(type == typeof(Guid))
                EmitGuidValueWriter(il, temp1, temp2, pinned);
            else
            {
                if(type.IsArray)
                    EmitArrayWriter(type, il, temp1, temp2, pinned, out delegates);
                else
                    EmitPropertiesWriter(type, il, temp1, temp2, pinned, out delegates);
            }
            il.Emit(OpCodes.Ret);

            var writer = (InternalWriterDelegate<T>)dynamicMethod.CreateDelegate(typeof(InternalWriterDelegate<T>));
            return (T obj, bool writeEmpty, ref byte[] result, ref int index, ref byte* pinnedResult) => writer(delegates, obj, writeEmpty, ref result, ref index, ref pinnedResult);
        }

        private static void EmitNullWriter(ILGenerator il, LocalBuilder temp1, LocalBuilder temp2, LocalBuilder pinned)
        {
            il.Emit(OpCodes.Ldc_I4_1); // stack: [1]
            EmitEnsureSize(il, temp1, temp2, pinned);
            EmitGoToCurrentLocation(il); // stack: [&result[index]]
            il.Emit(OpCodes.Ldc_I4, (int)GroBufTypeCode.Empty); // stack: [&result[index], TypeCode.Empty]
            il.Emit(OpCodes.Stind_I1); // result[index] = TypeCode.Empty; stack: []
            EmitIncreaseIndexBy1(il); // index = index + 1
        }

        private void EmitArrayWriter(Type type, ILGenerator il, LocalBuilder temp1, LocalBuilder temp2, LocalBuilder pinned, out Delegate[] delegates)
        {
            var allDoneLabel = il.DefineLabel();
            var length = il.DeclareLocal(typeof(int));
            LoadObj(il); // stack: [obj]
            il.Emit(OpCodes.Ldlen); // stack: [obj.Length]
            il.Emit(OpCodes.Dup); // stack: [obj.Length, obj.Length]
            il.Emit(OpCodes.Stloc, length); // length = obj.Length; stack: [length]
            var writeLengthLabel = il.DefineLabel();
            il.Emit(OpCodes.Brtrue, writeLengthLabel); // if(length != 0) goto writeLength
            LoadWriteEmpty(il); // stack: [writeEmpty]
            il.Emit(OpCodes.Brfalse, allDoneLabel); // if(!writeEmpty) goto allDone;
            EmitNullWriter(il, temp1, temp2, pinned);
            il.Emit(OpCodes.Ret);

            il.MarkLabel(writeLengthLabel);
            il.Emit(OpCodes.Ldc_I4, 9); // stack: [9]
            EmitEnsureSize(il, temp1, temp2, pinned);
            EmitGoToCurrentLocation(il); // stack: [&result[index]]
            il.Emit(OpCodes.Ldc_I4, (int)GroBufTypeCode.Array); // stack: [&result[index], TypeCode.Array]
            il.Emit(OpCodes.Stind_I1); // result[index] = TypeCode.Array
            EmitIncreaseIndexBy1(il); // index = index + 1
            LoadIndex(il); // stack: [index]
            var start = il.DeclareLocal(typeof(int));
            il.Emit(OpCodes.Stloc, start); // start = index
            EmitIncreaseIndexBy4(il); // index = index + 4
            EmitGoToCurrentLocation(il); // stack: [&result[index]]
            il.Emit(OpCodes.Ldloc, length); // stack: [&result[index], length]
            il.Emit(OpCodes.Stind_I4); // *(int*)&result[index] = length; stack: []
            EmitIncreaseIndexBy4(il); // index = index + 4

            var i = il.DeclareLocal(typeof(int));
            il.Emit(OpCodes.Ldc_I4_0); // stack: [0]
            il.Emit(OpCodes.Stloc, i); // i = 0; stack: []
            var cycleStart = il.DefineLabel();
            il.MarkLabel(cycleStart);
            il.Emit(OpCodes.Ldarg_0); // stack: [delegates]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [delegates, 0]
            il.Emit(OpCodes.Ldelem_Ref); // stack: [delegates[0]]
            LoadObj(il); // stack: [delegates[0], obj]
            il.Emit(OpCodes.Ldloc, i); // stack: [delegates[0], obj, i]
            var elementType = type.GetElementType();
            EmitArrayItemGetter(elementType, il); // stack: [delegates[0], obj[i]]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [delegates[0], obj[i], true]
            LoadResultByRef(il); // stack: [delegates[0], obj[i], true, ref result]
            LoadIndexByRef(il); // stack: [delegates[0], obj[i], true, ref result, ref index]
            LoadPinnedResultByRef(il); // stack: [delegates[0], obj[i], true, ref result, ref index, ref pinnedResult]
            var writerDelegate = GetWriterDelegate(elementType);
            delegates = new[] {writerDelegate};
            il.Emit(OpCodes.Call, writerDelegate.GetType().GetMethod("Invoke")); // delegates[0].Write<elementType>(obj[i], true, ref result, ref index, ref pinnedResult); stack: []
            il.Emit(OpCodes.Ldloc, length); // stack: [length]
            il.Emit(OpCodes.Ldloc, i); // stack: [length, i]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [length, i, 1]
            il.Emit(OpCodes.Add); // stack: [length, i + 1]
            il.Emit(OpCodes.Dup); // stack: [length, i + 1, i + 1]
            il.Emit(OpCodes.Stloc, i); // i = i + 1; stack: [length, i]
            il.Emit(OpCodes.Bgt, cycleStart); // if(length > i) goto cycleStart; stack: []

            LoadPinnedResult(il); // stack: [pinnedResult]
            il.Emit(OpCodes.Ldloc, start); // stack: [pinnedResult, start]
            il.Emit(OpCodes.Add); // stack: [pinnedResult + start]
            LoadIndex(il); // stack: [pinnedResult + start, index]
            il.Emit(OpCodes.Ldloc, start); // stack: [pinnedResult + start, index, start]
            il.Emit(OpCodes.Sub); // stack: [pinnedResult + start, index - start]
            il.Emit(OpCodes.Ldc_I4_4); // stack: [pinnedResult + start, index - start, 4]
            il.Emit(OpCodes.Sub); // stack: [pinnedResult + start, index - start - 4]
            il.Emit(OpCodes.Stind_I4); // *(int*)(pinnedResult + start) = index - start - 4

            il.MarkLabel(allDoneLabel);
        }

        private static void EmitArrayItemGetter(Type elementType, ILGenerator il)
        {
            if(elementType.IsClass) // class
                il.Emit(OpCodes.Ldelem_Ref);
            else if(!elementType.IsPrimitive)
            {
                // struct
                il.Emit(OpCodes.Ldelema, elementType);
                il.Emit(OpCodes.Ldobj, elementType);
            }
            else
            {
                // Primitive
                switch(Type.GetTypeCode(elementType))
                {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                    il.Emit(OpCodes.Ldelem_I1);
                    break;
                case TypeCode.Byte:
                    il.Emit(OpCodes.Ldelem_U1);
                    break;
                case TypeCode.Int16:
                    il.Emit(OpCodes.Ldelem_I2);
                    break;
                case TypeCode.Int32:
                    il.Emit(OpCodes.Ldelem_I4);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    il.Emit(OpCodes.Ldelem_I8);
                    break;
                case TypeCode.UInt16:
                    il.Emit(OpCodes.Ldelem_U2);
                    break;
                case TypeCode.UInt32:
                    il.Emit(OpCodes.Ldelem_U4);
                    break;
                case TypeCode.Single:
                    il.Emit(OpCodes.Ldelem_R4);
                    break;
                case TypeCode.Double:
                    il.Emit(OpCodes.Ldelem_R8);
                    break;
                default:
                    throw new NotSupportedException();
                }
            }
        }

        private void EmitPropertiesWriter(Type type, ILGenerator il, LocalBuilder length, LocalBuilder temp2, LocalBuilder pinned, out Delegate[] delegates)
        {
            var start = il.DeclareLocal(typeof(int));
            LoadIndexByRef(il); // stack: [ref index]
            il.Emit(OpCodes.Dup); // stack: [ref index, ref index]
            il.Emit(OpCodes.Ldind_I4); // stack: [ref index, index]
            il.Emit(OpCodes.Dup); // stack: [ref index, index, index]
            il.Emit(OpCodes.Stloc, start); // start = index; stack: [ref index, index]
            il.Emit(OpCodes.Ldc_I4_5); // stack: [ref index, index, 5]
            il.Emit(OpCodes.Add); // stack: [ref index, index + 5]
            il.Emit(OpCodes.Stind_I4); // index = index + 5; stack: []

            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var prev = il.DeclareLocal(typeof(int));
            delegates = new Delegate[properties.Length];
            for(int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                il.Emit(OpCodes.Ldarg_0); // stack: [delegates]
                il.Emit(OpCodes.Ldc_I4, i); // stack: [delegates, i]
                il.Emit(OpCodes.Ldelem_Ref); // stack: [delegates[i]]
                il.Emit(type.IsClass ? OpCodes.Ldarg_S : OpCodes.Ldarga_S, 1); // stack: [delegates[i], {obj}]
                il.Emit(OpCodes.Callvirt, property.GetGetMethod()); // stack: [delegates[i], obj.prop]
                il.Emit(OpCodes.Ldc_I4_0); // stack: [delegates[i], obj.prop, false]
                LoadResultByRef(il); // stack: [delegates[i], obj.prop, false, ref result]
                LoadIndexByRef(il); // stack: [delegates[i], obj.prop, false, ref result, ref index]
                il.Emit(OpCodes.Dup); // stack: [delegates[i], obj.prop, false, ref result, ref index, ref index]
                il.Emit(OpCodes.Dup); // stack: [delegates[i], obj.prop, false, ref result, ref index, ref index, ref index]
                il.Emit(OpCodes.Ldind_I4); // stack: [delegates[i], obj.prop, false, ref result, ref index, ref index, index]
                il.Emit(OpCodes.Dup); // stack: [delegates[i], obj.prop, false, ref result, ref index, ref index, index, index]
                il.Emit(OpCodes.Stloc, prev); // prev = index; stack: [delegates[i], obj.prop, false, ref result, ref index, ref index, index]
                il.Emit(OpCodes.Ldc_I4_8); // stack: [delegates[i], obj.prop, false, ref result, ref index, ref index, index, 8]
                il.Emit(OpCodes.Add); // stack: [delegates[i], obj.prop, false, ref result, ref index, ref index, index + 8]
                il.Emit(OpCodes.Stind_I4); // index = index + 8; stack: [delegates[i], obj.prop, false, ref result, ref index]
                LoadPinnedResultByRef(il); // stack: [delegates[i], obj.prop, false, ref result, ref index, ref pinnedResult]
                var writerDelegate = GetWriterDelegate(property.PropertyType);
                delegates[i] = writerDelegate;
                il.Emit(OpCodes.Call, writerDelegate.GetType().GetMethod("Invoke")); // delegates[i].Write<PropertyType>(obj.prop, false, ref result, ref index, ref pinnedResult)
                LoadIndex(il); // stack: [index]
                il.Emit(OpCodes.Ldc_I4_8); // stack: [index, 8]
                il.Emit(OpCodes.Sub); // stack: [index - 8]
                il.Emit(OpCodes.Ldloc, prev); // stack: [index - 8, prev]
                var writeHashCodeLabel = il.DefineLabel();
                il.Emit(OpCodes.Bgt, writeHashCodeLabel); // if(index - 8 > prev) goto writeHashCode;
                LoadIndexByRef(il); // stack: [ref index]
                il.Emit(OpCodes.Ldloc, prev); // stack: [ref index, prev]
                il.Emit(OpCodes.Stind_I4); // index = prev;
                var next = il.DefineLabel();
                il.Emit(OpCodes.Br, next); // goto next;

                il.MarkLabel(writeHashCodeLabel);

                LoadPinnedResult(il); // stack: [pinnedResult]
                il.Emit(OpCodes.Ldloc, prev); // stack: [pinnedResult, prev]
                il.Emit(OpCodes.Add); // stack: [pinnedResult + prev]
                il.Emit(OpCodes.Ldc_I8, (long)GroBufHelpers.CalcHash(property.Name)); // stack: [&result[index], prop.Name.HashCode]
                il.Emit(OpCodes.Stind_I8); // *(long*)(pinnedResult + prev) = prop.Name.HashCode; stack: []

                il.MarkLabel(next);
            }

            LoadIndex(il); // stack: [index]
            il.Emit(OpCodes.Ldloc, start); // stack: [index, start]
            il.Emit(OpCodes.Sub); // stack: [index - start]
            il.Emit(OpCodes.Ldc_I4_5); // stack: [index - start, 5]
            il.Emit(OpCodes.Sub); // stack: [index - start - 5]

            var writeLengthLabel = il.DefineLabel();
            var allDoneLabel = il.DefineLabel();
            il.Emit(OpCodes.Dup); // stack: [index - start - 5, index - start - 5]
            il.Emit(OpCodes.Stloc, length); // length = index - start - 5; stack: [length]
            il.Emit(OpCodes.Brtrue, writeLengthLabel); // if(length != 0) goto writeLength;

            LoadIndexByRef(il); // stack: [ref index]
            il.Emit(OpCodes.Ldloc, start); // stack: [ref index, start]
            il.Emit(OpCodes.Stind_I4); // index = start
            LoadWriteEmpty(il); // stack: [writeEmpty]
            il.Emit(OpCodes.Brfalse, allDoneLabel); // if(!writeEmpty) goto allDone;
            EmitNullWriter(il, length, temp2, pinned);
            il.Emit(OpCodes.Ret);

            il.MarkLabel(writeLengthLabel);

            LoadPinnedResult(il); // stack: [pinnedResult]
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

        private static void EmitIncreaseIndexBy8(ILGenerator il)
        {
            LoadIndexByRef(il); // stack: [ref index]
            il.Emit(OpCodes.Dup); // stack: [ref index, ref index]
            il.Emit(OpCodes.Ldind_I4); // stack: [ref index, index]
            il.Emit(OpCodes.Ldc_I4_8); // stack: [ref index, index, 8]
            il.Emit(OpCodes.Add); // stack: [ref index, index + 8]
            il.Emit(OpCodes.Stind_I4); // index = index + 8
        }

        private static void EmitIncreaseIndexBy4(ILGenerator il)
        {
            LoadIndexByRef(il); // stack: [ref index]
            il.Emit(OpCodes.Dup); // stack: [ref index, ref index]
            il.Emit(OpCodes.Ldind_I4); // stack: [ref index, index]
            il.Emit(OpCodes.Ldc_I4_4); // stack: [ref index, index, 4]
            il.Emit(OpCodes.Add); // stack: [ref index, index + 4]
            il.Emit(OpCodes.Stind_I4); // index = index + 4
        }

        private static void EmitIncreaseIndexBy1(ILGenerator il)
        {
            LoadIndexByRef(il); // stack: [ref index]
            il.Emit(OpCodes.Dup); // stack: [ref index, ref index]
            il.Emit(OpCodes.Ldind_I4); // stack: [ref index, index]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [ref index, index, 1]
            il.Emit(OpCodes.Add); // stack: [ref index, index + 1]
            il.Emit(OpCodes.Stind_I4); // index = index + 1
        }

        private static void EmitIncreaseIndexBy2(ILGenerator il)
        {
            LoadIndexByRef(il); // stack: [ref index]
            il.Emit(OpCodes.Dup); // stack: [ref index, ref index]
            il.Emit(OpCodes.Ldind_I4); // stack: [ref index, index]
            il.Emit(OpCodes.Ldc_I4_2); // stack: [ref index, index, 2]
            il.Emit(OpCodes.Add); // stack: [ref index, index + 2]
            il.Emit(OpCodes.Stind_I4); // index = index + 2
        }

        private static void EmitGoToCurrentLocation(ILGenerator il)
        {
            LoadPinnedResult(il); // stack: [pinnedResult]
            LoadIndex(il); // stack: [pinnedResult, index]
            il.Emit(OpCodes.Add); // stack: [pinnedResult + index]
        }

        private static void EmitEnsureSize(ILGenerator il, LocalBuilder length, LocalBuilder desiredLength, LocalBuilder dest)
        {
            // stack: [size]
            LoadIndex(il); // stack: [size, index]
            il.Emit(OpCodes.Add); // stack: [index + size]
            il.Emit(OpCodes.Dup); // stack: [index + size, index + size]
            il.Emit(OpCodes.Stloc, desiredLength); // desiredLength = index + size; stack: [index + size]
            LoadResultByRef(il); // stack: [index + size, ref result]
            il.Emit(OpCodes.Ldind_Ref); // stack: [index + size, result]
            il.Emit(OpCodes.Ldlen); // stack: [index + size, result.Length]

            var sufficientLabel = il.DefineLabel();
            il.Emit(OpCodes.Ble, sufficientLabel); // if(index + size <= length) goto sufficient; stack: []
            // Resize
            LoadResultByRef(il); // stack: [ref result]
            il.Emit(OpCodes.Dup); // stack: [ref result, ref result]
            il.Emit(OpCodes.Ldind_Ref); // stack: [ref result, result]
            il.Emit(OpCodes.Ldlen); // stack: [ref result, result.Length]
            var cycleStart = il.DefineLabel();
            il.MarkLabel(cycleStart);
            il.Emit(OpCodes.Ldc_I4_1); // stack: [ref result, length, 1]
            il.Emit(OpCodes.Shl); // stack: [ref result, length << 1]
            il.Emit(OpCodes.Dup); // stack: [ref result, length << 1, length << 1]
            il.Emit(OpCodes.Ldloc, desiredLength); // stack: [ref result, length << 1, length << 1, desiredLength]
            il.Emit(OpCodes.Blt, cycleStart); // stack: [ref result, length]
            il.Emit(OpCodes.Newarr, typeof(byte)); // stack: [ref result, new byte[length]]
            il.Emit(OpCodes.Dup); // stack: [ref result, new byte[length], new byte[length]]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [ref result, new byte[length], new byte[length], 0]
            il.Emit(OpCodes.Ldelema, typeof(byte)); // stack: [ref result, new byte[length], &(new byte[length])[0]]
            il.Emit(OpCodes.Stloc, dest); // dest = &(new byte[length])[0]; stack: [ref result, new byte[length]]
            il.Emit(OpCodes.Ldloc, dest); // stack: [ref result, new byte[length], dest]
            LoadPinnedResult(il); // stack: [ref result, new byte[length], dest, pinnedResult]
            LoadIndex(il); // stack: [ref result, new byte[length], dest, pinnedResult, index]
            il.Emit(OpCodes.Unaligned, 1L);
            il.Emit(OpCodes.Cpblk); // dest[0 .. index - 1] = pinnedResult[0 .. index - 1]; stack: [ref result, new byte[length]]
            il.Emit(OpCodes.Stind_Ref); // result = new byte[length]; stack: []
            LoadPinnedResultByRef(il); // stack: [ref pinnedResult]
            il.Emit(OpCodes.Ldloc, dest); // stack: [ref pinnedResult, dest]
            il.Emit(OpCodes.Stind_I); // pinnedResult = dest
            il.Emit(OpCodes.Ldc_I4_0); // stack: [0]
            il.Emit(OpCodes.Conv_U); // stack: [(uint)0]
            il.Emit(OpCodes.Stloc, dest); // dest = 0
            il.MarkLabel(sufficientLabel);
        }

        private void EmitDateTimeValueWriter(Type type, ILGenerator il, out Delegate[] delegates)
        {
            il.Emit(OpCodes.Ldarg_0); // stack: [delegates]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [delegates, 0]
            il.Emit(OpCodes.Ldelem_Ref); // stack: [delegates[0]]
            LoadObjByRef(il); // stack: [delegates[0], &obj]
            il.Emit(OpCodes.Call, type.GetProperty("Ticks").GetGetMethod()); // stack: [delegates[0], obj.Ticks]
            LoadWriteEmpty(il); // stack: [delegates[0], obj.Value, writeEmpty]
            LoadResultByRef(il); // stack: [delegates[0], obj.Value, writeEmpty, ref result]
            LoadIndexByRef(il); // stack: [delegates[0], obj.Value, writeEmpty, ref result, ref index]
            LoadPinnedResultByRef(il); // stack: [delegates[0], obj.Value, writeEmpty, ref result, ref index, ref pinnedResult]
            var writerDelegate = GetWriterDelegate(typeof(long));
            delegates = new[] {writerDelegate};
            il.Emit(OpCodes.Call, writerDelegate.GetType().GetMethod("Invoke")); // writerDelegate.Write<long>(obj.Ticks, writeEmpty, ref result, ref index, ref pinnedResult)
        }

        private void EmitNullableValueWriter(Type type, ILGenerator il, out Delegate[] delegates)
        {
            il.Emit(OpCodes.Ldarg_0); // stack: [delegates]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [delegates, 0]
            il.Emit(OpCodes.Ldelem_Ref); // stack: [delegates[0]]
            LoadObjByRef(il); // stack: [delegates[0], &obj]
            il.Emit(OpCodes.Call, type.GetProperty("Value").GetGetMethod()); // stack: [delegates[0], obj.Value]
            LoadWriteEmpty(il); // stack: [delegates[0], obj.Value, writeEmpty]
            LoadResultByRef(il); // stack: [delegates[0], obj.Value, writeEmpty, ref result]
            LoadIndexByRef(il); // stack: [delegates[0], obj.Value, writeEmpty, ref result, ref index]
            LoadPinnedResultByRef(il); // stack: [delegates[0], obj.Value, writeEmpty, ref result, ref index, ref pinnedResult]
            var writerDelegate = GetWriterDelegate(type.GetGenericArguments()[0]);
            delegates = new[] {writerDelegate};
            il.Emit(OpCodes.Call, writerDelegate.GetType().GetMethod("Invoke")); // this.writerDelegate<elementType>(obj.Value, writeEmpty, ref result, ref index, ref pinnedResult)
        }

        private static void EmitPrimitiveValueWriter(Type type, ILGenerator il, LocalBuilder temp1, LocalBuilder temp2, LocalBuilder pinned)
        {
            var typeCode = GroBufHelpers.GetTypeCode(type);
            switch(typeCode)
            {
            case GroBufTypeCode.Int8:
            case GroBufTypeCode.UInt8:
                il.Emit(OpCodes.Ldc_I4_2);
                EmitEnsureSize(il, temp1, temp2, pinned);
                EmitGoToCurrentLocation(il); // stack: [&result[index]]
                il.Emit(OpCodes.Ldc_I4, (int)typeCode); // stack: [&result[index], typeCode]
                il.Emit(OpCodes.Stind_I1); // result[index] = typeCode
                EmitIncreaseIndexBy1(il); // index = index + 1
                EmitGoToCurrentLocation(il); // stack: [&result[index]]
                LoadObj(il); // stack: [&result[index], obj]
                il.Emit(OpCodes.Stind_I1); // result[index] = obj
                EmitIncreaseIndexBy1(il); // index = index + 1
                break;
            case GroBufTypeCode.Int16:
            case GroBufTypeCode.UInt16:
                il.Emit(OpCodes.Ldc_I4_3);
                EmitEnsureSize(il, temp1, temp2, pinned);
                EmitGoToCurrentLocation(il); // stack: [&result[index]]
                il.Emit(OpCodes.Ldc_I4, (int)typeCode); // stack: [&result[index], typeCode]
                il.Emit(OpCodes.Stind_I1); // result[index] = typeCode
                EmitIncreaseIndexBy1(il); // index = index + 1
                EmitGoToCurrentLocation(il); // stack: [&result[index]]
                LoadObj(il); // stack: [&result[index], obj]
                il.Emit(OpCodes.Stind_I2); // result[index] = obj
                EmitIncreaseIndexBy2(il); // index = index + 2
                break;
            case GroBufTypeCode.Int32:
            case GroBufTypeCode.UInt32:
                il.Emit(OpCodes.Ldc_I4_5);
                EmitEnsureSize(il, temp1, temp2, pinned);
                EmitGoToCurrentLocation(il); // stack: [&result[index]]
                il.Emit(OpCodes.Ldc_I4, (int)typeCode); // stack: [&result[index], typeCode]
                il.Emit(OpCodes.Stind_I1); // result[index] = typeCode
                EmitIncreaseIndexBy1(il); // index = index + 1
                EmitGoToCurrentLocation(il); // stack: [&result[index]]
                LoadObj(il); // stack: [&result[index], obj]
                il.Emit(OpCodes.Stind_I4); // result[index] = obj
                EmitIncreaseIndexBy4(il); // index = index + 4
                break;
            case GroBufTypeCode.Int64:
            case GroBufTypeCode.UInt64:
                il.Emit(OpCodes.Ldc_I4, 9);
                EmitEnsureSize(il, temp1, temp2, pinned);
                EmitGoToCurrentLocation(il); // stack: [&result[index]]
                il.Emit(OpCodes.Ldc_I4, (int)typeCode); // stack: [&result[index], typeCode]
                il.Emit(OpCodes.Stind_I1); // result[index] = typeCode
                EmitIncreaseIndexBy1(il); // index = index + 1
                EmitGoToCurrentLocation(il); // stack: [&result[index]]
                LoadObj(il); // stack: [&result[index], obj]
                il.Emit(OpCodes.Stind_I8); // result[index] = obj
                EmitIncreaseIndexBy8(il); // index = index + 8
                break;
            case GroBufTypeCode.Single:
                il.Emit(OpCodes.Ldc_I4_5);
                EmitEnsureSize(il, temp1, temp2, pinned);
                EmitGoToCurrentLocation(il); // stack: [&result[index]]
                il.Emit(OpCodes.Ldc_I4, (int)typeCode); // stack: [&result[index], typeCode]
                il.Emit(OpCodes.Stind_I1); // result[index] = typeCode
                EmitIncreaseIndexBy1(il); // index = index + 1
                EmitGoToCurrentLocation(il); // stack: [&result[index]]
                LoadObj(il); // stack: [&result[index], obj]
                il.Emit(OpCodes.Stind_R4); // result[index] = obj
                EmitIncreaseIndexBy4(il); // index = index + 4
                break;
            case GroBufTypeCode.Double:
                il.Emit(OpCodes.Ldc_I4, 9);
                EmitEnsureSize(il, temp1, temp2, pinned);
                EmitGoToCurrentLocation(il); // stack: [&result[index]]
                il.Emit(OpCodes.Ldc_I4, (int)typeCode); // stack: [&result[index], typeCode]
                il.Emit(OpCodes.Stind_I1); // result[index] = typeCode
                EmitIncreaseIndexBy1(il); // index = index + 1
                EmitGoToCurrentLocation(il); // stack: [&result[index]]
                LoadObj(il); // stack: [&result[index], obj]
                il.Emit(OpCodes.Stind_R8); // result[index] = obj
                EmitIncreaseIndexBy8(il); // index = index + 8
                break;
            default:
                throw new NotSupportedException();
            }
        }

        private static void EmitGuidValueWriter(ILGenerator il, LocalBuilder temp1, LocalBuilder temp2, LocalBuilder pinned)
        {
            il.Emit(OpCodes.Ldc_I4, 17); // stack: [17]
            EmitEnsureSize(il, temp1, temp2, pinned);
            EmitGoToCurrentLocation(il); // stack: [&result[index]]
            il.Emit(OpCodes.Ldc_I4, (int)GroBufTypeCode.Guid); // stack: [&result[index], typeCode.Guid]
            il.Emit(OpCodes.Stind_I1); // result[index] = typeCode.Guid
            EmitIncreaseIndexBy1(il); // index = index + 1
            EmitGoToCurrentLocation(il); // stack: [&result[index]]
            LoadObjByRef(il); // stack: [&result[index], &obj]
            il.Emit(OpCodes.Ldind_I8); // stack: [&result[index], (int64)*obj]
            il.Emit(OpCodes.Stind_I8); // result[index] = (int64)*obj
            EmitIncreaseIndexBy8(il); // index = index + 8
            EmitGoToCurrentLocation(il); // stack: [&result[index]]
            LoadObjByRef(il); // stack: [&result[index], &obj]
            il.Emit(OpCodes.Ldc_I4_8); // stack: [&result[index], &obj, 8]
            il.Emit(OpCodes.Add); // stack: [&result[index], &obj + 8]
            il.Emit(OpCodes.Ldind_I8); // stack: [&result[index], *(&obj+8)]
            il.Emit(OpCodes.Stind_I8); // result[index] = (int64)*(obj + 8)
            EmitIncreaseIndexBy8(il); // index = index + 8
        }

        private static void EmitStringValueWriter(ILGenerator il, LocalBuilder length, LocalBuilder temp2, LocalBuilder pinned)
        {
            LoadObj(il); // stack: [obj]
            il.Emit(OpCodes.Call, GetLengthPropertyGetter()); // stack: [obj.Length]
            il.Emit(OpCodes.Ldc_I4_1); // stack: [obj.Length, 1]
            il.Emit(OpCodes.Shl); // stack: [obj.Length << 1]
            il.Emit(OpCodes.Dup); // stack: [obj.Length << 1, obj.Length << 1]
            il.Emit(OpCodes.Ldc_I4_5); // stack: [obj.Length << 1, obj.Length << 1, 5]
            il.Emit(OpCodes.Add); // stack: [obj.Length << 1, obj.Length << 1 + 5]
            EmitEnsureSize(il, length, temp2, pinned);
            il.Emit(OpCodes.Stloc, length); // length = obj.Length << 1
            EmitGoToCurrentLocation(il); // stack: [&result[index]]
            il.Emit(OpCodes.Ldc_I4, (int)GroBufTypeCode.String); // stack: [&result[index], TypeCode.String]
            il.Emit(OpCodes.Stind_I1); // result[index] = TypeCode.String
            EmitIncreaseIndexBy1(il); // index = index + 1
            EmitGoToCurrentLocation(il); // stack: [&result[index]]
            il.Emit(OpCodes.Ldloc, length); // stack: [&result[index], length]
            il.Emit(OpCodes.Stind_I4); // result[index] = length
            EmitIncreaseIndexBy4(il); // index = index + 4

            EmitGoToCurrentLocation(il); // stack: [&result[index]]
            var str = il.DeclareLocal(typeof(string), true);
            LoadObj(il); // stack: [&result[index], obj]
            il.Emit(OpCodes.Stloc, str); // str = obj
            il.Emit(OpCodes.Ldloc, str); // stack: [&result[index], str]
            il.Emit(OpCodes.Conv_I); // stack: [&result[index], (int)str]
            il.Emit(OpCodes.Ldc_I4, RuntimeHelpers.OffsetToStringData); // stack: [&result[index], (IntPtr)str, offset]
            il.Emit(OpCodes.Add); // stack: [&result[index], (IntPtr)str + offset]
            il.Emit(OpCodes.Ldloc, length); // stack: [&result[index], (IntPtr)str + offset, length]
            il.Emit(OpCodes.Unaligned, 1L);
            il.Emit(OpCodes.Cpblk); // &result[index] = str
            il.Emit(OpCodes.Ldc_I4_0); // stack: [0]
            il.Emit(OpCodes.Conv_U); // stack: [(uint)0]
            il.Emit(OpCodes.Stloc, str); // str = (uint)0;

            LoadIndexByRef(il); // stack: [ref index]
            il.Emit(OpCodes.Dup); // stack: [ref index, ref index]
            il.Emit(OpCodes.Ldind_I4); // stack: [ref index, index]
            il.Emit(OpCodes.Ldloc, length); // stack: [ref index, index, length]
            il.Emit(OpCodes.Add); // stack: [ref index, index + length]
            il.Emit(OpCodes.Stind_I4); // index = index + length
        }

        private unsafe Delegate GetWriterDelegate(Type type)
        {
            if(getWriterMethod == null)
                getWriterMethod = ((MethodCallExpression)((Expression<Action<GroBufWriterOld>>)(grawWriter => grawWriter.GetWriter<int>())).Body).Method.GetGenericMethodDefinition();
            return ((Delegate)getWriterMethod.MakeGenericMethod(new[] {type}).Invoke(this, new object[0]));
        }

        private static MethodInfo GetIsNullOrEmptyMethod()
        {
            return isNullOrEmptyMethod ?? (isNullOrEmptyMethod = ((MethodCallExpression)((Expression<Func<string, bool>>)(s => string.IsNullOrEmpty(s))).Body).Method);
        }

        private static MethodInfo GetLengthPropertyGetter()
        {
            return lengthPropertyGetter ?? (lengthPropertyGetter = ((PropertyInfo)((MemberExpression)((Expression<Func<string, int>>)(s => s.Length)).Body).Member).GetGetMethod());
        }

        private static MethodInfo getWriterMethod;
        private static MethodInfo isNullOrEmptyMethod;
        private static MethodInfo lengthPropertyGetter;

        private readonly Hashtable writers = new Hashtable();
        private readonly object writersLock = new object();

        private readonly Hashtable pinningWriters = new Hashtable();
        private readonly object pinningWritersLock = new object();
    }
}