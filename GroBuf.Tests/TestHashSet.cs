using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using FluentAssertions;

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
            serializer = new Serializer(new PropertiesExtractor());
        }

        [Test]
        public void TestGetSize()
        {
            var hashSet = new HashSet<string> {"1", "2"};
            int size = serializer.GetSize(hashSet);
            Console.WriteLine(size);
        }

        [Test]
        public void TestWriteNotEmptyWithWriteEmptyObjects()
        {
            serializer = new Serializer(new PropertiesExtractor(), null, GroBufOptions.WriteEmptyObjects);
            var arr = new[] {Guid.NewGuid(), Guid.NewGuid()};

            var c = new TestWithHashSet {H = new HashSet<Guid>(arr)};
            byte[] bytes = serializer.Serialize(c);
            var actual = serializer.Deserialize<TestWithHashSet>(bytes);
            Assert.IsNotNull(actual.H);
            CollectionAssert.AreEquivalent(arr, actual.H.ToArray());
        }

        [Test]
        public void TestWriteEmptyWithWriteEmptyObjects()
        {
            serializer = new Serializer(new PropertiesExtractor(), null, GroBufOptions.WriteEmptyObjects);
            var c = new TestWithHashSet {H = new HashSet<Guid>()};
            byte[] bytes = serializer.Serialize(c);
            var actual = serializer.Deserialize<TestWithHashSet>(bytes);
            Assert.IsNotNull(actual.H);
            CollectionAssert.IsEmpty(actual.H.ToArray());
        }

        [Test]
        public void TestWriteEmptyWithWriteEmptyObjectsPrimitives()
        {
            serializer = new Serializer(new PropertiesExtractor(), null, GroBufOptions.WriteEmptyObjects);
            var c = new TestWithHashSet {H2 = new HashSet<int>()};
            byte[] bytes = serializer.Serialize(c);
            var actual = serializer.Deserialize<TestWithHashSet>(bytes);
            Assert.IsNotNull(actual.H2);
            CollectionAssert.IsEmpty(actual.H2.ToArray());
        }

        [Test]
        public void TestGetSizePrimitive()
        {
            var hashSet = new HashSet<int> {1, 2};
            int size = serializer.GetSize(hashSet);
            Console.WriteLine(size);
        }

        [Test]
        public void TestWrite()
        {
            var hashSet = new HashSet<string> {"1", "2"};
            byte[] buf = serializer.Serialize(hashSet);
            Console.WriteLine(buf.Length);
        }

        [Test]
        public void TestWritePrimitive()
        {
            var hashSet = new HashSet<int> {1, 2};
            byte[] buf = serializer.Serialize(hashSet);
            Console.WriteLine(buf.Length);
        }

        [Test]
        public void TestRead()
        {
            var hashSet = new HashSet<string> {"1", "2"};
            var buf = serializer.Serialize(hashSet);
            var hashSet2 = serializer.Deserialize<HashSet<string>>(buf);
            Assert.AreEqual(2, hashSet2.Count);
            Assert.IsTrue(hashSet2.Contains("1"));
            Assert.IsTrue(hashSet2.Contains("2"));
        }

        [Test]
        public void TestAddRemoveElements()
        {
            var hashSet = new HashSet<string> {"1", "2"};
            hashSet.Remove("1");
            var buf = serializer.Serialize(hashSet);
            var hashSet2 = serializer.Deserialize<HashSet<string>>(buf);
            Assert.AreEqual(1, hashSet2.Count);
            Assert.IsTrue(hashSet2.Contains("2"));
        }

        [Test]
        public void TestAddRemoveElementsPrimitive()
        {
            var hashSet = new HashSet<int> {1, 2};
            hashSet.Remove(1);
            byte[] buf = serializer.Serialize(hashSet);
            var hashSet2 = serializer.Deserialize<HashSet<int>>(buf);
            Assert.AreEqual(1, hashSet2.Count);
            Assert.IsTrue(hashSet2.Contains(2));
        }

        [Test]
        public void TestReadPrimitive()
        {
            var hashSet = new HashSet<int> {1, 2};
            byte[] buf = serializer.Serialize(hashSet);
            var hashSet2 = serializer.Deserialize<HashSet<int>>(buf);
            Assert.AreEqual(2, hashSet2.Count);
            Assert.IsTrue(hashSet2.Contains(1));
            Assert.IsTrue(hashSet2.Contains(2));
        }

        [Test]
        public void TestCompatibilityWithArray()
        {
            var hashSet = new HashSet<string> {"1", "2"};
            byte[] data = serializer.Serialize(hashSet);
            var array = serializer.Deserialize<string[]>(data);
            Assert.AreEqual(2, array.Length);
            Assert.AreEqual("1", array[0]);
            Assert.AreEqual("2", array[1]);
            array = new[] {"3", "2", "1"};
            data = serializer.Serialize(array);
            hashSet = serializer.Deserialize<HashSet<string>>(data);
            Assert.AreEqual(3, hashSet.Count);
            Assert.IsTrue(hashSet.Contains("1"));
            Assert.IsTrue(hashSet.Contains("2"));
            Assert.IsTrue(hashSet.Contains("3"));
        }

        [Test]
        public void TestCompatibilityWithArrayOfPrimitives()
        {
            var hashSet = new HashSet<int> {1, 2};
            byte[] data = serializer.Serialize(hashSet);
            var array = serializer.Deserialize<int[]>(data);
            Assert.AreEqual(2, array.Length);
            Assert.AreEqual(1, array[0]);
            Assert.AreEqual(2, array[1]);
            array = new[] {3, 2, 1};
            data = serializer.Serialize(array);
            hashSet = serializer.Deserialize<HashSet<int>>(data);
            Assert.AreEqual(3, hashSet.Count);
            Assert.IsTrue(hashSet.Contains(1));
            Assert.IsTrue(hashSet.Contains(2));
            Assert.IsTrue(hashSet.Contains(3));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void TestBackwardsCompatibility(int i, bool generateFiles = false)
        {
            var rnd = new Random(i);
            var count = rnd.Next(1, 100);
            var set = new HashSet<Zzz>();
            for (int j = 0; j < count; j++)
                set.Add(new Zzz
                    {
                        Id = $"{i}{j}{count}{rnd.Next(1000, 10000)}",
                        Properties = new Dictionary<string, byte[]>
                            {
                                {rnd.Next(1000, 10000).ToString(), new byte[] {5, 4}}
                            }
                    });

            var buf = serializer.Serialize(set);

            if (generateFiles)
                File.WriteAllBytes($"{TestContext.CurrentContext.TestDirectory}/../../../Files/set{i}.buf", buf);

            var expectedBuf = File.ReadAllBytes($"{TestContext.CurrentContext.TestDirectory}/Files/set{i}.buf");
            buf.Should().BeEquivalentTo(expectedBuf);

            var set2 = serializer.Deserialize<HashSet<Zzz>>(buf);
            set2.Should().BeEquivalentTo(set);
        }

        [Test]
        [Category("LongRunning")]
        public void TestPerformance()
        {
            var hashSet = new HashSet<int>();
            for (int i = 0; i < 10000; ++i)
                hashSet.Add(i);
            Console.WriteLine(serializer.GetSize(hashSet));
            serializer.Deserialize<HashSet<int>>(serializer.Serialize(hashSet));
            Stopwatch stopwatch = Stopwatch.StartNew();
            const int iterations = 10000;
            for (int iter = 0; iter < iterations; ++iter)
                serializer.GetSize(hashSet);
            TimeSpan elapsed = stopwatch.Elapsed;
            Console.WriteLine("Size computing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " size computations per second)");
            stopwatch = Stopwatch.StartNew();
            for (int iter = 0; iter < iterations; ++iter)
                serializer.Serialize(hashSet);
            elapsed = stopwatch.Elapsed;
            Console.WriteLine("Serializing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " serializations per second)");
            byte[] buf = serializer.Serialize(hashSet);
            stopwatch = Stopwatch.StartNew();
            for (int iter = 0; iter < iterations; ++iter)
                serializer.Deserialize<HashSet<int>>(buf);
            elapsed = stopwatch.Elapsed;
            Console.WriteLine("Deserializing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " deserializations per second)");
        }

        [Test]
        [Category("LongRunning")]
        public void TestPerformanceGuid()
        {
            var hashSet = new HashSet<Guid>();
            for (int i = 0; i < 10000; ++i)
                hashSet.Add(Guid.NewGuid());
            Console.WriteLine(serializer.GetSize(hashSet));
            serializer.Deserialize<HashSet<Guid>>(serializer.Serialize(hashSet));
            Stopwatch stopwatch = Stopwatch.StartNew();
            const int iterations = 10000;
            for (int iter = 0; iter < iterations; ++iter)
                serializer.GetSize(hashSet);
            TimeSpan elapsed = stopwatch.Elapsed;
            Console.WriteLine("Size computing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " size computations per second)");
            stopwatch = Stopwatch.StartNew();
            for (int iter = 0; iter < iterations; ++iter)
                serializer.Serialize(hashSet);
            elapsed = stopwatch.Elapsed;
            Console.WriteLine("Serializing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " serializations per second)");
            byte[] buf = serializer.Serialize(hashSet);
            stopwatch = Stopwatch.StartNew();
            for (int iter = 0; iter < iterations; ++iter)
                serializer.Deserialize<HashSet<Guid>>(buf);
            elapsed = stopwatch.Elapsed;
            Console.WriteLine("Deserializing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " deserializations per second)");
        }

        private Serializer serializer;

        private class Zzz
        {
            public Zzz()
            {
                Properties = new Dictionary<string, byte[]>();
            }

            protected bool Equals(Zzz other)
            {
                return Id == other.Id;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Zzz)obj);
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }

            public string Id { get; set; }
            public Dictionary<string, byte[]> Properties { get; set; }
        }

        private class TestWithHashSet
        {
            public HashSet<Guid> H { get; set; }
            public HashSet<int> H2 { get; set; }
        }
    }
}