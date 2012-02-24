using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.Writers
{
    internal class WriterMethodBuilderContext
    {
        public WriterMethodBuilderContext(WriterTypeBuilderContext context, ILGenerator il)
        {
            Context = context;
            Il = il;
            LocalInt = il.DeclareLocal(typeof(int));
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
        /// Loads <c>result</c> onto the evaluation stack
        /// </summary>
        public void LoadResult()
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
            LoadResult(); // stack: [result]
            LoadIndex(); // stack: [result, index]
            Il.Emit(OpCodes.Add); // stack: [result + index]
        }

        /// <summary>
        /// Puts TypeCode.Empty in <c>result</c> if <c>writeEmpty</c> = true and returns
        /// </summary>
        public void WriteNull()
        {
            var retLabel = Il.DefineLabel();
            LoadWriteEmpty(); // stack: [writeEmpty]
            Il.Emit(OpCodes.Brfalse, retLabel); // if(!writeEmpty) goto ret;
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
    }
}