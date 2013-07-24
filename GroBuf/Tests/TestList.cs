using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestList
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new SerializerImpl(new PropertiesExtractor());
        }

        [Test]
        public void TestGetSize()
        {
            var list = new List<string> {"1", "2"};
            int size = serializer.GetSize(list);
            Console.WriteLine(size);
        }

        [Test]
        public void TestGetSizePrimitives()
        {
            var list = new List<int> {1, 2};
            int size = serializer.GetSize(list);
            Console.WriteLine(size);
        }

        [Test]
        public void TestWrite()
        {
            var list = new List<string> {"1", "2"};
            byte[] buf = serializer.Serialize(list);
            Console.WriteLine(buf.Length);
        }

        [Test]
        public void TestWritePrimitives()
        {
            var list = new List<int> {1, 2};
            byte[] buf = serializer.Serialize(list);
            Console.WriteLine(buf.Length);
        }

        [Test]
        public void TestRead()
        {
            var list = new List<string> {"1", "2"};
            byte[] buf = serializer.Serialize(list);
            var list2 = serializer.Deserialize<List<string>>(buf);
            Assert.AreEqual(2, list2.Count);
            Assert.AreEqual("1", list2[0]);
            Assert.AreEqual("2", list2[1]);
        }

        [Test]
        public void TestReadPrimitives()
        {
            var list = new List<int> {1, 2};
            byte[] buf = serializer.Serialize(list);
            var list2 = serializer.Deserialize<List<int>>(buf);
            Assert.AreEqual(2, list2.Count);
            Assert.AreEqual(1, list2[0]);
            Assert.AreEqual(2, list2[1]);
        }

        [Test]
        public void TestCanAddRemove()
        {
            var list = new List<int> {1, 2};
            byte[] buf = serializer.Serialize(list);
            var list2 = serializer.Deserialize<List<int>>(buf);
            Assert.AreEqual(2, list2.Count);
            Assert.AreEqual(1, list2[0]);
            Assert.AreEqual(2, list2[1]);
            for(int i = 3; i <= 100; ++i)
                list2.Add(i);
            Assert.AreEqual(100, list2.Count);
            for(int i = 1; i <= 100; ++i)
                Assert.AreEqual(i, list2[i - 1]);
            buf = serializer.Serialize(list2);
            list = serializer.Deserialize<List<int>>(buf);
            Assert.AreEqual(100, list.Count);
            for(int i = 1; i <= 100; ++i)
                Assert.AreEqual(i, list2[i - 1]);
            list.RemoveRange(10, 90);
            Assert.AreEqual(10, list.Count);
            for(int i = 1; i <= 10; ++i)
                Assert.AreEqual(i, list2[i - 1]);
        }

        [Test]
        public void TestCompatibilityWithArray()
        {
            var list = new List<string> {"1", "2"};
            byte[] data = serializer.Serialize(list);
            var array = serializer.Deserialize<string[]>(data);
            Assert.AreEqual(2, array.Length);
            Assert.AreEqual("1", array[0]);
            Assert.AreEqual("2", array[1]);
            array = new[] {"3", "2", "1"};
            data = serializer.Serialize(array);
            list = serializer.Deserialize<List<string>>(data);
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("3", list[0]);
            Assert.AreEqual("2", list[1]);
            Assert.AreEqual("1", list[2]);
        }

        [Test]
        public void TestCompatibilityWithArrayOfPrimitives()
        {
            var list = new List<int> {1, 2};
            byte[] data = serializer.Serialize(list);
            var array = serializer.Deserialize<int[]>(data);
            Assert.AreEqual(2, array.Length);
            Assert.AreEqual(1, array[0]);
            Assert.AreEqual(2, array[1]);
            array = new[] {3, 2, 1};
            data = serializer.Serialize(array);
            list = serializer.Deserialize<List<int>>(data);
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(3, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(1, list[2]);
        }

        [Test]
        public void TestPerformance()
        {
            var list = new List<int>();
            for(int i = 0; i < 10000; ++i)
                list.Add(i);

            var stream = new MemoryStream(128 * 1024);

            Console.WriteLine(serializer.GetSize(list));
            serializer.Deserialize<List<int>>(serializer.Serialize(list));
            Stopwatch stopwatch = Stopwatch.StartNew();
            const int iterations = 1000000;
            for(int iter = 0; iter < iterations; ++iter)
                serializer.GetSize(list);
            TimeSpan elapsed = stopwatch.Elapsed;
            Console.WriteLine("Size computing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " size computations per second)");
            stopwatch = Stopwatch.StartNew();
            for(int iter = 0; iter < iterations; ++iter)
            {
                stream.Position = 0;
                stream.SetLength(0);
                serializer.Serialize(list);
            }
            elapsed = stopwatch.Elapsed;
            Console.WriteLine("Serializing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " serializations per second)");
            byte[] buf = serializer.Serialize(list);
            stopwatch = Stopwatch.StartNew();
            for(int iter = 0; iter < iterations; ++iter)
            {
                stream.Position = 0;
                stream.SetLength(0);
                serializer.Deserialize<List<int>>(buf);
            }
            elapsed = stopwatch.Elapsed;
            Console.WriteLine("Deserializing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " deserializations per second)");
        }

        [Test]
        public void TestPerformanceGuid()
        {
            var list = new List<Guid>();
            for(int i = 0; i < 10000; ++i)
                list.Add(Guid.NewGuid());

            var stream = new MemoryStream(128 * 1024);

            Console.WriteLine(serializer.GetSize(list));
            serializer.Deserialize<List<Guid>>(serializer.Serialize(list));
            Stopwatch stopwatch = Stopwatch.StartNew();
            const int iterations = 10000;
            for(int iter = 0; iter < iterations; ++iter)
                serializer.GetSize(list);
            TimeSpan elapsed = stopwatch.Elapsed;
            Console.WriteLine("Size computing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " size computations per second)");
            stopwatch = Stopwatch.StartNew();
            for(int iter = 0; iter < iterations; ++iter)
            {
                stream.Position = 0;
                stream.SetLength(0);
                serializer.Serialize(list);
            }
            elapsed = stopwatch.Elapsed;
            Console.WriteLine("Serializing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " serializations per second)");
            byte[] buf = serializer.Serialize(list);
            stopwatch = Stopwatch.StartNew();
            for(int iter = 0; iter < iterations; ++iter)
            {
                stream.Position = 0;
                stream.SetLength(0);
                serializer.Deserialize<List<Guid>>(buf);
            }
            elapsed = stopwatch.Elapsed;
            Console.WriteLine("Deserializing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " deserializations per second)");
        }

        [Test]
        public void TestPerformanceProtobuf()
        {
            var list = new List<int>();
            for(int i = 0; i < 10000; ++i)
                list.Add(i);

            var stream = new MemoryStream(128 * 1024);
            ProtoBuf.Serializer.Serialize(stream, list);
            stream.Position = 0;
            stream.SetLength(0);
            ProtoBuf.Serializer.Deserialize<List<int>>(stream);

            const int iterations = 10000;
            Stopwatch stopwatch = Stopwatch.StartNew();
            for(int iter = 0; iter < iterations; ++iter)
            {
                stream.Position = 0;
                stream.SetLength(0);
                ProtoBuf.Serializer.Serialize(stream, list);
            }
            TimeSpan elapsed = stopwatch.Elapsed;
            Console.WriteLine("Serializing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " serializations per second)");
            stopwatch = Stopwatch.StartNew();
            for(int iter = 0; iter < iterations; ++iter)
            {
                stream.Position = 0;
                stream.SetLength(0);
                ProtoBuf.Serializer.Deserialize<List<int>>(stream);
            }
            elapsed = stopwatch.Elapsed;
            Console.WriteLine("Deserializing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " deserializations per second)");
        }

        [Test]
        public void TestListGetSizeBug()
        {
            serializer = new SerializerImpl(new PropertiesExtractor(), null, GroBufOptions.WriteEmptyObjects);
            var contract = new BadContract
                {
                    List = new List<Item>()
                };
            byte[] serialized = serializer.Serialize(contract);
            int size = serializer.GetSize(contract);
            Assert.AreEqual(serialized.Length, size);

            int index = 0;
            var dst = new byte[size];
            serializer.Serialize(contract, dst, ref index);
            Assert.AreEqual(index, size);
        }

        private SerializerImpl serializer;

        private class Item
        {
        }

        private class BadContract
        {
            public List<Item> List { get; set; }
        }
    }
}