using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestNull
    {
        private Serializer serializer;

        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer();
        }

        public class A
        {
            public string S { get; set; }
            public B B { get; set; }
        }

        public class B
        {
            public int[] Ints { get; set; }
        }

        [Test]
        public void TestClass()
        {
            var o = (A)null;
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            Assert.IsNotNull(oo);
            Assert.IsNull(oo.S);
            Assert.IsNull(oo.B);
        }
    }
}