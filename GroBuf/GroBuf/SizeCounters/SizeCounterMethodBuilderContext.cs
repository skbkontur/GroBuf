using System.Reflection;

using GrEmit;

namespace GroBuf.SizeCounters
{
    internal class SizeCounterMethodBuilderContext
    {
        public SizeCounterMethodBuilderContext(SizeCounterTypeBuilderContext context, GroboIL il)
        {
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
            Il.Ret();
        }

        public SizeCounterTypeBuilderContext Context { get; private set; }
        public GroboIL Il { get; private set; }
    }
}