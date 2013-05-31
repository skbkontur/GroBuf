using System;
using System.Linq;
using System.Reflection;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class QxxTest
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new SerializerImpl(new AllPropertiesExtractor());
        }

        [Test]
        public void TestInterfaceFail()
        {
            serializer.Serialize(new I1[] {new C1 {Data = 1}});
        }

        public class AllPropertiesExtractor : IDataMembersExtractor
        {
            public MemberInfo[] GetMembers(Type type)
            {
                return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToArray();
            }
        }

        private SerializerImpl serializer;

        private class C1 : I1
        {
            public int Data { get; set; }
        }

        private interface I1
        {
            int Data { get; }
        }
    }
}