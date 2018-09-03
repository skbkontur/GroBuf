using System;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestDateTime
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor());
        }

        [Test]
        public void TestSize()
        {
            var dateTime = new DateTime(2010, 1, 1);
            var size = serializer.GetSize(dateTime);
            Assert.AreEqual(9, size);
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
        public void TestLong()
        {
            var o = 12735641765;
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<DateTime>(data);
            Assert.AreEqual(DateTimeKind.Utc, oo.Kind);
            Assert.AreEqual(12735641765, oo.Ticks);
        }

        [Test]
        public void TestLong2()
        {
            var data = serializer.Serialize(long.MinValue);
            Assert.Throws<DataCorruptedException>(() => serializer.Deserialize<DateTime>(data));
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
        public void TestLocalOldFormat()
        {
            var data = new byte[10];
            data[0] = (byte)GroBufTypeCode.DateTimeOld;
            Array.Copy(BitConverter.GetBytes(1234567891234 | long.MinValue), 0, data, 1, 8);
            data[9] = (byte)DateTimeKind.Local;
            var o = serializer.Deserialize<DateTime>(data);
            Assert.AreEqual(DateTimeKind.Local, o.Kind);
            Assert.AreEqual(1234567891234, o.Ticks);
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

        [Test]
        public void TestSkipDateTimeLocal()
        {
            var o = new TestClassA {S = "zzz", DateTime = new DateTime(12387401892734, DateTimeKind.Local), Z = "qxx"};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<TestClassB>(data);
            Assert.AreEqual("zzz", oo.S);
            Assert.AreEqual("qxx", oo.Z);
        }

        [Test]
        public void TestSkipDateTimeUtc()
        {
            var o = new TestClassA {S = "zzz", DateTime = new DateTime(12387401892734, DateTimeKind.Utc), Z = "qxx"};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<TestClassB>(data);
            Assert.AreEqual("zzz", oo.S);
            Assert.AreEqual("qxx", oo.Z);
        }

        [Test]
        public void TestSkipDateTimeUnspecified()
        {
            var o = new TestClassA {S = "zzz", DateTime = new DateTime(12387401892734, DateTimeKind.Unspecified), Z = "qxx"};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<TestClassB>(data);
            Assert.AreEqual("zzz", oo.S);
            Assert.AreEqual("qxx", oo.Z);
        }

        private Serializer serializer;

        public class TestClassA
        {
            public string S { get; set; }
            public DateTime DateTime { get; set; }
            public string Z { get; set; }
        }

        public class TestClassB
        {
            public string S { get; set; }
            public string Z { get; set; }
        }
    }
}