using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace GroBuf.Readers
{
    public class ObjectConstructionHelper
    {
        static ObjectConstructionHelper()
        {
            getTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static);
            if(getTypeFromHandle == null)
                throw new MissingMethodException("GetTypeFromHandle");
            getUninitializedObject = typeof(FormatterServices).GetMethod("GetUninitializedObject", BindingFlags.Public | BindingFlags.Static);
            if(getUninitializedObject == null)
                throw new MissingMethodException("GetUninitializedObject");
        }

        public static void EmitConstructionOfType(Type type, ILGenerator il)
        {
            ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
            if(constructor == null)
            {
                il.Emit(OpCodes.Ldtoken, type);
                il.Emit(OpCodes.Call, getTypeFromHandle);
                il.Emit(OpCodes.Call, getUninitializedObject);
                OpCode castOpcode = type.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass;
                il.Emit(castOpcode, type);
            }
            else
                il.Emit(OpCodes.Newobj, constructor);
        }

        private static readonly MethodInfo getTypeFromHandle;
        private static readonly MethodInfo getUninitializedObject;
    }
}