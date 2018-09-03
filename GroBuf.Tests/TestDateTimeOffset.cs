using System;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestDateTimeOffset
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new AllPropertiesExtractor());
        }

        [Test]
        public void TestSize()
        {
            var dateTimeOffset = new DateTimeOffset(new DateTime(2010, 1, 1), new TimeSpan(0, 1, 1, 0));
            var size = serializer.GetSize(dateTimeOffset);
            Assert.AreEqual(13, size);
        }

        [Test]
        public void TestReadWrite()
        {
            var dateTimeOffset = new DateTimeOffset(new DateTime(2010, 1, 1), new TimeSpan(0, 1, 1, 0));
            var data = serializer.Serialize(dateTimeOffset);
            var dateTimeOffset2 = serializer.Deserialize<DateTimeOffset>(data);
            Assert.AreEqual(dateTimeOffset, dateTimeOffset2);
        }

        [Test]
        public void TestReadWrite2()
        {
            var zzz = new Zzz {DateTimeOffset = new DateTimeOffset(new DateTime(2010, 1, 1), new TimeSpan(0, 1, 1, 0))};
            var data = serializer.Serialize(zzz);
            var zzz2 = serializer.Deserialize<Zzz>(data);
            Assert.AreEqual(zzz.DateTimeOffset, zzz2.DateTimeOffset);
        }

        private Serializer serializer;

        public class Zzz
        {
            public DateTimeOffset DateTimeOffset { get; set; }
        }
    }
}