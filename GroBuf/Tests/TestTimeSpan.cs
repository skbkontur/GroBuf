using System;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestTimeSpan
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new AllPropertiesExtractor());
        }

        [Test]
        public void TestSize()
        {
            var timeSpan = new TimeSpan(12938746);
            var size = serializer.GetSize(timeSpan);
            Assert.AreEqual(9, size);
        }

        [Test]
        public void TestReadWrite()
        {
            var timeSpan = new TimeSpan(239856851);
            var data = serializer.Serialize(timeSpan);
            var timeSpan2 = serializer.Deserialize<TimeSpan>(data);
            Assert.AreEqual(timeSpan, timeSpan2);
        }

        [Test]
        public void TestReadWrite2()
        {
            var zzz = new Zzz {TimeSpan = new TimeSpan(82354222765175)};
            var data = serializer.Serialize(zzz);
            var zzz2 = serializer.Deserialize<Zzz>(data);
            Assert.AreEqual(zzz.TimeSpan, zzz2.TimeSpan);
        }

        public class Zzz
        {
            public TimeSpan TimeSpan { get; set; }
        }

        private Serializer serializer;
    }
}