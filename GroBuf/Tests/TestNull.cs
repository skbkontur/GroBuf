using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestNull
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new SerializerImpl();
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

        public class A
        {
            public string S { get; set; }
            public B B { get; set; }
        }

        public class B
        {
            public int[] Ints { get; set; }
        }

        private Serializer serializer;
    }
}