using System;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestObject
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new SerializerImpl(new PropertiesExtracter());
        }

        [Test]
        public void Test1()
        {
            var o = new A {S = "zzz", B = new B {S = (byte)100}};
            byte[] data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            Assert.AreEqual("zzz", oo.S);
            Assert.AreEqual(100, oo.B.S);
        }

        [Test]
        public void Test2()
        {
            var o = new A {S = new Guid("aae41ff9-a337-4a69-b2ee-12ead4669201"), B = new B {S = new DateTime(182374682)}};
            byte[] data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            Assert.AreEqual(new Guid("aae41ff9-a337-4a69-b2ee-12ead4669201"), oo.S);
            Assert.AreEqual(new DateTime(182374682), oo.B.S);
        }

        public class A
        {
            public object S { get; set; }
            public B B { get; set; }
        }

        public struct B
        {
            public object S { get; set; }
        }

        private SerializerImpl serializer;
    }
}