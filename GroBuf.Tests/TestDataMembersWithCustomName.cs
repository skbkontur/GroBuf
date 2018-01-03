using System.Runtime.Serialization;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestDataMembersWithCustomName
    {
        [Test]
        public void Test()
        {
            var serializer = new Serializer(new DataMembersByAttributeExtractor(false));
            var a = new TestClassA {S = "zzz", X = 5};
            var data = serializer.Serialize(a);
            var b = serializer.Deserialize<TestClassB>(data);
            Assert.AreEqual("zzz", b.T);
            Assert.AreEqual(5, b.Y);
        }

        public class TestClassA
        {
            public string S { get; set; }

            [DataMember(Name = "Y")]
            public int X { get; set; }
        }

        public class TestClassB
        {
            [DataMember(Name = "S")]
            public string T { get; set; }

            public int Y { get; set; }
        }
    }
}