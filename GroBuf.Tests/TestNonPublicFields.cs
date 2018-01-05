using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestNonPublicFields
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new AllFieldsExtractor());
        }

        [Test]
        public void Test()
        {
            byte[] serialize = serializer.Serialize(new CWithnonPublics(2378, 3434));
            var result = serializer.Deserialize<CWithnonPublics>(serialize);
            Assert.AreEqual(2378, result.A);
            Assert.AreEqual(3434, result.GetB());
        }

        public class CWithnonPublics
        {
            public CWithnonPublics()
            {
            }

            public CWithnonPublics(int a, int b)
            {
                A = a;
                B = b;
            }

            public int GetB()
            {
                return B;
            }

            public int A { get; private set; }
            public int B { private get; set; }
        }

        private Serializer serializer;
    }
}