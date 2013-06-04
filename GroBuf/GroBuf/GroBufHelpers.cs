using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using GrEmit;

namespace GroBuf
{
    public static class GroBufHelpers
    {
        public static MethodInfo GetMethod<TAttribute>(Type type)
        {
            MethodInfo result = type.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(method => method.GetCustomAttributes(typeof(TAttribute), true).Any());
            if(result != null)
                return result;
            return type.BaseType == typeof(object) ? null : GetMethod<TAttribute>(type.BaseType);
        }

        public static ulong[] CalcHashAndCheck(IEnumerable<string> strings)
        {
            var dict = new Dictionary<ulong, string>();
            foreach(var s in strings)
            {
                ulong hash = CalcHash(s);
                if(hash == 0)
                    throw new InvalidOperationException("Hash code of '" + s + "' equals to zero");
                if(dict.ContainsKey(hash))
                {
                    if(dict[hash] == s)
                        throw new InvalidOperationException("Duplicated string '" + s + "'");
                    throw new InvalidOperationException("Hash code collision: strings '" + s + "' and '" + dict[hash] + "' have the same hash code = '" + hash + "'");
                }
                dict.Add(hash, s);
            }
            return dict.Keys.ToArray();
        }

        public static ulong CalcHash(string str)
        {
            return HashCalculator.CalcHash(str);
        }

        public static readonly int[] Lengths = BuildLengths();

        public static readonly Func<DynamicMethod, IntPtr> ExtractDynamicMethodPointer = EmitDynamicMethodPointerExtractor();
        public static readonly HashCalculator HashCalculator = new HashCalculator(Seed, 1000);
        public const int Seed = 314159265; //NOTE не менять !!!

        private static Func<DynamicMethod, IntPtr> EmitDynamicMethodPointerExtractor()
        {
            var method = new DynamicMethod("DynamicMethodPointerExtractor", typeof(IntPtr), new[] {typeof(DynamicMethod)}, typeof(GroBufHelpers).Module, true);
            var il = new GroboIL(method);
            il.Ldarg(0); // stack: [dynamicMethod]
            MethodInfo getMethodDescriptorMethod = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.Instance | BindingFlags.NonPublic);
            if(getMethodDescriptorMethod == null)
                throw new MissingMethodException(typeof(DynamicMethod).Name, "GetMethodDescriptor");
            il.Call(getMethodDescriptorMethod); // stack: [dynamicMethod.GetMethodDescriptor()]
            var runtimeMethodHandle = il.DeclareLocal(typeof(RuntimeMethodHandle));
            il.Stloc(runtimeMethodHandle); // runtimeMethodHandle = dynamicMethod.GetMethodDescriptor(); stack: []
            il.Ldloc(runtimeMethodHandle); // stack: [runtimeMethodHandle]
            MethodInfo prepareMethodMethod = typeof(RuntimeHelpers).GetMethod("PrepareMethod", new[] {typeof(RuntimeMethodHandle)});
            if(prepareMethodMethod == null)
                throw new MissingMethodException(typeof(RuntimeHelpers).Name, "PrepareMethod");
            il.Call(prepareMethodMethod); // RuntimeHelpers.PrepareMethod(runtimeMethodHandle)
            MethodInfo getFunctionPointerMethod = typeof(RuntimeMethodHandle).GetMethod("GetFunctionPointer", BindingFlags.Instance | BindingFlags.Public);
            if(getFunctionPointerMethod == null)
                throw new MissingMethodException(typeof(RuntimeMethodHandle).Name, "GetFunctionPointer");
            il.Ldloca(runtimeMethodHandle); // stack: [&runtimeMethodHandle]
            il.Call(getFunctionPointerMethod); // stack: [runtimeMethodHandle.GetFunctionPointer()]
            il.Ret(); // return runtimeMethodHandle.GetFunctionPointer()
            return (Func<DynamicMethod, IntPtr>)method.CreateDelegate(typeof(Func<DynamicMethod, IntPtr>));
        }

        private static int[] BuildLengths()
        {
            var lengths = new int[256];
            Type type = typeof(GroBufTypeCode);
            FieldInfo[] fields = type.GetFields();
            foreach(var field in fields)
            {
                if(field.FieldType != type) continue;
                var attribute = (DataLengthAttribute)field.GetCustomAttributes(typeof(DataLengthAttribute), false).SingleOrDefault();
                if(attribute == null) throw new InvalidOperationException(string.Format("Data length of '{0}.{1}' must be specified", type, field));
                lengths[(int)field.GetValue(dummy)] = attribute.Length;
            }
            return lengths;
        }

        private static readonly object dummy = new object();
    }
}