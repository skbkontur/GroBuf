using System;
using System.Threading;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestGetHashMultiThread
    {
        [Test]
        public void Test()
        {
            const int numberOfThreads = 2;
            for(int i = 0; i < numberOfThreads - 1; ++i)
                new Thread(Zzz).Start();
            Zzz();
            Assert.IsFalse(wasBug);
        }

        public void Zzz()
        {
            try
            {
                for(int i = 0; i < 10000; ++i)
                    GroBufHelpers.CalcHash(new string('z', i + 1));
            }
            catch(Exception)
            {
                wasBug = true;
                return;
            }
        }

        private volatile bool wasBug;
    }
}