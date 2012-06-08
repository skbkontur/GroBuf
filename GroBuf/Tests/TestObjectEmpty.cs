using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestObjectEmpty
    {
        private Serializer serializer;

        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer();
        }

        [Test]
        public void TestEmptyRoot()
        {
            var data = serializer.Serialize(new A());
            var obj = serializer.Deserialize<A>(data);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.S);
            Assert.IsNull(obj.B);
        }

        [Test]
        public void TestEmptyProp()
        {
            var data = serializer.Serialize(new A{B = new B{C = new C()}});
            var obj = serializer.Deserialize<A>(data);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.S);
            Assert.IsNull(obj.B);
        }

        [Test]
        public void TestEmptyElementInArray()
        {
            var data = serializer.Serialize(new A{B = new B{Cs = new[] {new C() }}});
            var obj = serializer.Deserialize<A>(data);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj.S);
            Assert.IsNotNull(obj.B);
            Assert.IsNotNull(obj.B.Cs);
            CollectionAssert.AreEqual(new C[] {null}, obj.B.Cs);
        }

        public class A
        {
            public string S { get; set; }
            public B B { get; set; }
        }

        public class B
        {
            public string S { get; set; }
            public C C { get; set; }
            public C[] Cs { get; set; }
        }

        public class C
        {
            public string S { get; set; }
        }
    }
}