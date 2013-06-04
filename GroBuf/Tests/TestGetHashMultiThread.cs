using System;
using System.Threading.Tasks;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestGetHashMultiThread
    {
        [Test]
        public void Test()
        {
            wasBug = false;
            Parallel.Invoke(Zzz, Zzz, Zzz, Zzz, Zzz, Zzz, Zzz);
            Assert.IsFalse(wasBug);
        }

        private void Zzz()
        {
            try
            {
                for(int i = 1; i < 1000; ++i)
                    GroBufHelpers.CalcHash(new string('z', i));
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                wasBug = true;
            }
        }

        private volatile bool wasBug;
    }
}