using GroBuf.DataMembersExtracters;
using GroBuf.Tests.TestTools;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestMerge
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new SerializerImpl(new PropertiesExtracter());
        }

        [Test]
        public void TestArray()
        {
            var first = new int?[] {1, null};
            byte[] data = serializer.Serialize(first);
            var second = new int?[] {2, 3, 5};
            serializer.Merge(data, ref second);
            second.AssertEqualsTo(new int?[] {1, 3, 5});

            first = new int?[] {1, null, 3, 4};
            data = serializer.Serialize(first);
            second = new int?[] {5, 6, 7};
            serializer.Merge(data, ref second);
            second.AssertEqualsTo(new int?[] {1, 6, 3, 4});
        }

        [Test]
        public void TestClass()
        {
            var first = new A {Bool = true};
            byte[] data = serializer.Serialize(first);
            var second = new A {Bool = false, B = new B {S = "zzz"}};
            serializer.Merge(data, ref second);
            second.AssertEqualsTo(new A {Bool = true, B = new B {S = "zzz"}});

            first = new A {Bool = true, B = new B {S = "qxx"}, Bs = new[] {null, new B {S = "qzz"}}};
            data = serializer.Serialize(first);
            second = new A {B = new B {S = "zzz", Long = 12341234}, Bs = new[] {new B {S = "xxx"}, new B {S = "qqq", Long = 1287346}}};
            serializer.Merge(data, ref second);
            second.AssertEqualsTo(new A {Bool = true, B = new B {S = "qxx", Long = 12341234}, Bs = new[] {new B {S = "xxx"}, new B {S = "qzz", Long = 1287346}}});
        }

        public class A
        {
            public B[] Bs { get; set; }
            public bool? Bool { get; set; }
            public B B { get; set; }
        }

        public class B
        {
            public string S { get; set; }
            public long? Long { get; set; }
        }

        private SerializerImpl serializer;
    }
}