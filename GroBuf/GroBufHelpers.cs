using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using GrEmit;

using GroBuf.DataMembersExtracters;

using Mono.Reflection;

namespace GroBuf
{
    public static class GroBufHelpers
    {
        static GroBufHelpers()
        {
            isMono = Type.GetType("Mono.Runtime") != null;
            ExtractDynamicMethodPointer = EmitDynamicMethodPointerExtractor();
            LeafTypes = BuildLeafTypes();
            LeafTypeHandles = LeafTypes.Select(type => type == null ? IntPtr.Zero : type.TypeHandle.Value).ToArray();
        }

        public static bool IsMono { get { return isMono; } }

        public static Type GetMemberType(this MemberInfo member)
        {
            switch(member.MemberType)
            {
            case MemberTypes.Property:
                return ((PropertyInfo)member).PropertyType;
            case MemberTypes.Field:
                return ((FieldInfo)member).FieldType;
            default:
                throw new NotSupportedException("Data member of type " + member.MemberType + " is not supported");
            }
        }

        public static MethodInfo GetMethod<TAttribute>(Type type)
        {
            MethodInfo result = type.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(method => method.GetCustomAttributes(typeof(TAttribute), true).Any());
            if(result != null)
                return result;
            return type.BaseType == typeof(object) ? null : GetMethod<TAttribute>(type.BaseType);
        }

        public static bool IsTuple(this Type type)
        {
            if(!type.IsGenericType)
                return false;
            type = type.GetGenericTypeDefinition();
            return type == typeof(Tuple<>) || type == typeof(Tuple<,>) || type == typeof(Tuple<,,>) || type == typeof(Tuple<,,,>)
                   || type == typeof(Tuple<,,,,>) || type == typeof(Tuple<,,,,,>) || type == typeof(Tuple<,,,,,,>)
                   || type == typeof(Tuple<,,,,,,>) || type == typeof(Tuple<,,,,,,,>);
        }

        public static ulong[] CalcHashesAndCheck(IEnumerable<IDataMember> dataMembers)
        {
            var dict = new Dictionary<ulong, MemberInfo>();
            foreach(var dataMember in dataMembers)
            {
                var hash = dataMember.Id ?? CalcHash(dataMember.Name);
                if(hash == 0)
                    throw new InvalidOperationException(string.Format("Hash code of '{0}.{1}' equals to zero", dataMember.Member.DeclaringType.Name, dataMember.Member.Name));
                if(dict.ContainsKey(hash))
                {
                    if(dict[hash] == dataMember.Member)
                        throw new InvalidOperationException(string.Format("Duplicated member '{0}.{1}'", dataMember.Member.DeclaringType.Name, dataMember.Member.Name));
                    throw new InvalidOperationException(string.Format("Hash code collision: members '{0}.{1}' and '{2}.{3}' have the same hash code = {4}", dataMember.Member.DeclaringType.Name, dataMember.Member.Name, dict[hash].DeclaringType.Name, dict[hash].Name, hash));
                }
                dict.Add(hash, dataMember.Member);
            }
            return dict.Keys.ToArray();
        }

        public static ulong CalcHash(string str)
        {
            return HashCalculator.CalcHash(str);
        }

        public static uint CalcSize(ulong[] values)
        {
            var hashSet = new HashSet<uint>();
            for(var n = Math.Max((uint)values.Length, 1);; ++n)
            {
                hashSet.Clear();
                bool ok = true;
                foreach(var x in values)
                {
                    var item = (uint)(x % n);
                    if(hashSet.Contains(item))
                    {
                        ok = false;
                        break;
                    }
                    hashSet.Add(item);
                }
                if(ok) return n;
            }
        }

        public static readonly IntPtr[] LeafTypeHandles;
        public static readonly Type[] LeafTypes;

        public static readonly int[] Lengths = BuildLengths();

        public static readonly Func<DynamicMethod, IntPtr> ExtractDynamicMethodPointer;
        public static readonly HashCalculator HashCalculator = new HashCalculator(Seed, 1000);
        public const int Seed = 314159265; //NOTE не менять !!!

        private static Func<DynamicMethod, IntPtr> EmitDynamicMethodPointerExtractor()
        {
            if (isMono)
            {
                return dynMethod =>
                {
                    var handle = dynMethod.MethodHandle;
                    RuntimeHelpers.PrepareMethod(handle);
                    return handle.GetFunctionPointer();
                };
            }
            var method = new DynamicMethod("DynamicMethodPointerExtractor", typeof(IntPtr), new[] {typeof(DynamicMethod)}, typeof(GroBufHelpers).Module, true);
            using (var il = new GroboIL(method))
            {
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
            }
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

        private static Type[] BuildLeafTypes()
        {
            Type type = typeof(GroBufTypeCode);
            FieldInfo[] fields = type.GetFields();
            var types = (from field in fields
                         where field.FieldType == type
                         select (LeafTypeAttribute)field.GetCustomAttributes(typeof(LeafTypeAttribute), false).SingleOrDefault()
                         into attribute
                         where attribute != null
                         select attribute.Type).ToList();
            var n = CalcSize(types.Select(x => (ulong)x.TypeHandle.Value.ToInt64()).ToArray());
            var result = new Type[n];
            foreach(var x in types)
                result[x.TypeHandle.Value.ToInt64() % n] = x;
            return result;
        }

        public static MemberInfo TryGetWritableMemberInfo(this MemberInfo memberInfo)
        {
            var propertyInfo = memberInfo as PropertyInfo;
            if(propertyInfo == null)
                return memberInfo;
            if(propertyInfo.CanWrite && propertyInfo.GetSetMethod(true).GetParameters().Length == 1)
                return memberInfo;
            try
            {
                return propertyInfo.GetBackingField();
            }
            catch
            {
            }
            return null;
        }

        private static readonly object dummy = new object();
        private static readonly bool isMono;
    }
}