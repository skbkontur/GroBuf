using System;
using System.Collections.Generic;
using System.Diagnostics;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestHashSet
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new SerializerImpl(new PropertiesExtractor());
        }

        [Test]
        public void TestGetSize()
        {
            var hashSet = new HashSet<string> {"1", "2"};
            var size = serializer.GetSize(hashSet);
            Console.WriteLine(size);
        }

        [Test]
        public void TestGetSizePrimitive()
        {
            var hashSet = new HashSet<int> {1, 2};
            var size = serializer.GetSize(hashSet);
            Console.WriteLine(size);
        }

        [Test]
        public void TestWrite()
        {
            var hashSet = new HashSet<string> { "1", "2" };
            var buf = serializer.Serialize(hashSet);
            Console.WriteLine(buf.Length);
        }

        [Test]
        public void TestWritePrimitive()
        {
            var hashSet = new HashSet<int> { 1, 2 };
            var buf = serializer.Serialize(hashSet);
            Console.WriteLine(buf.Length);
        }

        [Test]
        public void TestRead()
        {
            var hashSet = new HashSet<string> { "1", "2" };
            var buf = serializer.Serialize(hashSet);
            var hashSet2 = serializer.Deserialize<HashSet<string>>(buf);
            Assert.AreEqual(2, hashSet2.Count);
            Assert.IsTrue(hashSet2.Contains("1"));
            Assert.IsTrue(hashSet2.Contains("2"));
        }

        [Test]
        public void TestReadPrimitive()
        {
            var hashSet = new HashSet<int> { 1, 2 };
            var buf = serializer.Serialize(hashSet);
            var hashSet2 = serializer.Deserialize<HashSet<int>>(buf);
            Assert.AreEqual(2, hashSet2.Count);
            Assert.IsTrue(hashSet2.Contains(1));
            Assert.IsTrue(hashSet2.Contains(2));
        }

        [Test]
        public void TestPerformance()
        {
            var hashSet = new HashSet<int>();
            for(int i = 0; i < 10000; ++i)
                hashSet.Add(i);
            Console.WriteLine(serializer.GetSize(hashSet));
            serializer.Deserialize<HashSet<int>>(serializer.Serialize(hashSet));
            var stopwatch = Stopwatch.StartNew();
            const int iterations = 10000;
            for(int iter = 0; iter < iterations; ++iter)
                serializer.GetSize(hashSet);
            var elapsed = stopwatch.Elapsed;
            Console.WriteLine("Size computing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " size computations per second)");
            stopwatch = Stopwatch.StartNew();
            for(int iter = 0; iter < iterations; ++iter)
                serializer.Serialize(hashSet);
            elapsed = stopwatch.Elapsed;
            Console.WriteLine("Serializing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " serializations per second)");
            var buf = serializer.Serialize(hashSet);
            stopwatch = Stopwatch.StartNew();
            for(int iter = 0; iter < iterations; ++iter)
                serializer.Deserialize<HashSet<int>>(buf);
            elapsed = stopwatch.Elapsed;
            Console.WriteLine("Deserializing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " deserializations per second)");
        }

        [Test]
        public void TestPerformanceGuid()
        {
            var hashSet = new HashSet<Guid>();
            for(int i = 0; i < 10000; ++i)
                hashSet.Add(Guid.NewGuid());
            Console.WriteLine(serializer.GetSize(hashSet));
            serializer.Deserialize<HashSet<Guid>>(serializer.Serialize(hashSet));
            var stopwatch = Stopwatch.StartNew();
            const int iterations = 10000;
            for(int iter = 0; iter < iterations; ++iter)
                serializer.GetSize(hashSet);
            var elapsed = stopwatch.Elapsed;
            Console.WriteLine("Size computing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " size computations per second)");
            stopwatch = Stopwatch.StartNew();
            for(int iter = 0; iter < iterations; ++iter)
                serializer.Serialize(hashSet);
            elapsed = stopwatch.Elapsed;
            Console.WriteLine("Serializing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " serializations per second)");
            var buf = serializer.Serialize(hashSet);
            stopwatch = Stopwatch.StartNew();
            for(int iter = 0; iter < iterations; ++iter)
                serializer.Deserialize<HashSet<Guid>>(buf);
            elapsed = stopwatch.Elapsed;
            Console.WriteLine("Deserializing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " deserializations per second)");
        }

        private SerializerImpl serializer;
    }
}