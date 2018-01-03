using GroBuf.DataMembersExtracters;
using GroBuf.Tests.TestTools;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestMultipleReadWrite
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor());
        }

        [Test]
        public void Test()
        {
            byte[] data = serializer.Serialize("zzz", 100, new[] {1, 2, 3});
            int index = 0;
            Assert.AreEqual("zzz", serializer.Deserialize<string>(data, ref index));
            Assert.AreEqual(100, serializer.Deserialize<int>(data, ref index));
            serializer.Deserialize<int[]>(data, ref index).AssertEqualsTo(new[] {1, 2, 3});
            Assert.AreEqual(index, data.Length);
        }

        [Test]
        public void TestBug()
        {
            byte[] data = serializer.Serialize(new A {Q = 1, Z = 2}, new B {X = 3, Y = 4});
            int index = 0;
            var aChanged = serializer.Deserialize<AChanged>(data, ref index);
            var bChanged = serializer.Deserialize<BChanged>(data, ref index);
            Assert.AreEqual(1, aChanged.Q);
            Assert.AreEqual(0, bChanged.Z);
            Assert.AreEqual(3, bChanged.X);
            Assert.AreEqual(4, bChanged.Y);
        }

        public class A
        {
            public int Q { get; set; }
            public int Z { get; set; }
        }

        public class B
        {
            public int X { get; set; }
            public int Y { get; set; }
        }

        public class AChanged
        {
            public int Q { get; set; }
        }

        public class BChanged
        {
            public int Z { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }

        private ISerializer serializer;
    }
}