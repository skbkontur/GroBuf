using System;
using System.Net;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestIPAddress
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new AllPropertiesExtractor());
        }

        [Test]
        public void TestSize()
        {
            var address = new IPAddress(new byte[] {123, 1, 2, 3});
            var size = serializer.GetSize(address);
            Assert.AreEqual(9, size);
            address = new IPAddress(Guid.NewGuid().ToByteArray());
            size = serializer.GetSize(address);
            Assert.AreEqual(21, size);
        }

        [Test]
        public void TestReadWrite()
        {
            var address = new IPAddress(new byte[] {123, 1, 2, 3});
            var data = serializer.Serialize(address);
            var address2 = serializer.Deserialize<IPAddress>(data);
            Assert.AreEqual(address, address2);
            address = new IPAddress(Guid.NewGuid().ToByteArray());
            data = serializer.Serialize(address);
            address2 = serializer.Deserialize<IPAddress>(data);
            Assert.AreEqual(address, address2);
        }

        [Test]
        public void TestReadWrite2()
        {
            var zzz = new Zzz {Address = new IPAddress(new byte[] {123, 1, 2, 3})};
            var data = serializer.Serialize(zzz);
            var zzz2 = serializer.Deserialize<Zzz>(data);
            Assert.AreEqual(zzz.Address, zzz2.Address);
            zzz = new Zzz {Address = new IPAddress(Guid.NewGuid().ToByteArray())};
            data = serializer.Serialize(zzz);
            zzz2 = serializer.Deserialize<Zzz>(data);
            Assert.AreEqual(zzz.Address, zzz2.Address);
        }

        public class Zzz
        {
            public IPAddress Address { get; set; }
        }

        private Serializer serializer;
    }
}