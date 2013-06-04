using System;
using System.Reflection;

using GrEmit;

namespace GroBuf.Writers
{
    internal class WriterMethodBuilderContext
    {
        public WriterMethodBuilderContext(WriterTypeBuilderContext context, GroboIL il)
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
            Il.Ldarg(0);
        }

        /// <summary>
        /// Loads <c>ref obj</c> onto the evaluation stack
        /// </summary>
        public void LoadObjByRef()
        {
            Il.Ldarga(0);
        }

        /// <summary>
        /// Loads <c>writeEmpty</c> onto the evaluation stack
        /// </summary>
        public void LoadWriteEmpty()
        {
            Il.Ldarg(1);
        }

        /// <summary>
        /// Loads <c>result</c> onto the evaluation stack
        /// </summary>
        public void LoadResult()
        {
            Il.Ldarg(2);
        }

        /// <summary>
        /// Loads <c>ref index</c> onto the evaluation stack
        /// </summary>
        public void LoadIndexByRef()
        {
            Il.Ldarg(3);
        }

        /// <summary>
        /// Loads <c>index</c> onto the evaluation stack
        /// </summary>
        public void LoadIndex()
        {
            Il.Ldarg(3);
            Il.Ldind(typeof(int));
        }

        /// <summary>
        /// Loads the specified field onto the evaluation stack
        /// </summary>
        /// <param name="field">Field to load</param>
        public void LoadField(FieldInfo field)
        {
            Il.Ldfld(field);
        }

        /// <summary>
        /// Increases <c>index</c> by 1
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
        /// Increases <c>index</c> by 2
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
        /// Increases <c>index</c> by 4
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
        /// Increases <c>index</c> by 8
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
        /// Loads &amp;<c>result</c>[<c>index</c>] onto the evaluation stack
        /// </summary>
        public void GoToCurrentLocation()
        {
            LoadResult(); // stack: [result]
            LoadIndex(); // stack: [result, index]
            Il.Add(); // stack: [result + index]
        }

        /// <summary>
        /// Puts TypeCode.Empty in <c>result</c> if <c>writeEmpty</c> = true and returns
        /// </summary>
        public void WriteNull()
        {
            var retLabel = Il.DefineLabel("return");
            LoadWriteEmpty(); // stack: [writeEmpty]
            Il.Brfalse(retLabel); // if(!writeEmpty) goto ret;
            WriteTypeCode(GroBufTypeCode.Empty);
            Il.MarkLabel(retLabel);
            Il.Ret();
        }

        /// <summary>
        /// Puts the specified type code at <c>result</c>[<c>index</c>]
        /// </summary>
        /// <param name="typeCode">Type code to put</param>
        public void WriteTypeCode(GroBufTypeCode typeCode)
        {
            GoToCurrentLocation(); // stack: [&result[index]]
            Il.Ldc_I4((int)typeCode); // stack: [&result[index], typeCode]
            Il.Stind(typeof(byte)); // result[index] = typeCode
            IncreaseIndexBy1(); // index = index + 1
        }

        public void CallWriter(Type type)
        {
            var counter = Context.GetWriter(type);
            if(counter.Pointer != IntPtr.Zero)
                Il.Ldc_IntPtr(counter.Pointer);
            else
            {
                Il.Ldfld(Context.ConstantsType.GetField("pointers", BindingFlags.Static | BindingFlags.NonPublic));
                Il.Ldc_I4(counter.Index);
                Il.Ldelem(typeof(IntPtr));
            }
            Il.Calli(CallingConventions.Standard, typeof(void), new[] {type, typeof(bool), typeof(IntPtr), typeof(int).MakeByRefType()});
        }

        public WriterTypeBuilderContext Context { get; private set; }
        public GroboIL Il { get; private set; }
        public GroboIL.Local LocalInt { get; private set; }
    }
}