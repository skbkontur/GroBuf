using System;
using System.Reflection;

using GrEmit;

namespace GroBuf.SizeCounters
{
    internal class SizeCounterMethodBuilderContext
    {
        public SizeCounterMethodBuilderContext(SizeCounterBuilderContext context, GroboIL il, bool packReferences)
        {
            this.packReferences = packReferences;
            size = packReferences ? il.DeclareLocal(typeof(int)) : null;
            Context = context;
            Il = il;
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
        /// Loads <c>context</c> onto the evaluation stack
        /// </summary>
        public void LoadContext()
        {
            Il.Ldarg(2);
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
        /// <c>obj</c> is empty. Return (int)<c>writeEmpty</c>
        /// </summary>
        public void ReturnForNull()
        {
            LoadWriteEmpty(); // stack: [writeEmpty]
            UpdateIndex();
            Il.Ret();
        }

        public void UpdateIndex()
        {
            if(!packReferences) return;
            Il.Stloc(size);
            LoadContext(); // stack: [context]
            Il.Dup(); // stack: [context, context]
            Il.Ldfld(WriterContext.IndexField); // stack: [context, context.index]
            Il.Ldloc(size); // stack: [context, context.index, size]
            Il.Add(); // stack: [context, context.index + size]
            Il.Stfld(WriterContext.IndexField); // context.index += size; stack: []
            Il.Ldloc(size);
        }

//        public void LoadSizeCounter(Type type)
//        {
//            var counter = Context.GetCounter(type);
//            Il.Ldfld(Context.ConstantsType.GetField("delegates", BindingFlags.Static | BindingFlags.NonPublic));
//            Il.Ldc_I4(counter.Index);
//            Il.Ldelem(typeof(SizeCounterDelegate<>).MakeGenericType(type));
//        }

        public void CallSizeCounter(Type type)
        {
//            var delegateType = typeof(SizeCounterDelegate<>).MakeGenericType(type);
//            Il.Call(delegateType.GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance), delegateType);
            var counter = Context.GetCounter(type);
            if(counter.Pointer != IntPtr.Zero)
                Il.Ldc_IntPtr(counter.Pointer);
            else
            {
                Il.Ldfld(Context.ConstantsType.GetField("pointers", BindingFlags.Static | BindingFlags.NonPublic));
                Il.Ldc_I4(counter.Index);
                Il.Ldelem(typeof(IntPtr));
            }
            Il.Calli(CallingConventions.Standard, typeof(int), new[] {type, typeof(bool), typeof(WriterContext)});
        }

        public SizeCounterBuilderContext Context { get; private set; }
        public GroboIL Il { get; private set; }
        private readonly bool packReferences;
        private GroboIL.Local size;
    }
}