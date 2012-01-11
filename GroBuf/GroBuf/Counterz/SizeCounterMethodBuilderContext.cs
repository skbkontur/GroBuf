using System.Reflection;
using System.Reflection.Emit;

namespace GroBuf.Counterz
{
    internal class SizeCounterMethodBuilderContext
    {
        public SizeCounterMethodBuilderContext(SizeCounterTypeBuilderContext context, ILGenerator il)
        {
            Context = context;
            Il = il;
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
        /// Loads the specified field onto the evaluation stack
        /// </summary>
        /// <param name="field">Field to load</param>
        public void LoadField(FieldInfo field)
        {
            Il.Emit(OpCodes.Ldnull);
            Il.Emit(OpCodes.Ldfld, field);
        }

        /// <summary>
        /// <c>obj</c> is empty. Return (int)<c>writeEmpty</c>
        /// </summary>
        public void ReturnForNull()
        {
            LoadWriteEmpty(); // stack: [writeEmpty]
            Il.Emit(OpCodes.Ret);
        }

        public SizeCounterTypeBuilderContext Context { get; private set; }
        public ILGenerator Il { get; private set; }
    }
}