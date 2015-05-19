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
        ///     Loads <c>obj</c> onto the evaluation stack
        /// </summary>
        public void LoadObj()
        {
            Il.Ldarg(0);
        }

        /// <summary>
        ///     Loads <c>ref obj</c> onto the evaluation stack
        /// </summary>
        public void LoadObjByRef()
        {
            Il.Ldarga(0);
        }

        /// <summary>
        ///     Loads <c>writeEmpty</c> onto the evaluation stack
        /// </summary>
        public void LoadWriteEmpty()
        {
            Il.Ldarg(1);
        }

        /// <summary>
        ///     Loads <c>result</c> onto the evaluation stack
        /// </summary>
        public void LoadResult()
        {
            Il.Ldarg(2);
        }

        /// <summary>
        ///     Loads <c>ref index</c> onto the evaluation stack
        /// </summary>
        public void LoadIndexByRef()
        {
            Il.Ldarg(3);
        }

        /// <summary>
        ///     Loads <c>index</c> onto the evaluation stack
        /// </summary>
        public void LoadIndex()
        {
            Il.Ldarg(3);
            Il.Ldind(typeof(int));
        }

        /// <summary>
        ///     Loads <c>context</c> onto the evaluation stack
        /// </summary>
        public void LoadContext()
        {
            Il.Ldarg(4);
        }

        /// <summary>
        ///     Loads <c>length</c> onto the evaluation stack
        /// </summary>
        public void LoadResultLength()
        {
            Il.Ldarg(4);
            Il.Ldfld(typeof(WriterContext).GetField("length", BindingFlags.Public | BindingFlags.Instance));
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
        ///     Loads &amp;<c>result</c>[<c>index</c>] onto the evaluation stack
        /// </summary>
        public void GoToCurrentLocation()
        {
            LoadResult(); // stack: [result]
            LoadIndex(); // stack: [result, index]
            Il.Add(); // stack: [result + index]
        }

        /// <summary>
        ///     Puts TypeCode.Empty in <c>result</c> if <c>writeEmpty</c> = true and returns
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
        ///     Asserts that the specified number of bytes can be written to <c>result</c> starting at <c>index</c>
        ///     <para></para>
        ///     The number of bytes must be pushed onto the evaluation stack
        /// </summary>
        public void AssertLength()
        {
            LoadIndex(); // stack: [length, index]
            Il.Add(); // stack: [length + index]
            LoadResultLength(); // stack: [length + index, resultLength]
            var bigEnoughLabel = Il.DefineLabel("bigEnough");
            Il.Ble(bigEnoughLabel, true);
            Il.Ldstr("Seems like the object being serialized has been changed during serialization");
            var constructor = typeof(InvalidOperationException).GetConstructor(new[] {typeof(string)});
            if(constructor == null)
                throw new MissingConstructorException(typeof(InvalidOperationException), typeof(string));
            Il.Newobj(constructor);
            Il.Throw();
            Il.MarkLabel(bigEnoughLabel);
        }

        /// <summary>
        ///     Puts the specified type code at <c>result</c>[<c>index</c>]
        /// </summary>
        /// <param name="typeCode">Type code to put</param>
        public void WriteTypeCode(GroBufTypeCode typeCode)
        {
            Il.Ldc_I4(1);
            AssertLength();
            GoToCurrentLocation(); // stack: [&result[index]]
            Il.Ldc_I4((int)typeCode); // stack: [&result[index], typeCode]
            Il.Stind(typeof(byte)); // result[index] = typeCode
            IncreaseIndexBy1(); // index = index + 1
        }

        public void CallWriter(GroboIL il, Type type)
        {
            var counter = Context.GetWriter(type);
            if(counter.Pointer != IntPtr.Zero)
                il.Ldc_IntPtr(counter.Pointer);
            else
            {
                il.Ldfld(Context.ConstantsType.GetField("pointers", BindingFlags.Static | BindingFlags.NonPublic));
                il.Ldc_I4(counter.Index);
                il.Ldelem(typeof(IntPtr));
            }
            il.Calli(CallingConventions.Standard, typeof(void), new[] {type, typeof(bool), typeof(IntPtr), typeof(int).MakeByRefType(), typeof(WriterContext)});
        }

        public void CallWriter(Type type)
        {
            CallWriter(Il, type);
        }

        public WriterTypeBuilderContext Context { get; private set; }
        public GroboIL Il { get; private set; }
        public GroboIL.Local LocalInt { get; private set; }
    }
}