using System;

using GroBuf.DataMembersExtractors;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestWithDerivedTypes
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor());
        }

        [Test]
        public void Test1()
        {
            var o = new Derived {Z = "zzz", S = "qxx"};
            var e = Assert.Throws<InvalidOperationException>(() => serializer.Serialize(o));
            Assert.AreEqual("Hash code collision: members 'Base.V' and 'Derived.V' have the same hash code = 1558541029501997959", e.Message);
        }

        [Test]
        public void Test2()
        {
            var o = new A {B = new Derived {Z = "zzz", S = "qxx"}};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            Assert.AreEqual("qxx", oo.B.S);
            Assert.AreEqual("DerivedV", oo.B.VF);
        }

        public class A
        {
            public Base B { get; set; }
        }

        public class Base
        {
            public string S { get; set; }
            public virtual string V { get { return "BaseV"; } set { VF = value; } }
            public string VF;
        }

        public class Derived : Base
        {
            public string Z { get; set; }
            public override string V { get { return "DerivedV"; } set { VFF = value; } }
            public string VFF;
        }

        private Serializer serializer;
    }
}