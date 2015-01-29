using System;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestHashCompatibility
    {
        [Test]
        public void TestValues()
        {
            HashCalculator hashCalculator = GroBufHelpers.HashCalculator;
            Assert.AreEqual(4840038476803469422L, hashCalculator.CalcHash("zzz"));
            Assert.AreEqual(815294827239093585L, hashCalculator.CalcHash("Prop"));
            Assert.AreEqual(2001271914681603707L, hashCalculator.CalcHash("1234567890"));
            Assert.AreEqual(14727305760438073051L, hashCalculator.CalcHash("TRASH"));
            Assert.AreEqual(5580606048428013952L, hashCalculator.CalcHash("trash"));
            Assert.AreEqual(0, hashCalculator.CalcHash(""));
        }

        [Test]
        public void TestCalcHash()
        {
            Console.WriteLine(GroBufHelpers.HashCalculator.CalcHash("X"));
        }

        [Test]
        public void TestMaxLenNotAffectsHash()
        {
            var h20 = new HashCalculator(GroBufHelpers.Seed, 20);
            var h10 = new HashCalculator(GroBufHelpers.Seed, 10);
            Assert.AreEqual(h20.CalcHash("qxx"), h10.CalcHash("qxx"));
        }

        [Test]
        public void TestLimits()
        {
            var h = new HashCalculator(GroBufHelpers.Seed, 3);
            Assert.AreEqual(17829415923593557195L, h.CalcHash("z"));
            Assert.AreEqual(1402044078693333494L, h.CalcHash("zz"));
            Assert.AreEqual(4840038476803469422L, h.CalcHash("zzz"));
            try
            {
                Assert.AreEqual(4840038476803469422L, h.CalcHash("zzzz"));
                Assert.Fail("NO CRASH");
            }
            catch(NotSupportedException e)
            {
                Assert.AreEqual("Names with length greater than 3", e.Message);
            }
        }
    }
}