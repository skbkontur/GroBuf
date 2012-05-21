using System;
using System.Linq;
using System.Reflection;

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
            serializer = new SerializerImpl(new AllPropertiesExtracter());
        }

        [Test]
        public void TestStupid()
        {
            byte[] serialize = serializer.Serialize(new CWithnonPublics(2378, 3434));
            var result = serializer.Deserialize<CWithnonPublics>(serialize);
            Assert.AreEqual(2378, result.A);
            Assert.AreEqual(3434, result.GetB());
        }

        public class AllPropertiesExtracter : IDataMembersExtracter
        {
            public MemberInfo[] GetMembers(Type type)
            {
                return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.CanRead && property.CanWrite).ToArray();
            }
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

        private SerializerImpl serializer;
    }
}