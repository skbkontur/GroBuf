using System;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestArraySegment
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor());
        }

        [Test]
        public void TestGetSize()
        {
            var list = new ArraySegment<string>(new[] {"1", "2", "3"});
            var size = serializer.GetSize(list);
            Console.WriteLine(size);
            list = new ArraySegment<string>(new[] {"1", "2", "3"}, 1, 2);
            size = serializer.GetSize(list);
            Console.WriteLine(size);
        }

        [Test]
        public void TestGetSizePrimitives()
        {
            var list = new ArraySegment<int>(new[] {1, 2, 3});
            var size = serializer.GetSize(list);
            Console.WriteLine(size);
            list = new ArraySegment<int>(new[] {1, 2, 3}, 1, 2);
            size = serializer.GetSize(list);
            Console.WriteLine(size);
        }

        [Test]
        public void TestWrite()
        {
            var list = new ArraySegment<string>(new[] {"1", "2", "3"});
            var buf = serializer.Serialize(list);
            Console.WriteLine(buf.Length);
            list = new ArraySegment<string>(new[] {"1", "2", "3"}, 1, 2);
            buf = serializer.Serialize(list);
            Console.WriteLine(buf.Length);
        }

        [Test]
        public void TestWritePrimitives()
        {
            var list = new ArraySegment<int>(new[] {1, 2, 3});
            var buf = serializer.Serialize(list);
            Console.WriteLine(buf.Length);
            list = new ArraySegment<int>(new[] {1, 2, 3}, 1, 2);
            buf = serializer.Serialize(list);
            Console.WriteLine(buf.Length);
        }

        [Test]
        public void TestRead()
        {
            var list = new ArraySegment<string>(new[] {"1", "2", "3"});
            var buf = serializer.Serialize(list);
            var list2 = serializer.Deserialize<ArraySegment<string>>(buf);
            Assert.AreEqual(3, list2.Count);
            Assert.AreEqual("1", list2.Array[list2.Offset + 0]);
            Assert.AreEqual("2", list2.Array[list2.Offset + 1]);
            Assert.AreEqual("3", list2.Array[list2.Offset + 2]);
            list = new ArraySegment<string>(new[] {"1", "2", "3"}, 1, 2);
            buf = serializer.Serialize(list);
            list2 = serializer.Deserialize<ArraySegment<string>>(buf);
            Assert.AreEqual(2, list2.Count);
            Assert.AreEqual("2", list2.Array[list2.Offset + 0]);
            Assert.AreEqual("3", list2.Array[list2.Offset + 1]);
        }

        [Test]
        public void TestReadPrimitives()
        {
            var list = new ArraySegment<int>(new[] {1, 2, 3});
            var buf = serializer.Serialize(list);
            var list2 = serializer.Deserialize<ArraySegment<int>>(buf);
            Assert.AreEqual(3, list2.Count);
            Assert.AreEqual(1, list2.Array[list2.Offset + 0]);
            Assert.AreEqual(2, list2.Array[list2.Offset + 1]);
            Assert.AreEqual(3, list2.Array[list2.Offset + 2]);
            list = new ArraySegment<int>(new[] {1, 2, 3}, 1, 2);
            buf = serializer.Serialize(list);
            list2 = serializer.Deserialize<ArraySegment<int>>(buf);
            Assert.AreEqual(2, list2.Count);
            Assert.AreEqual(2, list2.Array[list2.Offset + 0]);
            Assert.AreEqual(3, list2.Array[list2.Offset + 1]);
        }

        [Test]
        public void Test_WriteSegment_ReadArray()
        {
            var list = new ArraySegment<string>(new[] {"1", "2", "3"});
            var data = serializer.Serialize(list);
            var array = serializer.Deserialize<string[]>(data);
            Assert.AreEqual(3, array.Length);
            Assert.AreEqual("1", array[0]);
            Assert.AreEqual("2", array[1]);
            Assert.AreEqual("3", array[2]);
            list = new ArraySegment<string>(new[] {"1", "2", "3"}, 1, 2);
            data = serializer.Serialize(list);
            array = serializer.Deserialize<string[]>(data);
            Assert.AreEqual(2, array.Length);
            Assert.AreEqual("2", array[0]);
            Assert.AreEqual("3", array[1]);
        }

        [Test]
        public void Test_WriteArray_ReadSegment()
        {
            var array = new[] {"3", "2", "1"};
            var data = serializer.Serialize(array);
            var list = serializer.Deserialize<ArraySegment<string>>(data);
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("3", list.Array[list.Offset]);
            Assert.AreEqual("2", list.Array[list.Offset + 1]);
            Assert.AreEqual("1", list.Array[list.Offset + 2]);
        }

        [Test]
        public void Test_WriteElement_ReadSegment()
        {
            var data = serializer.Serialize("zzz");
            var list = serializer.Deserialize<ArraySegment<string>>(data);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("zzz", list.Array[list.Offset]);
        }

        [Test]
        public void Test_WriteSegment_ReadArray_Primitives()
        {
            var list = new ArraySegment<int>(new[] {1, 2, 3});
            var data = serializer.Serialize(list);
            var array = serializer.Deserialize<int[]>(data);
            Assert.AreEqual(3, array.Length);
            Assert.AreEqual(1, array[0]);
            Assert.AreEqual(2, array[1]);
            Assert.AreEqual(3, array[2]);
            list = new ArraySegment<int>(new[] {1, 2, 3}, 1, 2);
            data = serializer.Serialize(list);
            array = serializer.Deserialize<int[]>(data);
            Assert.AreEqual(2, array.Length);
            Assert.AreEqual(2, array[0]);
            Assert.AreEqual(3, array[1]);
        }

        [Test]
        public void Test_WriteArray_ReadSegment_Primitives()
        {
            var array = new[] {3, 2, 1};
            var data = serializer.Serialize(array);
            var list = serializer.Deserialize<ArraySegment<int>>(data);
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(3, list.Array[list.Offset]);
            Assert.AreEqual(2, list.Array[list.Offset + 1]);
            Assert.AreEqual(1, list.Array[list.Offset + 2]);
        }

        [Test]
        public void Test_WriteElement_ReadSegment_Primitives()
        {
            var data = serializer.Serialize(123);
            var list = serializer.Deserialize<ArraySegment<int>>(data);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(123, list.Array[list.Offset]);
        }

        private Serializer serializer;
    }
}