using System;
using System.Linq;
using System.Reflection;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class SerializeInterfaceTest
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new AllPropertiesExtractor());
        }

        [Test]
        public void TestInterfaceArray()
        {
            byte[] serialize = serializer.Serialize(new I1[] {new C1 {Data = 2}});
            var deserialize = serializer.Deserialize<C1[]>(serialize);
            Assert.AreEqual(1, deserialize.Length);
            Assert.AreEqual(2, deserialize[0].Data);
        }

        private interface I1
        {
            int Data { get; }
        }

        private Serializer serializer;

        public class AllPropertiesExtractor : IDataMembersExtractor
        {
            public IDataMember[] GetMembers(Type type)
            {
                return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(DataMember.Create).ToArray();
            }
        }

        private class C1 : I1
        {
            public int Data { get; set; }
        }
    }
}