using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestNonPublicProperties
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor());
        }

        [Test]
        public void Test()
        {
            byte[] serialize = serializer.Serialize(new CWithnonPublics(2378, 3434, 5656));
            var result = serializer.Deserialize<CWithnonPublics>(serialize);
            Assert.AreEqual(2378, result.A);
            Assert.AreEqual(3434, result.GetB());
            Assert.AreEqual(5656, result.C);
        }

        public class CWithnonPublics
        {
            public CWithnonPublics()
            {
            }

            public CWithnonPublics(int a, int b, int c)
            {
                A = a;
                B = b;
                C = c;
            }

            public int GetB()
            {
                return B;
            }

            public int A { get; private set; }
            public int B { private get; set; }
            public int C { get; }
        }

        private Serializer serializer;
    }
}