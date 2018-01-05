using System;
using System.Reflection;

using GrEmit;

namespace GroBuf.SizeCounters
{
    internal class SizeCounterMethodBuilderContext
    {
        public SizeCounterMethodBuilderContext(SizeCounterBuilderContext context, GroboIL il)
        {
            Context = context;
            Il = il;
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
        ///     Loads <c>context</c> onto the evaluation stack
        /// </summary>
        public void LoadContext()
        {
            Il.Ldarg(2);
        }

        /// <summary>
        ///     Loads <c>context.serializerId</c> onto the evaluation stack
        /// </summary>
        public void LoadSerializerId()
        {
            LoadContext();
            Il.Ldfld(WriterContext.SerializerIdField);
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
        ///     <c>obj</c> is empty. Return (int)<c>writeEmpty</c>
        /// </summary>
        public void ReturnForNull()
        {
            LoadWriteEmpty(); // stack: [writeEmpty]
            Il.Ret();
        }

        public void CallSizeCounter(GroboIL il, Type type)
        {
            var counter = Context.GetCounter(type);
            if(counter.Pointer != IntPtr.Zero)
                il.Ldc_IntPtr(counter.Pointer);
            else
            {
                il.Ldfld(Context.ConstantsType.GetField("pointers", BindingFlags.Static | BindingFlags.NonPublic));
                il.Ldc_I4(counter.Index);
                il.Ldelem(typeof(IntPtr));
            }
            il.Calli(CallingConventions.Standard, typeof(int), new[] {type, typeof(bool), typeof(WriterContext)});
        }

        public void CallSizeCounter(Type type)
        {
            CallSizeCounter(Il, type);
        }

        public SizeCounterBuilderContext Context { get; private set; }
        public GroboIL Il { get; private set; }
    }
}