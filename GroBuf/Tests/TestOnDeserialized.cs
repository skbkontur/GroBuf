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
        public void Test()
        {
            var o = new TestData {S = "zzz", DeserializedCalled = false};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<TestData>(data);
            Assert.IsTrue(oo.DeserializedCalled);
        }

        private class TestData
        {
            public string S { get; set; }
            public bool DeserializedCalled { get; set; }

            [OnDeserialized]
            private void OnDeserialized()
            {
                DeserializedCalled = true;
            }
        }
    }
}