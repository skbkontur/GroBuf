using System;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestTuple
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new AllPropertiesExtractor());
        }

        [Test]
        public void TestGetSize1()
        {
            Console.WriteLine(serializer.GetSize(new Tuple<int>(10)));
        }

        [Test]
        public void TestGetSize2()
        {
            Console.WriteLine(serializer.GetSize(new Tuple<int, string>(10, "zzz")));
        }

        [Test]
        public void TestWrite1()
        {
            var data = serializer.Serialize(new Tuple<int>(10));
            Console.WriteLine(data.Length);
        }

        [Test]
        public void TestWrite2()
        {
            var data = serializer.Serialize(new Tuple<int, string>(10, "zzz"));
            Console.WriteLine(data.Length);
        }

        [Test]
        public void TestRead1()
        {
            var data = serializer.Serialize(new Tuple<int>(10));
            var tuple = serializer.Deserialize<Tuple<int>>(data);
            Assert.AreEqual(10, tuple.Item1);
        }

        [Test]
        public void TestRead2()
        {
            var data = serializer.Serialize(new Tuple<int, string>(10, "zzz"));
            var tuple = serializer.Deserialize<Tuple<int, string>>(data);
            Assert.AreEqual(10, tuple.Item1);
            Assert.AreEqual("zzz", tuple.Item2);
        }

        [Test]
        public void TestRead_8Args()
        {
            var data = serializer.Serialize(new Tuple<int, int, int, int, int, int, int, Tuple<int>>(1, 2, 3, 4, 5, 6, 7, new Tuple<int>(8)));
            var tuple = serializer.Deserialize<Tuple<int, int, int, int, int, int, int, Tuple<int>>>(data);
            Assert.AreEqual(1, tuple.Item1);
            Assert.AreEqual(2, tuple.Item2);
            Assert.AreEqual(3, tuple.Item3);
            Assert.AreEqual(4, tuple.Item4);
            Assert.AreEqual(5, tuple.Item5);
            Assert.AreEqual(6, tuple.Item6);
            Assert.AreEqual(7, tuple.Item7);
            Assert.AreEqual(8, tuple.Rest.Item1);
        }

        [Test]
        public void TestRead_Error()
        {
            var data = serializer.Serialize(new Tuple<int>(10));
            Assert.Throws<DataCorruptedException>(() => serializer.Deserialize<Tuple<int, string>>(data));
        }

        [Test]
        public void TestRead_Error2()
        {
            var data = serializer.Serialize(new Tuple<int, string>(10, "zzz"));
            Assert.Throws<DataCorruptedException>(() => serializer.Deserialize<Tuple<int>>(data));
        }

        [Test]
        public void Test_Property()
        {
            var o = new TestClass {Tuple = new Tuple<int, string>(10, "zzz")};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<TestClass>(data);
            Assert.IsNotNull(oo.Tuple);
            Assert.AreEqual(10, oo.Tuple.Item1);
            Assert.AreEqual("zzz", oo.Tuple.Item2);
        }

        private Serializer serializer;

        private class TestClass
        {
            public Tuple<int, string> Tuple { get; set; }
        }
    }
}