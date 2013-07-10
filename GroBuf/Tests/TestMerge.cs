using System.Collections.Generic;

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
            serializer = new SerializerImpl(new PropertiesExtractor(), null, GroBufOptions.MergeOnRead);
        }

        [Test]
        public void TestArray1()
        {
            var first = new int?[] {1, null};
            var second = new int?[] {2, 3, 5};
            serializer.Merge(first, ref second);
            second.AssertEqualsTo(new int?[] {1, 3, 5});

            first = new int?[] {1, null, 3, 4};
            second = new int?[] {5, 6, 7};
            serializer.Merge(first, ref second);
            second.AssertEqualsTo(new int?[] {1, 6, 3, 4});
        }

        [Test]
        public void TestArray2()
        {
            var first = new[] {1, 2};
            var second = new[] {2, 3, 5};
            serializer.Merge(first, ref second);
            second.AssertEqualsTo(new[] {1, 2, 5});

            first = new[] {1, 2, 3, 4};
            second = new[] {5, 6, 7};
            serializer.Merge(first, ref second);
            second.AssertEqualsTo(new[] {1, 2, 3, 4});
        }

        [Test]
        public void TestList1()
        {
            var first = new List<int?> {1, null};
            var second = new List<int?> {2, 3, 5};
            serializer.Merge(first, ref second);
            Assert.AreEqual(3, second.Count);
            Assert.AreEqual(1, second[0]);
            Assert.AreEqual(3, second[1]);
            Assert.AreEqual(5, second[2]);

            first = new List<int?> {1, null, 3, 4};
            second = new List<int?> {5, 6, 7};
            serializer.Merge(first, ref second);
            Assert.AreEqual(4, second.Count);
            Assert.AreEqual(1, second[0]);
            Assert.AreEqual(6, second[1]);
            Assert.AreEqual(3, second[2]);
            Assert.AreEqual(4, second[3]);
        }

        [Test]
        public void TestList2()
        {
            var first = new List<int> {1, 2};
            var second = new List<int> {2, 3, 5};
            serializer.Merge(first, ref second);
            Assert.AreEqual(3, second.Count);
            Assert.AreEqual(1, second[0]);
            Assert.AreEqual(2, second[1]);
            Assert.AreEqual(5, second[2]);

            first = new List<int> {1, 2, 3, 4};
            second = new List<int> {5, 6, 7};
            serializer.Merge(first, ref second);
            Assert.AreEqual(4, second.Count);
            Assert.AreEqual(1, second[0]);
            Assert.AreEqual(2, second[1]);
            Assert.AreEqual(3, second[2]);
            Assert.AreEqual(4, second[3]);
        }

        [Test]
        public void TestClass()
        {
            var first = new A {Bool = true};
            var second = new A {Bool = false, B = new B {S = "zzz"}};
            serializer.Merge(first, ref second);
            second.AssertEqualsTo(new A {Bool = true, B = new B {S = "zzz"}});

            first = new A {Bool = true, B = new B {S = "qxx"}, Bs = new[] {null, new B {S = "qzz"}}};
            second = new A {B = new B {S = "zzz", Long = 12341234}, Bs = new[] {new B {S = "xxx"}, new B {S = "qqq", Long = 1287346}}};
            serializer.Merge(first, ref second);
            second.AssertEqualsTo(new A {Bool = true, B = new B {S = "qxx", Long = 12341234}, Bs = new[] {new B {S = "xxx"}, new B {S = "qzz", Long = 1287346}}});
        }

        [Test]
        public void TestStruct()
        {
            var first = new As {Bool = true};
            var second = new As {Bool = false, B = new Bs {S = "zzz"}};
            serializer.Merge(first, ref second);
            second.AssertEqualsTo(new As {Bool = true, B = new Bs {S = "zzz"}});

            first = new As {Bool = true, B = new Bs {S = "qxx"}, Bs = new[] {new Bs(), new Bs {S = "qzz"}}};
            second = new As {B = new Bs {S = "zzz", Long = 12341234}, Bs = new[] {new Bs {S = "xxx"}, new Bs {S = "qqq", Long = 1287346}}};
            serializer.Merge(first, ref second);
            second.AssertEqualsTo(new As {Bool = true, B = new Bs {S = "qxx", Long = 12341234}, Bs = new[] {new Bs {S = "xxx"}, new Bs {S = "qzz", Long = 1287346}}});
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

        public struct As
        {
            public Bs[] Bs { get; set; }
            public bool? Bool { get; set; }
            public Bs B { get; set; }
        }

        public struct Bs
        {
            public string S { get; set; }
            public long? Long { get; set; }
        }

        private SerializerImpl serializer;
    }
}