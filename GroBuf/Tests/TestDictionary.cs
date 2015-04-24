using System;
using System.Collections.Generic;
using System.Diagnostics;

using GroBuf.DataMembersExtractors;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestDictionary
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor());
        }

        [Test]
        public void TestGetSize()
        {
            var dict = new Dictionary<string, int> {{"1", 1}, {"2", 2}};
            var size = serializer.GetSize(dict);
            Console.WriteLine(size);
        }

        [Test]
        public void TestWrite()
        {
            var dict = new Dictionary<string, int> {{"1", 1}, {"2", 2}};
            var buf = serializer.Serialize(dict);
            Console.WriteLine(buf.Length);
        }

        [Test]
        public void TestRead()
        {
            var dict = new Dictionary<string, int> {{"1", 1}, {"2", 2}};
            var buf = serializer.Serialize(dict);
            var dict2 = serializer.Deserialize<Dictionary<string, int>>(buf);
            Assert.AreEqual(2, dict2.Count);
            Assert.AreEqual(1, dict2["1"]);
            Assert.AreEqual(2, dict2["2"]);
        }

        [Test]
        public void TestArray()
        {
            var zzz = new Zzz();
            zzz.Properties.Add("zzz", serializer.Serialize(true));
            zzz.Properties.Add("qxx", serializer.Serialize(false));
            var data = serializer.Serialize(zzz);
            var zzz3 = serializer.Deserialize<Zzz>(data);
            Assert.AreEqual(2, zzz3.Properties.Count);
            Assert.AreEqual(true, serializer.Deserialize<bool>(zzz3.Properties["zzz"]));
            Assert.AreEqual(false, serializer.Deserialize<bool>(zzz3.Properties["qxx"]));
        }

        [Test]
        public void TestPerformance()
        {
            var dict = new Dictionary<int, int>();
            for(int i = 0; i < 10000; ++i)
                dict.Add(i, i);
            Console.WriteLine(serializer.GetSize(dict));
            serializer.Deserialize<Dictionary<int, int>>(serializer.Serialize(dict));
            var stopwatch = Stopwatch.StartNew();
            const int iterations = 10000;
            for(int iter = 0; iter < iterations; ++iter)
                serializer.GetSize(dict);
            var elapsed = stopwatch.Elapsed;
            Console.WriteLine("Size computing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " size computations per second)");
            stopwatch = Stopwatch.StartNew();
            for(int iter = 0; iter < iterations; ++iter)
                serializer.Serialize(dict);
            elapsed = stopwatch.Elapsed;
            Console.WriteLine("Serializing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " serializations per second)");
            var buf = serializer.Serialize(dict);
            stopwatch = Stopwatch.StartNew();
            for(int iter = 0; iter < iterations; ++iter)
                serializer.Deserialize<Dictionary<int, int>>(buf);
            elapsed = stopwatch.Elapsed;
            Console.WriteLine("Deserializing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " deserializations per second)");
        }

        public class Zzz
        {
            public Zzz()
            {
                Properties = new Dictionary<string, byte[]>();
            }

            public Dictionary<string, byte[]> Properties { get; set; }
        }

        private Serializer serializer;
    }
}