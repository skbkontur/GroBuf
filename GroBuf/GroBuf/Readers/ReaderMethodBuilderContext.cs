using System;
using System.Collections.Generic;
using System.Reflection;

using GrEmit;
using GrEmit.Utils;

namespace GroBuf.Readers
{
    internal class ReaderMethodBuilderContext
    {
        public ReaderMethodBuilderContext(ReaderTypeBuilderContext context, GroboIL il, bool referenceType)
        {
            Context = context;
            Il = il;
            TypeCode = il.DeclareLocal(typeof(int));
            Length = il.DeclareLocal(typeof(uint));
            Index = referenceType ? il.DeclareLocal(typeof(int)) : null;
        }

        /// <summary>
        ///     Loads <c>pinnedData</c> onto the evaluation stack
        /// </summary>
        public void LoadData()
        {
            Il.Ldarg(0);
        }

        /// <summary>
        ///     Loads <c>ref index</c> onto the evaluation stack
        /// </summary>
        public void LoadIndexByRef()
        {
            Il.Ldarg(1);
        }

        /// <summary>
        ///     Loads <c>index</c> onto the evaluation stack
        /// </summary>
        public void LoadIndex()
        {
            Il.Ldarg(1);
            Il.Ldind(typeof(int));
        }

        /// <summary>
        ///     Loads <c>dataLength</c> onto the evaluation stack
        /// </summary>
        public void LoadDataLength()
        {
            Il.Ldarg(3);
            Il.Ldfld(typeof(ReaderContext).GetField("length", BindingFlags.Public | BindingFlags.Instance));
        }

        /// <summary>
        ///     Loads <c>context</c> onto the evaluation stack
        /// </summary>
        public void LoadContext()
        {
            Il.Ldarg(3);
        }

        /// <summary>
        ///     Loads <c>ref result</c> onto the evaluation stack
        /// </summary>
        public void LoadResultByRef()
        {
            Il.Ldarg(2);
        }

        /// <summary>
        ///     Loads <c>result</c> onto the evaluation stack
        /// </summary>
        public void LoadResult(Type resultType)
        {
            Il.Ldarg(2);
            Il.Ldind(resultType);
        }

        /// <summary>
        ///     Loads the specified field onto the evaluation stack
        /// </summary>
        /// <param name="field">Field to load</param>
        public void LoadField(FieldInfo field)
        {
            Il.Ldfld(field);
        }

        /// <summary>
        ///     Increases <c>index</c> by 1
        /// </summary>
        public void IncreaseIndexBy1()
        {
            LoadIndexByRef(); // stack: [ref index]
            LoadIndex(); // stack: [ref index, index]
            Il.Ldc_I4(1); // stack: [ref index, index, 1]
            Il.Add(); // stack: [ref index, index + 1]
            Il.Stind(typeof(int)); // index = index + 1
        }

        /// <summary>
        ///     Increases <c>index</c> by 2
        /// </summary>
        public void IncreaseIndexBy2()
        {
            LoadIndexByRef(); // stack: [ref index]
            LoadIndex(); // stack: [ref index, index]
            Il.Ldc_I4(2); // stack: [ref index, index, 2]
            Il.Add(); // stack: [ref index, index + 2]
            Il.Stind(typeof(int)); // index = index + 2
        }

        /// <summary>
        ///     Increases <c>index</c> by 4
        /// </summary>
        public void IncreaseIndexBy4()
        {
            LoadIndexByRef(); // stack: [ref index]
            LoadIndex(); // stack: [ref index, index]
            Il.Ldc_I4(4); // stack: [ref index, index, 4]
            Il.Add(); // stack: [ref index, index + 4]
            Il.Stind(typeof(int)); // index = index + 4
        }

        /// <summary>
        ///     Increases <c>index</c> by 8
        /// </summary>
        public void IncreaseIndexBy8()
        {
            LoadIndexByRef(); // stack: [ref index]
            LoadIndex(); // stack: [ref index, index]
            Il.Ldc_I4(8); // stack: [ref index, index, 8]
            Il.Add(); // stack: [ref index, index + 8]
            Il.Stind(typeof(int)); // index = index + 8
        }

        /// <summary>
        ///     Loads &amp;<c>data</c>[<c>index</c>] onto the evaluation stack
        /// </summary>
        public void GoToCurrentLocation()
        {
            LoadData(); // stack: [pinnedData]
            LoadIndex(); // stack: [pinnedData, index]
            Il.Add(); // stack: [pinnedData + index]
        }

        /// <summary>
        ///     Asserts that the specified number of bytes can be read from <c>data</c> starting at <c>index</c>
        ///     <para></para>
        ///     The number of bytes must be pushed onto the evaluation stack
        /// </summary>
        public void AssertLength()
        {
            LoadIndex(); // stack: [length, index]
            Il.Add(); // stack: [length + index]
            LoadDataLength(); // stack: [length + index, dataLength]
            var bigEnoughLabel = Il.DefineLabel("bigEnough");
            Il.Ble(bigEnoughLabel, true);
            Il.Ldstr("Unexpected end of data");
            var constructor = typeof(DataCorruptedException).GetConstructor(new[] {typeof(string)});
            if(constructor == null)
                throw new MissingConstructorException(typeof(DataCorruptedException), typeof(string));
            Il.Newobj(constructor);
            Il.Throw();
            Il.MarkLabel(bigEnoughLabel);
        }

        /// <summary>
        ///     Checks TypeCode and throws Exception if it is invalid
        /// </summary>
        public void CheckTypeCode()
        {
            LoadField(Context.Lengths);
            Il.Ldloc(TypeCode); // stack: [lengths, typeCode]
            Il.Ldelem(typeof(int)); // stack: [lengths[typeCode]]
            var okLabel = Il.DefineLabel("ok");
            Il.Brtrue(okLabel); // if(lengths[typeCode] != 0) goto ok;
            Il.Ldstr("Unknown type code: ");
            Il.Ldloca(TypeCode);
            Il.Call(HackHelpers.GetMethodDefinition<int>(x => x.ToString()), typeof(int));
            Il.Call(HackHelpers.GetMethodDefinition<string>(s => s + "zzz"));
            var constructor = typeof(DataCorruptedException).GetConstructor(new[] {typeof(string)});
            if(constructor == null)
                throw new MissingConstructorException(typeof(DataCorruptedException), typeof(string));
            Il.Newobj(constructor);
            Il.Throw();
            Il.MarkLabel(okLabel);
        }

        public void SkipValue()
        {
            LoadIndexByRef(); // stack: [ref index]
            LoadIndex(); // stack: [ref index, index]

            // todo: сделать switch
            Il.Ldloc(TypeCode); // stack: [ref index, index, TypeCode]
            Il.Ldc_I4((int)GroBufTypeCode.DateTimeOld); // stack: [ref index, index, TypeCode, GroBufTypeCode.DateTimeOld]
            var notDateTimeLabel = Il.DefineLabel("notDateTime");
            Il.Bne_Un(notDateTimeLabel); // if(TypeCode != GroBufTypeCode.DateTimeOld) goto notDateTime; stack: [ref index, index]
            Il.Ldc_I4(8); // stack: [ref index, index, 8]
            AssertLength();
            GoToCurrentLocation(); // stack: [ref index, index, &data[index]]
            Il.Ldc_I4(4);
            Il.Add();
            Il.Ldind(typeof(int)); // stack: [ref index, index, (int)(&data[index])]
            Il.Ldc_I4(31); // stack: [ref index, index, (int)&data[index], 31]
            Il.Shr(true); // stack: [ref index, index, (int)&data[index] >> 31]
            Il.Ldc_I4(8); // stack: [ref index, index, (int)&data[index] >> 31, 8]
            Il.Add(); // stack: [ref index, index, (int)&data[index] >> 31 + 8]
            var increaseLabel = Il.DefineLabel("increase");
            Il.Br(increaseLabel);

            Il.MarkLabel(notDateTimeLabel);
            LoadField(Context.Lengths);

            Il.Ldloc(TypeCode); // stack: [ref index, index, lengths, typeCode]
            Il.Ldelem(typeof(int)); // stack: [ref index, index, lengths[typeCode]]
            Il.Dup(); // stack: [ref index, index, lengths[typeCode], lengths[typeCode]]
            Il.Ldc_I4(-1); // stack: [ref index, index, lengths[typeCode], lengths[typeCode], -1]
            Il.Bne_Un(increaseLabel); // if(lengths[typeCode] != -1) goto increase;

            Il.Ldc_I4(4);
            AssertLength();
            Il.Pop(); // stack: [ref index, index]
            Il.Dup(); // stack: [ref index, index, index]
            LoadData(); // stack: [ref index, index, index, pinnedData]
            Il.Add(); // stack: [ref index, index, index + pinnedData]
            Il.Ldind(typeof(uint)); // stack: [ref index, index, *(uint*)(pinnedData + index)]
            Il.Ldc_I4(4); // stack: [ref index, index, *(uint*)(pinnedData + index), 4]
            Il.Add(); // stack: [ref index, *(uint*)(pinnedData + index) + 4]

            Il.MarkLabel(increaseLabel);
            Il.Dup(); // stack: [ref index, length, length]
            AssertLength(); // stack: [ref index, length]
            Il.Add(); // stack: [ref index, index + length]
            Il.Stind(typeof(int)); // index = index + length
        }

        public void AssertTypeCode(GroBufTypeCode expectedTypeCode)
        {
            Il.Ldloc(TypeCode); // stack: [typeCode]
            Il.Ldc_I4((int)expectedTypeCode); // stack: [typeCode, expectedTypeCode]

            var okLabel = Il.DefineLabel("ok");
            Il.Beq(okLabel);

            SkipValue();
            Il.Ret();

            Il.MarkLabel(okLabel);
        }

        public static void LoadReader(GroboIL il, Type type, ReaderTypeBuilderContext context)
        {
            var counter = context.GetReader(type);
            if (counter.Pointer != IntPtr.Zero)
                il.Ldc_IntPtr(counter.Pointer);
            else
            {
                il.Ldfld(context.ConstantsType.GetField("pointers", BindingFlags.Static | BindingFlags.NonPublic));
                il.Ldc_I4(counter.Index);
                il.Ldelem(typeof(IntPtr));
            }
        }

        public static void CallReader(GroboIL il, Type type, ReaderTypeBuilderContext context)
        {
            LoadReader(il, type, context);
            il.Calli(CallingConventions.Standard, typeof(void), new[] {typeof(IntPtr), typeof(int).MakeByRefType(), type.MakeByRefType(), typeof(ReaderContext)});
        }

        public void CallReader(Type type)
        {
            CallReader(Il, type, Context);
        }

        public void LoadReader(Type type)
        {
            LoadReader(Il, type, Context);
        }

        public void StoreObject(Type type)
        {
            if(Index == null) return;
            if(type.IsValueType)
                throw new InvalidOperationException("A reference type expected");
            // Store in array of all references
            LoadContext(); // stack: [context]
            Il.Ldfld(ReaderContext.ObjectsField); // stack: [context.objects]
            var doneLabel = Il.DefineLabel("done");
            Il.Brfalse(doneLabel); // if(context.objects == null) goto done; stack: []

            LoadContext(); // stack: [context]
            Il.Ldfld(ReaderContext.ObjectsField); // stack: [context.objects]
            Il.Ldloc(Index); // stack: [context.objects, index]
            LoadResult(type); // stack: [context.objects, index, result]
            Il.Call(HackHelpers.GetMethodDefinition<Dictionary<int, object>>(dict => dict.Add(0, null))); // context.objects.Add(index, result)
            Il.MarkLabel(doneLabel);
        }

        public ReaderTypeBuilderContext Context { get; private set; }
        public GroboIL Il { get; private set; }

        public GroboIL.Local TypeCode { get; private set; }
        public GroboIL.Local Length { get; private set; }
        public GroboIL.Local Index { get; private set; }
    }
}