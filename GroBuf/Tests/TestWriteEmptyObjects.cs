using System.Collections.Generic;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestWriteEmptyObjects
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor(), null, GroBufOptions.WriteEmptyObjects);
        }

        [Test]
        public void TestEmptyArray()
        {
            var a = new A {Strings = new string[0]};
            var data = serializer.Serialize(a);
            var aa = serializer.Deserialize<A>(data);
            Assert.IsNotNull(aa);
            Assert.IsNotNull(aa.Strings);
            Assert.AreEqual(0, aa.Strings.Length);
        }

        [Test]
        public void TestEmptyPrimitivesArray()
        {
            var a = new A {Ints = new int[0]};
            var data = serializer.Serialize(a);
            var aa = serializer.Deserialize<A>(data);
            Assert.IsNotNull(aa);
            Assert.IsNotNull(aa.Ints);
            Assert.AreEqual(0, aa.Ints.Length);
        }

        [Test]
        public void TestEmptyList()
        {
            var a = new A {StringList = new List<string>()};
            var data = serializer.Serialize(a);
            var aa = serializer.Deserialize<A>(data);
            Assert.IsNotNull(aa);
            Assert.IsNotNull(aa.StringList);
            Assert.AreEqual(0, aa.StringList.Count);
        }

        [Test]
        public void TestEmptyPrimitivesList()
        {
            var a = new A {IntList = new List<int>()};
            var data = serializer.Serialize(a);
            var aa = serializer.Deserialize<A>(data);
            Assert.IsNotNull(aa);
            Assert.IsNotNull(aa.IntList);
            Assert.AreEqual(0, aa.IntList.Count);
        }

        [Test]
        public void TestEmptyHashSet()
        {
            var a = new A {StringHashSet = new HashSet<string>()};
            var data = serializer.Serialize(a);
            var aa = serializer.Deserialize<A>(data);
            Assert.IsNotNull(aa);
            Assert.IsNotNull(aa.StringHashSet);
            Assert.AreEqual(0, aa.StringHashSet.Count);
        }

        [Test]
        public void TestEmptyPrimitivesHashSet()
        {
            var a = new A { IntHashSet = new HashSet<int>() };
            var data = serializer.Serialize(a);
            var aa = serializer.Deserialize<A>(data);
            Assert.IsNotNull(aa);
            Assert.IsNotNull(aa.IntHashSet);
            Assert.AreEqual(0, aa.IntHashSet.Count);
        }

        [Test]
        public void TestEmptyDictionary()
        {
            var a = new A {Dict = new Dictionary<int, int>()};
            var data = serializer.Serialize(a);
            var aa = serializer.Deserialize<A>(data);
            Assert.IsNotNull(aa);
            Assert.IsNotNull(aa.Dict);
            Assert.AreEqual(0, aa.Dict.Count);
        }

        [Test]
        public void TestEmptyClass()
        {
            var b = new B {A = new A()};
            var data = serializer.Serialize(b);
            var bb = serializer.Deserialize<B>(data);
            Assert.IsNotNull(bb);
            Assert.IsNotNull(bb.A);
        }

        [Test]
        public void TestComplex()
        {
            var b = new B {A = new A {Strings = new string[0], Ints = new int[0]}, ArrayA = new[] {null, new A()}};
            var data = serializer.Serialize(b);
            var bb = serializer.Deserialize<B>(data);
            Assert.IsNotNull(bb);
            Assert.IsNotNull(bb.A);
            Assert.IsNotNull(bb.A.Strings);
            Assert.AreEqual(0, bb.A.Strings.Length);
            Assert.IsNotNull(bb.A.Ints);
            Assert.AreEqual(0, bb.A.Ints.Length);
            Assert.IsNotNull(bb.ArrayA);
            Assert.AreEqual(2, bb.ArrayA.Length);
            Assert.IsNull(bb.ArrayA[0]);
            Assert.IsNotNull(bb.ArrayA[1]);
        }

        private Serializer serializer;

        private class A
        {
            public string[] Strings { get; set; }
            public int[] Ints { get; set; }
            public List<string> StringList { get; set; }
            public List<int> IntList { get; set; }
            public HashSet<string> StringHashSet { get; set; }
            public HashSet<int> IntHashSet { get; set; }
            public Dictionary<int, int> Dict { get; set; }
        }

        private class B
        {
            public A A { get; set; }
            public A[] ArrayA { get; set; }
        }
    }
}