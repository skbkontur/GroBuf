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
            serializer = new Serializer(new PropertiesExtractor(), null, GroBufOptions.MergeOnRead);
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

        [Test]
        public void PropertyHasBecomeArray()
        {
            var o = new C3_Old {S = "zzz", X = 123, C3 = new C3_Subclass {S = "qxx"}};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<C3_New>(data);

            Assert.IsNotNull(oo.S);
            Assert.AreEqual(1, oo.S.Length);
            Assert.AreEqual("zzz", oo.S[0]);
            Assert.IsNotNull(oo.X);
            Assert.AreEqual(1, oo.X.Length);
            Assert.AreEqual(123, oo.X[0]);
            Assert.IsNotNull(oo.C3);
            Assert.AreEqual(1, oo.C3.Length);
            Assert.IsNotNull(oo.C3[0]);
            Assert.AreEqual("qxx", oo.C3[0].S);

            oo = new C3_New{S = new string[0], X = new int[0], C3 = new C3_Subclass[0]};
            serializer.Merge(o, ref oo);
            Assert.IsNotNull(oo.S);
            Assert.AreEqual(1, oo.S.Length);
            Assert.AreEqual("zzz", oo.S[0]);
            Assert.IsNotNull(oo.X);
            Assert.AreEqual(1, oo.X.Length);
            Assert.AreEqual(123, oo.X[0]);
            Assert.IsNotNull(oo.C3);
            Assert.AreEqual(1, oo.C3.Length);
            Assert.IsNotNull(oo.C3[0]);
            Assert.AreEqual("qxx", oo.C3[0].S);

            oo = new C3_New{S = new string[1], X = new int[1], C3 = new C3_Subclass[1]};
            serializer.Merge(o, ref oo);
            Assert.IsNotNull(oo.S);
            Assert.AreEqual(1, oo.S.Length);
            Assert.AreEqual("zzz", oo.S[0]);
            Assert.IsNotNull(oo.X);
            Assert.AreEqual(1, oo.X.Length);
            Assert.AreEqual(123, oo.X[0]);
            Assert.IsNotNull(oo.C3);
            Assert.AreEqual(1, oo.C3.Length);
            Assert.IsNotNull(oo.C3[0]);
            Assert.AreEqual("qxx", oo.C3[0].S);

            oo = new C3_New{S = new string[2], X = new int[2], C3 = new C3_Subclass[2]};
            serializer.Merge(o, ref oo);
            Assert.IsNotNull(oo.S);
            Assert.AreEqual(2, oo.S.Length);
            Assert.AreEqual("zzz", oo.S[0]);
            Assert.IsNotNull(oo.X);
            Assert.AreEqual(2, oo.X.Length);
            Assert.AreEqual(123, oo.X[0]);
            Assert.IsNotNull(oo.C3);
            Assert.AreEqual(2, oo.C3.Length);
            Assert.IsNotNull(oo.C3[0]);
            Assert.AreEqual("qxx", oo.C3[0].S);

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

        public class C3_Subclass
        {
            public string S { get; set; }
        }

        public class C3_Old
        {
            public string S { get; set; }
            public int X { get; set; }
            public C3_Subclass C3 { get; set; }
        }

        public class C3_New
        {
            public string[] S { get; set; }
            public int[] X { get; set; }
            public C3_Subclass[] C3 { get; set; }
        }

        private Serializer serializer;
    }
}