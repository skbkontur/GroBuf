using System;
using System.Diagnostics;
using System.Text;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestStrings
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor());
        }

        [Test]
        public void TestPerformance()
        {
            const int length = 32;
            var arr = new char[length];
            for (int i = 0; i < length; ++i)
                arr[i] = (char)('a' + i % 26);
            var s = new string(arr);
            byte[] data = serializer.Serialize(s);
            var buf = Encoding.UTF8.GetBytes(s);
            const int iterations = 100000;
            long size = 0;
            var stopwatch = Stopwatch.StartNew();
            for(int i = 0; i < iterations; ++i)
            {
                //data = Encoding.UTF8.GetBytes(s);//serializer.Serialize(s);
                s = Encoding.UTF8.GetString(buf);
                size += s.Length;
            }
            var elapsed = stopwatch.Elapsed;
            Console.WriteLine("Serializing: " + elapsed.TotalMilliseconds * 1000 / iterations + " microseconds (" + Math.Round(1000.0 * iterations / elapsed.TotalMilliseconds) + " serializations per second)");
            Console.WriteLine("Size: " + ((double)size) / iterations + " bytes");
        }

        [Test]
        public void TestString()
        {
            const string s = "zzz ароваро \u2376 \uDEAD";
            byte[] bytes = serializer.Serialize(s);
            var deserialize = serializer.Deserialize<string>(bytes);
            Assert.AreEqual("zzz ароваро \u2376 \uDEAD", deserialize);
        }

        [Test]
        public void TestString1()
        {
            const string s = "zzz ароваро \u2376 \uDEAD";
            byte[] bytes = serializer.Serialize(typeof(string), s);
            var deserialize = serializer.Deserialize(typeof(string), bytes);
            Assert.AreEqual("zzz ароваро \u2376 \uDEAD", deserialize);
        }

        [Test]
        public void TestStringInProp()
        {
            const string s = "zzz ароваро \u2376 \uDEAD";
            byte[] bytes = serializer.Serialize(new WithS {S = s});
            var deserialize = serializer.Deserialize<WithS>(bytes);
            Assert.AreEqual("zzz ароваро \u2376 \uDEAD", deserialize.S);
        }

        [Test]
        public void TestStringNull()
        {
            byte[] bytes = serializer.Serialize<string>(null);
            var deserialize = serializer.Deserialize<string>(bytes);
            Assert.AreEqual(null, deserialize);
        }

        [Test]
        public void TestStringNullInProp()
        {
            byte[] bytes = serializer.Serialize(new WithS());
            var deserialize = serializer.Deserialize<WithS>(bytes);
            Assert.AreEqual(null, deserialize.S);
        }

        [Test]
        public void TestStringEmpty()
        {
            byte[] bytes = serializer.Serialize("");
            var deserialize = serializer.Deserialize<string>(bytes);
            Assert.AreEqual("", deserialize);
        }

        [Test]
        public void TestStringEmptyInProp()
        {
            byte[] bytes = serializer.Serialize(new WithS {S = ""});
            var deserialize = serializer.Deserialize<WithS>(bytes);
            Assert.AreEqual("", deserialize.S);
        }

        public class WithS
        {
            public string S { get; set; }
        }

        private Serializer serializer;
    }
}