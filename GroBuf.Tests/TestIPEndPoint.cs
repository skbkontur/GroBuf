using System;
using System.Net;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestIPEndPoint
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new AllPropertiesExtractor());
        }

        [Test]
        public void TestReadWrite()
        {
            var endPoint = new IPEndPoint(new IPAddress(new byte[] {123, 1, 2, 3}), 146);
            var data = serializer.Serialize(endPoint);
            var endPoint2 = serializer.Deserialize<IPEndPoint>(data);
            Assert.AreEqual(endPoint, endPoint2);
            endPoint = new IPEndPoint(new IPAddress(Guid.NewGuid().ToByteArray()), 146);
            data = serializer.Serialize(endPoint);
            endPoint2 = serializer.Deserialize<IPEndPoint>(data);
            Assert.AreEqual(endPoint, endPoint2);
        }

        [Test]
        public void TestReadWrite2()
        {
            var zzz = new Zzz {EndPoint = new IPEndPoint(new IPAddress(new byte[] {123, 1, 2, 3}), 146)};
            var data = serializer.Serialize(zzz);
            var zzz2 = serializer.Deserialize<Zzz>(data);
            Assert.AreEqual(zzz.EndPoint, zzz2.EndPoint);
            zzz = new Zzz {EndPoint = new IPEndPoint(new IPAddress(Guid.NewGuid().ToByteArray()), 146)};
            data = serializer.Serialize(zzz);
            zzz2 = serializer.Deserialize<Zzz>(data);
            Assert.AreEqual(zzz.EndPoint, zzz2.EndPoint);
        }

        private Serializer serializer;

        public class Zzz
        {
            public IPEndPoint EndPoint { get; set; }
        }
    }
}