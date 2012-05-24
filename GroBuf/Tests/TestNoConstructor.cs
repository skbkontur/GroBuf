using System;
using System.Diagnostics;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestNoConstructor
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer();
        }

        [Test]
        public void TestStupid()
        {
            byte[] serialize = serializer.Serialize(new CNoConstructor(2));
            var cNoConstructor = serializer.Deserialize<CNoConstructor>(serialize);
            Assert.AreEqual(2, cNoConstructor.A);
        }

        [Test]
        public void TestSpeed()
        {
            byte[] bytes = serializer.Serialize(new CNoConstructorCopy {A = 2});
            var cNoConstructor = serializer.Deserialize<CNoConstructorCopy>(bytes);
            Stopwatch w = Stopwatch.StartNew();
            for(int i = 0; i < iterations; i++)
            {
                cNoConstructor = serializer.Deserialize<CNoConstructorCopy>(bytes);
            }
            Console.WriteLine("total ms: " + w.ElapsedMilliseconds);
            Console.WriteLine(cNoConstructor.A);
            Assert.AreEqual(2, cNoConstructor.A);
        }

        [Test]
        public void TestSpeedNoCtor()
        {
            byte[] bytes = serializer.Serialize(new CNoConstructor(2));
            var cNoConstructor = serializer.Deserialize<CNoConstructor>(bytes);
            Stopwatch w = Stopwatch.StartNew();
            for(int i = 0; i < iterations; i++)
            {
                cNoConstructor = serializer.Deserialize<CNoConstructor>(bytes);
            }
            Console.WriteLine("total ms: " + w.ElapsedMilliseconds);
            Console.WriteLine(cNoConstructor.A);
            Assert.AreEqual(2, cNoConstructor.A);
        }

        public class CNoConstructor
        {
            public CNoConstructor(int a)
            {
                A = a;
            }

            public int A { get; set; }
        }

        public class CNoConstructorCopy
        {
            public int A { get; set; }
        }

        private Serializer serializer;
        private const int iterations = 10000000;
    }
}