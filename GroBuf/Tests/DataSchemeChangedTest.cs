using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class DataSchemeChangedTest
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor());
        }

        [Test]
        public void PropertyHasBeenAdded()
        {
            var o = new C1_Old {S = "zzz", X = 123};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<C1_New>(data);
            Assert.AreEqual("zzz", oo.S);
            Assert.AreEqual(123, oo.X);
            Assert.IsNull(oo.D);
        }

        [Test]
        public void PropertyHasBeenRemoved()
        {
            var o = new C2_Old {S = "zzz", X = 123, D = 3.14};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<C2_New>(data);
            Assert.AreEqual("zzz", oo.S);
            Assert.AreEqual(3.14, oo.D);
        }

        public class C1_Old
        {
            public string S { get; set; }
            public int X { get; set; }
        }

        public class C1_New
        {
            public int X { get; set; }
            public double? D { get; set; }
            public string S { get; set; }
        }

        public class C2_Old
        {
            public int X { get; set; }
            public double? D { get; set; }
            public string S { get; set; }
        }

        public class C2_New
        {
            public string S { get; set; }
            public double? D { get; set; }
        }

        private Serializer serializer;
    }
}