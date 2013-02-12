using System;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestDateTime
    {
        private SerializerImpl serializer;

        [SetUp]
        public void SetUp()
        {
            serializer = new SerializerImpl(new PropertiesExtractor());
        }

        [Test]
        public void TestUtc()
        {
            var o = new DateTime(12735641765, DateTimeKind.Utc);
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<DateTime>(data);
            Assert.AreEqual(DateTimeKind.Utc, oo.Kind);
            Assert.AreEqual(12735641765, oo.Ticks);
        }

        [Test]
        public void TestLocal()
        {
            var o = new DateTime(12735641765, DateTimeKind.Local);
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<DateTime>(data);
            Assert.AreEqual(DateTimeKind.Local, oo.Kind);
            Assert.AreEqual(12735641765, oo.Ticks);
        }

        [Test]
        public void TestUnspecified()
        {
            var o = new DateTime(12735641765, DateTimeKind.Unspecified);
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<DateTime>(data);
            Assert.AreEqual(DateTimeKind.Unspecified, oo.Kind);
            Assert.AreEqual(12735641765, oo.Ticks);
        }

    }
}