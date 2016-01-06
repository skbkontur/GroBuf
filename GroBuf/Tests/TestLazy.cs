using System;
using System.Collections.Generic;
using System.Reflection;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestLazy
    {
        private Serializer serializer;

        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new AllPropertiesExtractor());
        }

        [Test]
        public void TestWrite1()
        {
            var o = new A {B = new GroBufLazy<B>(new B {S = "zzz"})};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A_WithoutLazy>(data);
            Assert.IsNotNull(oo.B);
            Assert.That(oo.B.S, Is.EqualTo("zzz"));
        }

        [Test]
        public void TestWrite2()
        {
            var o = new A {B = new GroBufLazy<B>(serializer.Serialize(new B{S = "qxx"}), null)};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A_WithoutLazy>(data);
            Assert.IsNotNull(oo.B);
            Assert.That(oo.B.S, Is.EqualTo("qxx"));
        }

        [Test]
        public void TestRead1()
        {
            var o = new A() {B = new GroBufLazy<B>(new B {S = "zzz"})};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            var rawField = typeof(GroBufLazy<B>).GetField("raw", BindingFlags.Instance | BindingFlags.NonPublic);
            var dataField = typeof(GroBufLazy<B>).GetField("data", BindingFlags.Instance | BindingFlags.NonPublic);
            var valueField = typeof(GroBufLazy<B>).GetField("value", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(rawField.GetValue(oo.B), Is.True);
            data = (byte[])dataField.GetValue(oo.B);
            var b = serializer.Deserialize<B>(data);
            Assert.That(b.S, Is.EqualTo("zzz"));
            Assert.IsNull(valueField.GetValue(oo.B));
            b = oo.B.Value;
            Assert.That(b.S, Is.EqualTo("zzz"));
            Assert.That(rawField.GetValue(oo.B), Is.False);
        }

        [Test]
        public void TestRead2()
        {
            var o = new C{Dict = new GroBufLazy<Dictionary<string, string>>(new Dictionary<string, string>{{"zzz", "1"}})};
            var oo = serializer.Copy(o);
            var rawField = typeof(GroBufLazy<Dictionary<string, string>>).GetField("raw", BindingFlags.Instance | BindingFlags.NonPublic);
            var dataField = typeof(GroBufLazy<Dictionary<string, string>>).GetField("data", BindingFlags.Instance | BindingFlags.NonPublic);
            var valueField = typeof(GroBufLazy<Dictionary<string, string>>).GetField("value", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(rawField.GetValue(oo.Dict), Is.True);
            Assert.That(valueField.GetValue(oo.Dict), Is.Null);
        }

        public class A
        {
            public GroBufLazy<B> B { get; set; }
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
            public GroBufLazy<Dictionary<string, string>> Dict { get; set; }
        }
    }
}