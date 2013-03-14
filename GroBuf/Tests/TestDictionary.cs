using System;
using System.Collections.Generic;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestDictionary
    {
        private SerializerImpl serializer;

        [SetUp]
        public void SetUp()
        {
            serializer = new SerializerImpl(new PropertiesExtractor());
        }

        [Test]
        public void TestGetSize()
        {
            var dict = new Dictionary<string, int> {{"1", 1}, {"2", 2}};
            var size = serializer.GetSize(dict);
            Console.WriteLine(size);
        }

        [Test]
        public void TestWrite()
        {
            var dict = new Dictionary<string, int> { { "1", 1 }, { "2", 2 } };
            var buf = serializer.Serialize(dict);
            Console.WriteLine(buf.Length);
        }

        [Test]
        public void TestRead()
        {
            var dict = new Dictionary<string, int> { { "1", 1 }, { "2", 2 } };
            var buf = serializer.Serialize(dict);
            var dict2 = serializer.Deserialize<Dictionary<string, int>>(buf);
            Assert.AreEqual(2, dict2.Count);
            Assert.AreEqual(1, dict2["1"]);
            Assert.AreEqual(2, dict2["2"]);
        }
    }
}