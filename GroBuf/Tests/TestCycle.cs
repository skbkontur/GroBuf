using GroBuf.DataMembersExtractors;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestCycle
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor());
        }

        [Test]
        public void Test1()
        {
            var o = new A {S = "First", Next = new A {S = "Second", Next = new A {S = "Third"}}};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            Assert.AreEqual("First", oo.S);
            Assert.IsNotNull(oo.Next);
            Assert.AreEqual("Second", oo.Next.S);
            Assert.IsNotNull(oo.Next.Next);
            Assert.AreEqual("Third", oo.Next.Next.S);
            Assert.IsNull(oo.Next.Next.Next);
        }

        [Test]
        public void Test2()
        {
            var o = new B {S = "First", C = new C {S = "Second", B = new B {S = "Third"}}};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<B>(data);
            Assert.AreEqual("First", oo.S);
            Assert.IsNotNull(oo.C);
            Assert.AreEqual("Second", oo.C.S);
            Assert.IsNotNull(oo.C.B);
            Assert.AreEqual("Third", oo.C.B.S);
            Assert.IsNull(oo.C.B.C);
        }

        public class A
        {
            public string S { get; set; }
            public A Next { get; set; }
        }

        public class B
        {
            public string S { get; set; }
            public C C { get; set; }
        }

        public class C
        {
            public string S { get; set; }
            public B B { get; set; }
        }

        private Serializer serializer;
    }
}