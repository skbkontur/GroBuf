using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SKBKontur.GroBuf
{
    internal static class GroBufHelpers
    {
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

        private delegate void CopyMemDelegate(IntPtr src, IntPtr dst, uint len);

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

        private static readonly CopyMemDelegate copyMem = EmitCopyMem();

        private static readonly Random random = new Random(12345678);
        private static readonly List<ulong[]> randTable = new List<ulong[]>();
    }
}