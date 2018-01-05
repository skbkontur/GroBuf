using System;

namespace GroBuf
{
    public class HashCalculator
    {
        public HashCalculator(int seed, int maxLength)
        {
            this.seed = seed;
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

        private ulong[][] InitRandTable(int count)
        {
            var random = new GroBufRandom(seed);
            var table = new ulong[count][];
            for(int len = 0; len < count; ++len)
            {
                var arr = new ulong[256];
                for(int i = 0; i < arr.Length; ++i)
                    arr[i] = ((ulong)(random.Next() & 0xFFFFFF)) | (((ulong)(random.Next() & 0xFFFFFF)) << 24) | (((ulong)(random.Next() & 0xFFFFFF)) << 48);
                table[len] = arr;
            }
            return table;
        }

        private readonly int seed;
        private readonly int maxLength;
        private readonly ulong[][] randTable;
    }
}