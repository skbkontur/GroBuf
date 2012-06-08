using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestStrings
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer();
        }

        [Test]
        public void TestString()
        {
            const string s = "zzz ароваро \u2376 \uDEAD";
            byte[] bytes = serializer.Serialize(s);
            var deserialize = serializer.Deserialize<string>(bytes);
            Assert.AreEqual("zzz ароваро \u2376 \uDEAD", deserialize);
        }

        [Test]
        public void TestStringInProp()
        {
            const string s = "zzz ароваро \u2376 \uDEAD";
            byte[] bytes = serializer.Serialize(new WithS {S = s});
            var deserialize = serializer.Deserialize<WithS>(bytes);
            Assert.AreEqual("zzz ароваро \u2376 \uDEAD", deserialize.S);
        }

        [Test]
        public void TestStringNull()
        {
            byte[] bytes = serializer.Serialize<string>(null);
            var deserialize = serializer.Deserialize<string>(bytes);
            Assert.AreEqual(null, deserialize);
        }

        [Test]
        public void TestStringNullInProp()
        {
            byte[] bytes = serializer.Serialize(new WithS());
            var deserialize = serializer.Deserialize<WithS>(bytes);
            Assert.AreEqual(null, deserialize.S);
        }

        [Test]
        public void TestStringEmpty()
        {
            byte[] bytes = serializer.Serialize("");
            var deserialize = serializer.Deserialize<string>(bytes);
            Assert.AreEqual("", deserialize);
        }

        [Test]
        public void TestStringEmptyInProp()
        {
            byte[] bytes = serializer.Serialize(new WithS {S = ""});
            var deserialize = serializer.Deserialize<WithS>(bytes);
            Assert.AreEqual("", deserialize.S);
        }

        public class WithS
        {
            public string S { get; set; }
        }

        private Serializer serializer;
    }
}