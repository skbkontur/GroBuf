using System;
using System.CodeDom;
using System.Linq;
using System.Reflection;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestLazy
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new AllPropertiesExtractor());
        }

        [Test]
        public void TestWrite1()
        {
            var o = new A {B = new Lazy<B>(() => new B {S = "zzz"})};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A_WithoutLazy>(data);
            Assert.IsNotNull(oo.B);
            Assert.That(oo.B.S, Is.EqualTo("zzz"));
        }

        private static long GetWriterId(Serializer serializer)
        {
            var grobufWriterType = typeof(Serializer).Assembly.GetTypes().Single(type => type.Name == "GroBufWriter");
            var writerField = typeof(Serializer).GetField("writer", BindingFlags.Instance | BindingFlags.NonPublic);
            var serializerIdField = grobufWriterType.GetField("serializerId", BindingFlags.Instance | BindingFlags.NonPublic);
            return (long)serializerIdField.GetValue(writerField.GetValue(serializer));
        }

        [Test]
        public void TestWrite2()
        {
            var o = new A { B = new Lazy<B>(new RawData<B>(GetWriterId(serializer), serializer.Serialize(new B { S = "qxx" }), bytes => serializer.Deserialize<B>(bytes)).GetValue) };
            var data = serializer.Serialize(o);
            Assert.That(o.B.IsValueCreated, Is.EqualTo(false));
            var oo = serializer.Deserialize<A_WithoutLazy>(data);
            Assert.IsNotNull(oo.B);
            Assert.That(oo.B.S, Is.EqualTo("qxx"));
        }

        [Test]
        public void TestWrite3()
        {
            var o = new A {B = new Lazy<B>(new RawData<B>(-1, serializer.Serialize(new B {S = "qxx"}), bytes => serializer.Deserialize<B>(bytes)).GetValue)};
            var data = serializer.Serialize(o);
            Assert.That(o.B.IsValueCreated, Is.EqualTo(true));
            var oo = serializer.Deserialize<A_WithoutLazy>(data);
            Assert.IsNotNull(oo.B);
            Assert.That(oo.B.S, Is.EqualTo("qxx"));
        }

        [Test]
        public void TestRead1()
        {
            var o = new A() {B = new Lazy<B>(() => new B {S = "zzz"})};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            var valueFactoryField = typeof(Lazy<B>).GetField("m_valueFactory", BindingFlags.Instance | BindingFlags.NonPublic);
            var targetField = typeof(Func<B>).GetField("_target", BindingFlags.Instance | BindingFlags.NonPublic);
            var dataField = typeof(RawData<B>).GetField("data", BindingFlags.Instance | BindingFlags.NonPublic);
            var func = (Func<B>)valueFactoryField.GetValue(oo.B);
            var target = targetField.GetValue(func);
            Assert.That(target, Is.InstanceOf<RawData<B>>());
            data = (byte[])dataField.GetValue(target);
            var b = serializer.Deserialize<B>(data);
            Assert.That(b.S, Is.EqualTo("zzz"));
            b = oo.B.Value;
            Assert.That(b.S, Is.EqualTo("zzz"));
        }

        private Serializer serializer;

        public class A
        {
            public Lazy<B> B { get; set; }
        }

        public class A_WithoutLazy
        {
            public B B { get; set; }
        }

        public class B
        {
            public string S { get; set; }
        }
    }
}