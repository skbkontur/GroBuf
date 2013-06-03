using System;
using System.Reflection;
using System.Runtime.Serialization;

using GrEmit;
using GrEmit.Utils;

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

        public static void EmitConstructionOfType(Type type, GroboIL il)
        {
            ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
            if(constructor != null)
                il.Newobj(constructor);
            else
            {
                il.Ldtoken(type);
                il.Call(getTypeFromHandle);
                il.Call(getUninitializedObject);
                if(type.IsValueType)
                    il.Unbox_Any(type);
            }
        }

        public static Func<object> ConstructType(Type type)
        {
            return EmitHelpers.EmitDynamicMethod<Func<object>>("Construct_" + type.Name + "_" + Guid.NewGuid(), type.Module, il => EmitCode(type, il));
        }

        private static void EmitCode(Type type, GroboIL groboIl)
        {
            EmitConstructionOfType(type, groboIl);
        }

        private static readonly MethodInfo getTypeFromHandle;
        private static readonly MethodInfo getUninitializedObject;
    }
}