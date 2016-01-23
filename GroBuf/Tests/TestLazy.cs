using System;
using System.Linq;
using System.Reflection;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestLazy
    {
        // TODO test value types
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new AllPropertiesExtractor());
        }

        [Test]
        public void Test_WriteLazyReadWithoutLazy()
        {
            var o = new A {B = new Lazy<B>(() => new B {S = "zzz"})};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A_WithoutLazy>(data);
            Assert.IsNotNull(oo.B);
            Assert.That(oo.B.S, Is.EqualTo("zzz"));
        }

        [Test]
        public void Test_WriteReadSameSerializer()
        {
            var o = new A { B = new Lazy<B>(() => new B { S = "qxx" }) };
            var data = serializer.Serialize(o);
            o = serializer.Deserialize<A>(data);
            data = serializer.Serialize(o);
            Assert.That(o.B.IsValueCreated, Is.EqualTo(false));
            var oo = serializer.Deserialize<A_WithoutLazy>(data);
            Assert.IsNotNull(oo.B);
            Assert.That(oo.B.S, Is.EqualTo("qxx"));
        }

        [Test]
        public void Test_WriteReadDifferentSerializers()
        {
            var o = new A { B = new Lazy<B>(() => new B { S = "qxx" }) };
            var data = serializer.Serialize(o);
            o = serializer.Deserialize<A>(data);
            var serializer2 = new Serializer(new AllPropertiesExtractor());
            data = serializer2.Serialize(o);
            Assert.That(o.B.IsValueCreated, Is.EqualTo(true));
            var oo = serializer2.Deserialize<A_WithoutLazy>(data);
            Assert.IsNotNull(oo.B);
            Assert.That(oo.B.S, Is.EqualTo("qxx"));
        }

        [Test]
        public void Test_ReadLazyAsRawData()
        {
            var o = new A() {B = new Lazy<B>(() => new B {S = "zzz"})};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            var valueFactoryField = typeof(Lazy<B>).GetField("m_valueFactory", BindingFlags.Instance | BindingFlags.NonPublic);
            var targetField = typeof(Func<B>).GetField("_target", BindingFlags.Instance | BindingFlags.NonPublic);
            var rawDataType = typeof(Serializer).Assembly.GetTypes().Single(type => type.Name == "RawData`1");
            var dataField = rawDataType.MakeGenericType(typeof(B)).GetField("data", BindingFlags.Instance | BindingFlags.NonPublic);
            var func = (Func<B>)valueFactoryField.GetValue(oo.B);
            var target = targetField.GetValue(func);
            Assert.That(target, Is.InstanceOf(rawDataType.MakeGenericType(typeof(B))));
            data = (byte[])dataField.GetValue(target);
            var b = serializer.Deserialize<B>(data);
            Assert.That(b.S, Is.EqualTo("zzz"));
            b = oo.B.Value;
            Assert.That(b.S, Is.EqualTo("zzz"));
        }

        [Test]
        public void Test_WriteLazyWithoutFactory()
        {
            var o = new A() { B = new Lazy<B>() };
            var data = serializer.Serialize(o);
        }

        [Test]
        public void Test_ValueType()
        {
            var guid = Guid.NewGuid();
            var o = new C {Guid = new Lazy<Guid>(() => guid)};
            var data = serializer.Serialize(o);
            Console.WriteLine(DebugViewBuilder.DebugView(data));
            var oo = serializer.Deserialize<C>(data);
            Assert.That(oo.Guid.Value, Is.EqualTo(guid));
        }

        private static long GetWriterId(Serializer serializer)
        {
            var grobufWriterType = typeof(Serializer).Assembly.GetTypes().Single(type => type.Name == "GroBufWriter");
            var writerField = typeof(Serializer).GetField("writer", BindingFlags.Instance | BindingFlags.NonPublic);
            var serializerIdField = grobufWriterType.GetField("serializerId", BindingFlags.Instance | BindingFlags.NonPublic);
            return (long)serializerIdField.GetValue(writerField.GetValue(serializer));
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

        public class C
        {
            public Lazy<Guid> Guid { get; set; }
        }
    }
}