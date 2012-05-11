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
            serializer = new Serializer();
        }

        [Test]
        public void Test()
        {
            var data = serializer.Serialize("zzz", 100, new[] {1, 2, 3});
            int index = 0;
            Assert.AreEqual("zzz", serializer.Deserialize<string>(data, ref index));
            Assert.AreEqual(100, serializer.Deserialize<int>(data, ref index));
            serializer.Deserialize<int[]>(data, ref index).AssertEqualsTo(new[] {1, 2, 3});
        }

        private ISerializer serializer;
    }
}