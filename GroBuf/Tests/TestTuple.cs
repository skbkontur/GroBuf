using System;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestTuple
    {
        private Serializer serializer;

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
            var o = new TestClass{Tuple = new Tuple<int, string>(10, "zzz")};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<TestClass>(data);
            Assert.IsNotNull(oo.Tuple);
            Assert.AreEqual(10, oo.Tuple.Item1);
            Assert.AreEqual("zzz", oo.Tuple.Item2);
        }

        private class TestClass
        {
            public Tuple<int, string> Tuple { get; set; }
        }
    }
}