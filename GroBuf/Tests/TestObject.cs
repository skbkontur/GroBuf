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
            serializer = new Serializer(new PropertiesExtractor());
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

        [Test]
        public void TestArray()
        {
            var o = new A {S = "zzz", B = new B {S = new object[] {(byte)100, "qxx"}}};
            byte[] data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            Assert.AreEqual("zzz", oo.S);
            var array = (Array)oo.B.S;
            Assert.NotNull(array);
            Assert.AreEqual(2, array.Length);
            Assert.AreEqual(100, array.GetValue(0));
            Assert.AreEqual("qxx", array.GetValue(1));
        }

        [Test]
        public void TestBad1()
        {
            var o = new A {S = "zzz", B = new B {S = new A {S = "qxx"}}, Z = "qxx"};
            byte[] data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            Assert.AreEqual("zzz", oo.S);
            Assert.AreEqual(null, oo.B.S);
            Assert.AreEqual("qxx", oo.Z);
        }

        [Test]
        public void TestBad2()
        {
            var o = new Az {S = "zzz", B = new Bz {S = new Cz {S = 100}}, Z = "qxx"};
            byte[] data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            Assert.AreEqual("zzz", oo.S);
            Assert.AreEqual(null, oo.B.S);
            Assert.AreEqual("qxx", oo.Z);
        }

        public class A
        {
            public object S { get; set; }
            public B B { get; set; }
            public object Z { get; set; }
        }

        public class Az
        {
            public object S { get; set; }
            public Bz B { get; set; }
            public string Z { get; set; }
        }

        public class Cz
        {
            public byte S { get; set; }
        }

        public struct B
        {
            public object S { get; set; }
        }

        public struct Bz
        {
            public Cz S { get; set; }
        }

        private Serializer serializer;
    }
}