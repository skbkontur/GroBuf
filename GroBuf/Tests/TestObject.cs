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
        public void Test()
        {
            var o = new A {S = "zzz", B = new B {S = (byte)100}};
            byte[] data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            Assert.AreEqual("zzz", oo.S);
            Assert.AreEqual(100, oo.B.S);
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