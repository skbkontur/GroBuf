using System;

namespace GroBuf
{
    public class HashCalculator
    {
        public HashCalculator(int maxLength)
        {
            this.maxLength = maxLength;
            randTable = InitRandTable(maxLength * 2);
        }

        public ulong CalcHash(string str)
        {
            if(str.Length > maxLength)
                throw new NotSupportedException(string.Format("Names with length greater than {0}", maxLength));
            ulong result = 0;
            for(int i = 0; i < str.Length; ++i)
            {
                result ^= randTable[2 * i][str[i] & 0xFF];
                result ^= randTable[2 * i + 1][(str[i] >> 8) & 0xFF];
            }
            return result;
        }

        private static ulong[][] InitRandTable(int count)
        {
            const int seed = 314159265; //NOTE не менять !!!
            var random = new GroBufRandom(seed);
            var randTable = new ulong[count][];
            for(int len = 0; len < count; ++len)
            {
                var arr = new ulong[256];
                for(int i = 0; i < arr.Length; ++i)
                    arr[i] = ((ulong)(random.Next() & 0xFFFFFF)) | (((ulong)(random.Next() & 0xFFFFFF)) << 24) | (((ulong)(random.Next() & 0xFFFFFF)) << 48);
                randTable[len] = arr;
            }
            return randTable;
        }

        private readonly int maxLength;
        private readonly ulong[][] randTable;
    }
}