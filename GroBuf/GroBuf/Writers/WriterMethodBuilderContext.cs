using System.Reflection;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf.Writers
{
    internal class WriterMethodBuilderContext
    {
        public WriterMethodBuilderContext(WriterTypeBuilderContext context, ILGenerator il)
        {
            Context = context;
            Il = il;
            LocalInt = il.DeclareLocal(typeof(int));
            pinnedBuf = il.DeclareLocal(typeof(byte).MakeByRefType(), true);
        }

        /// <summary>
        /// Loads <c>obj</c> onto the evaluation stack
        /// </summary>
        public void LoadObj()
        {
            Il.Emit(OpCodes.Ldarg_0);
        }

        /// <summary>
        /// Loads <c>ref obj</c> onto the evaluation stack
        /// </summary>
        public void LoadObjByRef()
        {
            Il.Emit(OpCodes.Ldarga_S, 0);
        }

        /// <summary>
        /// Loads <c>writeEmpty</c> onto the evaluation stack
        /// </summary>
        public void LoadWriteEmpty()
        {
            Il.Emit(OpCodes.Ldarg_1);
        }

        /// <summary>
        /// Loads <c>ref result</c> onto the evaluation stack
        /// </summary>
        public void LoadResultByRef()
        {
            Il.Emit(OpCodes.Ldarg_2);
        }

        /// <summary>
        /// Loads <c>ref index</c> onto the evaluation stack
        /// </summary>
        public void LoadIndexByRef()
        {
            Il.Emit(OpCodes.Ldarg_3);
        }

        /// <summary>
        /// Loads <c>index</c> onto the evaluation stack
        /// </summary>
        public void LoadIndex()
        {
            Il.Emit(OpCodes.Ldarg_3);
            Il.Emit(OpCodes.Ldind_I4);
        }

        /// <summary>
        /// Loads <c>ref pinnedResult</c> onto the evaluation stack
        /// </summary>
        public void LoadPinnedResultByRef()
        {
            Il.Emit(OpCodes.Ldarg_S, 4);
        }

        /// <summary>
        /// Loads <c>pinnedResult</c> onto the evaluation stack
        /// </summary>
        public void LoadPinnedResult()
        {
            Il.Emit(OpCodes.Ldarg_S, 4);
            Il.Emit(OpCodes.Ldind_I);
        }

        /// <summary>
        /// Loads the specified field onto the evaluation stack
        /// </summary>
        /// <param name="field">Field to load</param>
        public void LoadField(FieldInfo field)
        {
            Il.Emit(OpCodes.Ldnull);
            Il.Emit(OpCodes.Ldfld, field);
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
        /// Loads &amp;<c>result</c>[<c>index</c>] onto the evaluation stack
        /// </summary>
        public void GoToCurrentLocation()
        {
            LoadPinnedResult(); // stack: [pinnedResult]
            LoadIndex(); // stack: [pinnedResult, index]
            Il.Emit(OpCodes.Add); // stack: [pinnedResult + index]
        }

        /// <summary>
        /// Makes sure that the specified number of bytes can be put in <c>result</c> starting at <c>index</c>
        /// <para></para>
        /// The number of bytes must be pushed onto the evaluation stack
        /// </summary>
        public void EnsureSize()
        {
            var desiredLength = LocalInt;
            var dest = pinnedBuf;
            // stack: [size]
            LoadIndex(); // stack: [size, index]
            Il.Emit(OpCodes.Add); // stack: [index + size]
            Il.Emit(OpCodes.Dup); // stack: [index + size, index + size]
            Il.Emit(OpCodes.Stloc, desiredLength); // desiredLength = index + size; stack: [index + size]
            LoadResultByRef(); // stack: [index + size, ref result]
            Il.Emit(OpCodes.Ldind_Ref); // stack: [index + size, result]
            Il.Emit(OpCodes.Ldlen); // stack: [index + size, result.Length]

            var sufficientLabel = Il.DefineLabel();
            Il.Emit(OpCodes.Ble, sufficientLabel); // if(index + size <= length) goto sufficient; stack: []
            // Resize
            LoadResultByRef(); // stack: [ref result]
            Il.Emit(OpCodes.Dup); // stack: [ref result, ref result]
            Il.Emit(OpCodes.Ldind_Ref); // stack: [ref result, result]
            Il.Emit(OpCodes.Ldlen); // stack: [ref result, result.Length]
            var cycleStart = Il.DefineLabel();
            Il.MarkLabel(cycleStart);
            Il.Emit(OpCodes.Ldc_I4_1); // stack: [ref result, length, 1]
            Il.Emit(OpCodes.Shl); // stack: [ref result, length << 1]
            Il.Emit(OpCodes.Dup); // stack: [ref result, length << 1, length << 1]
            Il.Emit(OpCodes.Ldloc, desiredLength); // stack: [ref result, length << 1, length << 1, desiredLength]
            Il.Emit(OpCodes.Blt, cycleStart); // stack: [ref result, length]
            Il.Emit(OpCodes.Newarr, typeof(byte)); // stack: [ref result, new byte[length]]
            Il.Emit(OpCodes.Dup); // stack: [ref result, new byte[length], new byte[length]]
            Il.Emit(OpCodes.Ldc_I4_0); // stack: [ref result, new byte[length], new byte[length], 0]
            Il.Emit(OpCodes.Ldelema, typeof(byte)); // stack: [ref result, new byte[length], &(new byte[length])[0]]
            Il.Emit(OpCodes.Stloc, dest); // dest = &(new byte[length])[0]; stack: [ref result, new byte[length]]
            Il.Emit(OpCodes.Ldloc, dest); // stack: [ref result, new byte[length], dest]
            LoadPinnedResult(); // stack: [ref result, new byte[length], dest, pinnedResult]
            LoadIndex(); // stack: [ref result, new byte[length], dest, pinnedResult, index]
            Il.Emit(OpCodes.Unaligned, 1L);
            Il.Emit(OpCodes.Cpblk); // dest[0 .. index - 1] = pinnedResult[0 .. index - 1]; stack: [ref result, new byte[length]]
            Il.Emit(OpCodes.Stind_Ref); // result = new byte[length]; stack: []
            LoadPinnedResultByRef(); // stack: [ref pinnedResult]
            Il.Emit(OpCodes.Ldloc, dest); // stack: [ref pinnedResult, dest]
            Il.Emit(OpCodes.Stind_I); // pinnedResult = dest
            Il.Emit(OpCodes.Ldc_I4_0); // stack: [0]
            Il.Emit(OpCodes.Conv_U); // stack: [(uint)0]
            Il.Emit(OpCodes.Stloc, dest); // dest = 0
            Il.MarkLabel(sufficientLabel);
        }

        /// <summary>
        /// Puts TypeCode.Empty in <c>result</c> if <c>writeEmpty</c> = true and returns
        /// </summary>
        public void WriteNull()
        {
            var retLabel = Il.DefineLabel();
            LoadWriteEmpty(); // stack: [writeEmpty]
            Il.Emit(OpCodes.Brfalse, retLabel); // if(!writeEmpty) goto ret;
            Il.Emit(OpCodes.Ldc_I4_1); // stack: [1]
            EnsureSize();
            WriteTypeCode(GroBufTypeCode.Empty);
            Il.MarkLabel(retLabel);
            Il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Puts the specified type code at <c>result</c>[<c>index</c>]
        /// </summary>
        /// <param name="typeCode">Type code to put</param>
        public void WriteTypeCode(GroBufTypeCode typeCode)
        {
            GoToCurrentLocation(); // stack: [&result[index]]
            Il.Emit(OpCodes.Ldc_I4, (int)typeCode); // stack: [&result[index], typeCode]
            Il.Emit(OpCodes.Stind_I1); // result[index] = typeCode
            IncreaseIndexBy1(); // index = index + 1
        }

        public WriterTypeBuilderContext Context { get; private set; }
        public ILGenerator Il { get; private set; }
        public LocalBuilder LocalInt { get; private set; }
        private readonly LocalBuilder pinnedBuf;
    }
}