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
            serializer = new SerializerImpl(new PropertiesExtractor(), GroBufOptions.WriteEmptyObjects);
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

        private SerializerImpl serializer;

        private class A
        {
            public string[] Strings { get; set; }
            public int[] Ints { get; set; }
            public List<string> StringList { get; set; }
            public List<int> IntList { get; set; }
        }

        private class B
        {
            public A A { get; set; }
            public A[] ArrayA { get; set; }
        }
    }
}