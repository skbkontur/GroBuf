using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestUnknownTypeCode
    {
        private SerializerImpl serializer;

        [SetUp]
        public void SetUp()
        {
            serializer = new SerializerImpl(new SerializeInterfaceTest.AllPropertiesExtractor());
        }

        [Test]
        public void Test()
        {
            var data = serializer.Serialize(0);
            data[0] = 127;
            Assert.Throws<DataCorruptedException>(() => serializer.Deserialize<int>(data));
        }
    }
}