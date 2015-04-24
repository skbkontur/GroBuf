using System;

using GroBuf.DataMembersExtractors;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestProperties
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor());
        }

        [Test]
        public void Test()
        {
            var o = new A {Bool = true, Ints = new[] {10, 2, 4}, B = new B {S = "zzz", Long = 123456789123456789, DateTime = new DateTime(1234567890123, DateTimeKind.Local)}};
            byte[] data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            Assert.AreEqual(true, oo.Bool);
            Assert.IsNotNull(oo.Ints);
            Assert.AreEqual(3, oo.Ints.Length);
            Assert.AreEqual(10, oo.Ints[0]);
            Assert.AreEqual(2, oo.Ints[1]);
            Assert.AreEqual(4, oo.Ints[2]);
            Assert.IsNotNull(oo.B);
            Assert.AreEqual("zzz", oo.B.S);
            Assert.AreEqual(123456789123456789, oo.B.Long);
            Assert.AreEqual(1234567890123, oo.B.DateTime.Ticks);
        }

        [Test]
        public void TestEmpty()
        {
            var o = new B {S = "zzz"};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<C>(data);
            Assert.IsNotNull(oo);
            data = serializer.Serialize(oo);
            o = serializer.Deserialize<B>(data);
            Assert.IsNotNull(o);
            Assert.IsNull(o.S);
            Assert.IsNull(o.Long);
            Assert.AreEqual(default(DateTime), o.DateTime);
        }

        public class A
        {
            public int[] Ints { get; set; }
            public bool? Bool { get; set; }
            public B B { get; set; }
        }

        public class B
        {
            public string S { get; set; }
            public long? Long { get; set; }
            public DateTime DateTime { get; set; }
        }

        public class C
        {
        }

        private Serializer serializer;
    }
}