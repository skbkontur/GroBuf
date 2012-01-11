using System;

namespace GroBuf
{
    internal class GroBufRandom
    {
        public GroBufRandom()
            : this(Environment.TickCount)
        {
        }

        public GroBufRandom(int seed)
        {
            m_seedArray = new int[56];
            int num4 = (seed == -0x80000000) ? 0x7fffffff : Math.Abs(seed);
            int num2 = 0x9a4ec86 - num4;
            m_seedArray[55] = num2;
            int num3 = 1;
            for(int i = 1; i < 55; i++)
            {
                int index = (21 * i) % 55;
                m_seedArray[index] = num3;
                num3 = num2 - num3;
                if(num3 < 0)
                    num3 += 0x7fffffff;
                num2 = m_seedArray[index];
            }
            for(int j = 1; j < 5; j++)
            {
                for(int k = 1; k < 56; k++)
                {
                    m_seedArray[k] -= m_seedArray[1 + ((k + 30) % 55)];
                    if(m_seedArray[k] < 0)
                        m_seedArray[k] += 0x7fffffff;
                }
            }
            m_inext = 0;
            m_inextp = 21;
        }

        public int Next()
        {
            int inext = m_inext;
            int inextp = m_inextp;
            if(++inext >= 56)
                inext = 1;
            if(++inextp >= 56)
                inextp = 1;
            int num = m_seedArray[inext] - m_seedArray[inextp];
            if(num == 0x7fffffff)
                num--;
            if(num < 0)
                num += 0x7fffffff;
            m_seedArray[inext] = num;
            m_inext = inext;
            m_inextp = inextp;
            return num;
        }

        private int m_inext;
        private int m_inextp;
        private readonly int[] m_seedArray;
    }
}