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
            byte[] serialize = serializer.Serialize(new CWithnonPublics("abstract", 2378, 3434, 5656, 6754, 9075, 4376));
            var result = serializer.Deserialize<CWithnonPublics>(serialize);
            Assert.AreEqual(2378, result.A);
            Assert.AreEqual(3434, result.GetB());
            Assert.AreEqual(5656, result.C);
            Assert.AreEqual(6754, result.D);
            Assert.AreEqual(9075, result.E);
            Assert.AreEqual(4376, result.F);
            Assert.AreEqual("abstract", result.AbstractProp);
        }

        public abstract class CWithnonPublicsBase
        {
            public abstract string AbstractProp { get; }
        }

        public class CWithnonPublics : CWithnonPublicsBase
        {
            private int d;
            private readonly int e;
            private readonly int f;

            public CWithnonPublics()
            {
            }

            public CWithnonPublics(string abstractProp, int a, int b, int c, int d, int e, int f)
            {
                AbstractProp = abstractProp;
                A = a;
                B = b;
                C = c;
                this.d = d;
                this.e = e;
                this.f = f;
            }

            public override string AbstractProp { get; }

            public int GetB()
            {
                return B;
            }

            public int A { get; private set; }
            public int B { private get; set; }
            public int C { get; }
            public int D { get { return d; } }
            public int E { get { return e; } }
            public int F => f;
        }

        private Serializer serializer;
    }
}