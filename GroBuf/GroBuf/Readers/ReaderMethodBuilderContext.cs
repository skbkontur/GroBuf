using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.Readers
{
    internal class ReaderMethodBuilderContext
    {
        public ReaderMethodBuilderContext(ReaderTypeBuilderContext context, ILGenerator il)
        {
            Context = context;
            Il = il;
            TypeCode = il.DeclareLocal(typeof(int));
            Length = il.DeclareLocal(typeof(uint));
        }

        /// <summary>
        /// Loads <c>pinnedData</c> onto the evaluation stack
        /// </summary>
        public void LoadData()
        {
            Il.Emit(OpCodes.Ldarg_0);
        }

        /// <summary>
        /// Loads <c>ref index</c> onto the evaluation stack
        /// </summary>
        public void LoadIndexByRef()
        {
            Il.Emit(OpCodes.Ldarg_1);
        }

        /// <summary>
        /// Loads <c>index</c> onto the evaluation stack
        /// </summary>
        public void LoadIndex()
        {
            Il.Emit(OpCodes.Ldarg_1);
            Il.Emit(OpCodes.Ldind_I4);
        }

        /// <summary>
        /// Loads <c>dataLength</c> onto the evaluation stack
        /// </summary>
        public void LoadDataLength()
        {
            Il.Emit(OpCodes.Ldarg_2);
        }

        /// <summary>
        /// Loads <c>ref result</c> onto the evaluation stack
        /// </summary>
        public void LoadResultByRef()
        {
            Il.Emit(OpCodes.Ldarg_3);
        }

        /// <summary>
        /// Loads <c>result</c> onto the evaluation stack
        /// </summary>
        public void LoadResult()
        {
            Il.Emit(OpCodes.Ldarg_3);
            Il.Emit(OpCodes.Ldind_Ref);
        }

        /// <summary>
        /// Loads the specified field onto the evaluation stack
        /// </summary>
        /// <param name="field">Field to load</param>
        public void LoadField(FieldInfo field)
        {
            Il.Emit(OpCodes.Ldsfld, field);
        }

        /// <summary>
        /// Increases <c>index</c> by 1
        /// </summary>
        public void IncreaseIndexBy1()
        {
            LoadIndexByRef(); // stack: [ref index]
            LoadIndex(); // stack: [ref index, index]
            Il.Emit(OpCodes.Ldc_I4_1); // stack: [ref index, index, 1]
            Il.Emit(OpCodes.Add); // stack: [ref index, index + 1]
            Il.Emit(OpCodes.Stind_I4); // index = index + 1
        }

        /// <summary>
        /// Increases <c>index</c> by 2
        /// </summary>
        public void IncreaseIndexBy2()
        {
            LoadIndexByRef(); // stack: [ref index]
            LoadIndex(); // stack: [ref index, index]
            Il.Emit(OpCodes.Ldc_I4_2); // stack: [ref index, index, 2]
            Il.Emit(OpCodes.Add); // stack: [ref index, index + 2]
            Il.Emit(OpCodes.Stind_I4); // index = index + 2
        }

        /// <summary>
        /// Increases <c>index</c> by 4
        /// </summary>
        public void IncreaseIndexBy4()
        {
            LoadIndexByRef(); // stack: [ref index]
            LoadIndex(); // stack: [ref index, index]
            Il.Emit(OpCodes.Ldc_I4_4); // stack: [ref index, index, 4]
            Il.Emit(OpCodes.Add); // stack: [ref index, index + 4]
            Il.Emit(OpCodes.Stind_I4); // index = index + 4
        }

        /// <summary>
        /// Increases <c>index</c> by 8
        /// </summary>
        public void IncreaseIndexBy8()
        {
            LoadIndexByRef(); // stack: [ref index]
            LoadIndex(); // stack: [ref index, index]
            Il.Emit(OpCodes.Ldc_I4_8); // stack: [ref index, index, 8]
            Il.Emit(OpCodes.Add); // stack: [ref index, index + 8]
            Il.Emit(OpCodes.Stind_I4); // index = index + 8
        }

        /// <summary>
        /// Loads &amp;<c>data</c>[<c>index</c>] onto the evaluation stack
        /// </summary>
        public void GoToCurrentLocation()
        {
            LoadData(); // stack: [pinnedData]
            LoadIndex(); // stack: [pinnedData, index]
            Il.Emit(OpCodes.Add); // stack: [pinnedData + index]
        }

        /// <summary>
        /// Asserts that the specified number of bytes can be read from <c>data</c> starting at <c>index</c>
        /// <para></para>
        /// The number of bytes must be pushed onto the evaluation stack
        /// </summary>
        public void AssertLength()
        {
            LoadIndex(); // stack: [length, index]
            Il.Emit(OpCodes.Add); // stack: [length + index]
            LoadDataLength(); // stack: [length + index, dataLength]
            var label = Il.DefineLabel();
            Il.Emit(OpCodes.Ble_Un, label);
            Il.Emit(OpCodes.Ldstr, "Unexpected end of data");
            var constructor = typeof(DataCorruptedException).GetConstructor(new[] {typeof(string)});
            if(constructor == null)
                throw new MissingConstructorException(typeof(DataCorruptedException), typeof(string));
            Il.Emit(OpCodes.Newobj, constructor);
            Il.Emit(OpCodes.Throw);
            Il.MarkLabel(label);
        }

        /// <summary>
        /// Checks TypeCode and throws Exception if it is invalid
        /// </summary>
        public void CheckTypeCode()
        {
            LoadField(Context.Lengths);
            Il.Emit(OpCodes.Ldloc, TypeCode); // stack: [lengths, typeCode]
            Il.Emit(OpCodes.Ldelem_I4); // stack: [lengths[typeCode]]
            var okLabel = Il.DefineLabel();
            Il.Emit(OpCodes.Brtrue, okLabel); // if(lengths[typeCode] != 0) goto ok;
            Il.Emit(OpCodes.Ldstr, "Unknown type code");
            var constructor = typeof(DataCorruptedException).GetConstructor(new[] {typeof(string)});
            if(constructor == null)
                throw new MissingConstructorException(typeof(DataCorruptedException), typeof(string));
            Il.Emit(OpCodes.Newobj, constructor);
            Il.Emit(OpCodes.Throw);
            Il.MarkLabel(okLabel);
        }

        public void SkipValue()
        {
            LoadIndexByRef(); // stack: [ref index]
            LoadIndex(); // stack: [ref index, index]

            // todo: сделать switch
            Il.Emit(OpCodes.Ldloc, TypeCode); // stack: [ref index, index, TypeCode]
            Il.Emit(OpCodes.Ldc_I4_S, (byte)GroBufTypeCode.DateTime); // stack: [ref index, index, TypeCode, GroBufTypeCode.DateTime]
            var notDateTimeLabel = Il.DefineLabel();
            Il.Emit(OpCodes.Bne_Un, notDateTimeLabel); // if(TypeCode != GroBufTypeCode.DateTime) goto notDateTime; stack: [ref index, index]
            Il.Emit(OpCodes.Ldc_I4_8); // stack: [ref index, index, 8]
            AssertLength();
            GoToCurrentLocation(); // stack: [ref index, index, &data[index]]
            Il.Emit(OpCodes.Ldc_I4_4);
            Il.Emit(OpCodes.Add);
            Il.Emit(OpCodes.Ldind_I4); // stack: [ref index, index, (int)(&data[index])]
            Il.Emit(OpCodes.Ldc_I4, 31); // stack: [ref index, index, (int)&data[index], 31]
            Il.Emit(OpCodes.Shr_Un); // stack: [ref index, index, (int)&data[index] >> 31]
            Il.Emit(OpCodes.Ldc_I4_8); // stack: [ref index, index, (int)&data[index] >> 31, 8]
            Il.Emit(OpCodes.Add); // stack: [ref index, index, (int)&data[index] >> 31 + 8]
            var increaseLabel = Il.DefineLabel();
            Il.Emit(OpCodes.Br, increaseLabel);

            Il.MarkLabel(notDateTimeLabel);
            LoadField(Context.Lengths);

            Il.Emit(OpCodes.Ldloc, TypeCode); // stack: [ref index, index, lengths, typeCode]
            Il.Emit(OpCodes.Ldelem_I4); // stack: [ref index, index, lengths[typeCode]]
            Il.Emit(OpCodes.Dup); // stack: [ref index, index, lengths[typeCode], lengths[typeCode]]
            Il.Emit(OpCodes.Ldc_I4_M1); // stack: [ref index, index, lengths[typeCode], lengths[typeCode], -1]
            Il.Emit(OpCodes.Bne_Un, increaseLabel); // if(lengths[typeCode] != -1) goto increase;

            Il.Emit(OpCodes.Ldc_I4_4);
            AssertLength();
            Il.Emit(OpCodes.Pop); // stack: [ref index, index]
            Il.Emit(OpCodes.Dup); // stack: [ref index, index, index]
            LoadData(); // stack: [ref index, index, index, pinnedData]
            Il.Emit(OpCodes.Add); // stack: [ref index, index, index + pinnedData]
            Il.Emit(OpCodes.Ldind_U4); // stack: [ref index, index, *(uint*)(pinnedData + index)]
            Il.Emit(OpCodes.Ldc_I4_4); // stack: [ref index, index, *(uint*)(pinnedData + index), 4]
            Il.Emit(OpCodes.Add); // stack: [ref index, *(uint*)(pinnedData + index) + 4]

            Il.MarkLabel(increaseLabel);
            Il.Emit(OpCodes.Dup); // stack: [ref index, length, length]
            AssertLength(); // stack: [ref index, length]
            Il.Emit(OpCodes.Add); // stack: [ref index, index + length]
            Il.Emit(OpCodes.Stind_I4); // index = index + length
        }

        public void AssertTypeCode(GroBufTypeCode expectedTypeCode)
        {
            Il.Emit(OpCodes.Ldloc, TypeCode); // stack: [typeCode]
            Il.Emit(OpCodes.Ldc_I4, (int)expectedTypeCode); // stack: [typeCode, expectedTypeCode]

            var label = Il.DefineLabel();
            Il.Emit(OpCodes.Beq, label);

            SkipValue();
            Il.Emit(OpCodes.Ret);

            Il.MarkLabel(label);
        }

        public ReaderTypeBuilderContext Context { get; private set; }
        public ILGenerator Il { get; private set; }

        public LocalBuilder TypeCode { get; private set; }
        public LocalBuilder Length { get; private set; }
    }
}