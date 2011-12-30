using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf
{
    internal static class GroBufHelpers
    {
        public static GroBufTypeCode GetTypeCode(Type type)
        {
            switch(Type.GetTypeCode(type))
            {
            case TypeCode.Boolean:
                return GroBufTypeCode.UInt8;
            case TypeCode.Char:
                return GroBufTypeCode.UInt16;
            case TypeCode.SByte:
                return GroBufTypeCode.Int8;
            case TypeCode.Byte:
                return GroBufTypeCode.UInt8;
            case TypeCode.Int16:
                return GroBufTypeCode.Int16;
            case TypeCode.UInt16:
                return GroBufTypeCode.UInt16;
            case TypeCode.Int32:
                return GroBufTypeCode.Int32;
            case TypeCode.UInt32:
                return GroBufTypeCode.UInt32;
            case TypeCode.Int64:
                return GroBufTypeCode.Int64;
            case TypeCode.UInt64:
                return GroBufTypeCode.UInt64;
            case TypeCode.Single:
                return GroBufTypeCode.Single;
            case TypeCode.Double:
                return GroBufTypeCode.Double;
            case TypeCode.Decimal:
                return GroBufTypeCode.Decimal;
            case TypeCode.DateTime:
                return GroBufTypeCode.Int64;
            case TypeCode.String:
                return GroBufTypeCode.String;
            default:
                if(type == typeof(Guid))
                    return GroBufTypeCode.Guid;
                if(type.IsEnum)
                    return GroBufTypeCode.Enum;
                if(type.IsArray)
                    return GroBufTypeCode.Array;
                return GroBufTypeCode.Object;
            }
        }

        // TODO: write own random
        public static ulong CalcHash(string str)
        {
            var bytes = GetBytes(str);
            if(randTable.Count < bytes.Length)
                InitRandTable(bytes.Length);
            ulong result = 0;
            for(int i = 0; i < bytes.Length; ++i)
                result ^= randTable[i][bytes[i]];
            return result;
        }

        public static readonly int[] Lengths = BuildLengths();

        private delegate void CopyMemDelegate(IntPtr src, IntPtr dst, uint len);

        private static int[] BuildLengths()
        {
            var lengths = new int[256];
            var type = typeof(GroBufTypeCode);
            var fields = type.GetFields();
            foreach(var field in fields)
            {
                if(field.FieldType != type) continue;
                var attribute = (DataLengthAttribute)field.GetCustomAttributes(typeof(DataLengthAttribute), false).SingleOrDefault();
                if(attribute == null) throw new InvalidOperationException(string.Format("Data length of '{0}.{1}' must be specified", type, field));
                var length = attribute.Length;
                if(length < 0 && length != -1)
                    throw new InvalidOperationException("Data length must be either -1 or greater 0");
                lengths[(int)field.GetValue(dummy)] = length;
            }
            return lengths;
        }

        private static CopyMemDelegate EmitCopyMem()
        {
            var dynamicMethod = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(IntPtr), typeof(IntPtr), typeof(uint)}, typeof(GroBufHelpers).Module, true);

            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1); // dest
            il.Emit(OpCodes.Ldarg_0); // src
            il.Emit(OpCodes.Ldarg_2); // bytesCount
            il.Emit(OpCodes.Unaligned, 1L);
            il.Emit(OpCodes.Cpblk);

            il.Emit(OpCodes.Ret);
            return (CopyMemDelegate)dynamicMethod.CreateDelegate(typeof(CopyMemDelegate));
        }

        private static void InitRandTable(int count)
        {
            while(randTable.Count < count)
            {
                var arr = new ulong[256];
                for(int i = 0; i < arr.Length; ++i)
                    arr[i] = ((ulong)(random.Next() & 0xFFFFFF)) | (((ulong)(random.Next() & 0xFFFFFF)) << 24) | (((ulong)(random.Next() & 0xFFFFFF)) << 48);
                randTable.Add(arr);
            }
        }

        private static byte[] GetBytes(string str)
        {
            int length = str.Length << 1;
            var result = new byte[length];
            unsafe
            {
                fixed(char* s = str)
                {
                    fixed(byte* r = &result[0])
                        copyMem((IntPtr)s, (IntPtr)r, (uint)length);
                }
            }
            return result;
        }

        private static readonly object dummy = new object();

        private static readonly CopyMemDelegate copyMem = EmitCopyMem();

        private static readonly Random random = new Random(12345678);
        private static readonly List<ulong[]> randTable = new List<ulong[]>();
    }
}