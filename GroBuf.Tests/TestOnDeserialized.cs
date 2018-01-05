using System;
using System.Runtime.Serialization;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestOnDeserialized
    {
        private Serializer serializer;

        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor());
        }

        [Test]
        public void TestOk()
        {
            var o = new TestData {S = "zzz", DeserializedCalled = false};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<TestData>(data);
            Assert.IsTrue(oo.DeserializedCalled);
            Assert.That(oo.s, Is.EqualTo("zzz"));
        }

        [Test]
        public void TestBadMethod()
        {
            var o = new BadData {S = "zzz"};
            var data = serializer.Serialize(o);
            Assert.Throws<InvalidOperationException>(() => serializer.Deserialize<BadData>(data));
        }

        private class TestData
        {
            public string S { get; set; }
            public bool DeserializedCalled { get; set; }
            public string s;

            [OnDeserialized]
            private void OnDeserialized(StreamingContext context)
            {
                DeserializedCalled = true;
                s = S;
            }
        }

        private class BadData
        {
            public string S { get; set; }

            [OnDeserialized]
            private void OnDeserialized()
            {
            }
        }
    }
}