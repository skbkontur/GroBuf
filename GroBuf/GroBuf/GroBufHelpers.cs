using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
            foreach(string s in strings)
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
            return hashCalculator.CalcHash(str);
        }

        public static readonly int[] Lengths = BuildLengths();

        private static int[] BuildLengths()
        {
            var lengths = new int[256];
            Type type = typeof(GroBufTypeCode);
            FieldInfo[] fields = type.GetFields();
            foreach(FieldInfo field in fields)
            {
                if(field.FieldType != type) continue;
                var attribute = (DataLengthAttribute)field.GetCustomAttributes(typeof(DataLengthAttribute), false).SingleOrDefault();
                if(attribute == null) throw new InvalidOperationException(string.Format("Data length of '{0}.{1}' must be specified", type, field));
                int length = attribute.Length;
                if(length < 0 && length != -1)
                    throw new InvalidOperationException("Data length must be either -1 or greater 0");
                lengths[(int)field.GetValue(dummy)] = length;
            }
            return lengths;
        }

        private static readonly HashCalculator hashCalculator = new HashCalculator(1000);

        private static readonly object dummy = new object();
    }
}