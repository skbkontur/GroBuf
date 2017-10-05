using System;
using System.Runtime.Serialization;

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

            oo = new C3_New {S = new string[0], X = new int[0], C3 = new C3_Subclass[0]};
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

            oo = new C3_New {S = new string[1], X = new int[1], C3 = new C3_Subclass[1]};
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

            oo = new C3_New {S = new string[2], X = new int[2], C3 = new C3_Subclass[2]};
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

        [Test]
        public void TestReadWriteToId()
        {
            var o = new C4_Old {S = "zzz", X = 123};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<C4_New>(data);
            Assert.AreEqual("zzz", oo.Zzz);
            Assert.AreEqual(123, oo.Qxx);
        }

        [Test]
        public void TestReadWriteFromId()
        {
            var o = new C4_New {Zzz = "zzz", Qxx = 123};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<C4_Old>(data);
            Assert.AreEqual("zzz", oo.S);
            Assert.AreEqual(123, oo.X);
        }

        [Test]
        public void Test_GroboMember_IdCollision()
        {
            var o = new C5_GroboMember_IdCollision();
            var e = Assert.Throws<InvalidOperationException>(() => serializer.Serialize(o));
            Assert.That(e.Message, Is.EqualTo("Hash code collision: members 'C5_GroboMember_IdCollision.X' and 'C5_GroboMember_IdCollision.S' have the same hash code = 1"));
        }

        [Test]
        public void Test_GroboMember_EmptyName()
        {
            var o = new C6_GroboMember_EmptyName();
            var e = Assert.Throws<InvalidOperationException>(() => serializer.Serialize(o));
            Assert.That(e.Message, Is.EqualTo("Empty grobo name of member 'C6_GroboMember_EmptyName.S'"));
        }

        [Test]
        public void Test_GroboMember_EmptyId()
        {
            var o = new C7_GroboMember_EmptyId();
            var e = Assert.Throws<InvalidOperationException>(() => serializer.Serialize(o));
            Assert.That(e.Message, Is.EqualTo("Hash code of 'C7_GroboMember_EmptyId.S' equals to zero"));
        }

        [Test]
        public void Test_MixedAnnotationAttributes()
        {
            var o = new C8_MixedAnnotationAttributes {S = "zzz", X = 123, Q = -1};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<C8_MixedAnnotationAttributes>(data);
            Assert.AreEqual("zzz", oo.S);
            Assert.AreEqual(123, oo.X);
            Assert.AreEqual(-1, oo.Q);
        }

        [Test]
        public void Test_GroboMember_ReadonlyProperty()
        {
            var o = new C9_GroboMember_ReadonlyProperty_Old("GRobas");
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<C9_GroboMember_ReadonlyProperty_New>(data);
            Assert.AreEqual("GRobas", oo.Zzz);
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

        public class C4_Old
        {
            public string S { get; set; }
            public int X { get; set; }
        }

        public class C4_New
        {
            [GroboMember(7770670552212394539)]
            public string Zzz { get; set; }

            [GroboMember("X")]
            public int Qxx { get; set; }
        }

        public class C5_GroboMember_IdCollision
        {
            [GroboMember(1)]
            public string S { get; set; }

            [GroboMember(1)]
            public int X { get; set; }
        }

        public class C6_GroboMember_EmptyName
        {
            [GroboMember("")]
            public string S { get; set; }
        }

        public class C7_GroboMember_EmptyId
        {
            [GroboMember(0)]
            public string S { get; set; }
        }

        public class C8_MixedAnnotationAttributes
        {
            public string S { get; set; }

            [DataMember(Name = "Z")]
            public int X { get; set; }

            [GroboMember(1)]
            public int? Q { get; set; }
        }

        public class C9_GroboMember_ReadonlyProperty_Old
        {
            public C9_GroboMember_ReadonlyProperty_Old(string qxx)
            {
                Qxx = qxx;
            }

            public string Qxx { get; }
        }

        public class C9_GroboMember_ReadonlyProperty_New
        {
            public C9_GroboMember_ReadonlyProperty_New(string gRobas)
            {
                Zzz = gRobas;
            }

            [GroboMember("Qxx")]
            public string Zzz { get; }
        }

        private Serializer serializer;
    }
}