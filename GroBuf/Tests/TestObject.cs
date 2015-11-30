using System;
using System.Collections;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestObject
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor());
        }

        [Test]
        public void Test1()
        {
            var o = new A {S = "zzz", B = new B {S = (byte)100}};
            byte[] data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            Assert.AreEqual("zzz", oo.S);
            Assert.AreEqual(100, oo.B.S);
        }

        [Test]
        public void Test2()
        {
            var o = new A {S = new Guid("aae41ff9-a337-4a69-b2ee-12ead4669201"), B = new B {S = new DateTime(182374682)}};
            byte[] data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            Assert.AreEqual(new Guid("aae41ff9-a337-4a69-b2ee-12ead4669201"), oo.S);
            Assert.AreEqual(new DateTime(182374682), oo.B.S);
        }

        [Test]
        public void TestDecimal()
        {
            var o = new A {S = 123m};
            byte[] data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            Assert.AreEqual(123m, oo.S);
        }

        [Test]
        public void TestOldDateTimeFormat()
        {
            var ticks = DateTime.UtcNow.Ticks;
            var data = new byte[9];
            data[0] = (byte)GroBufTypeCode.DateTimeOld;
            Array.Copy(BitConverter.GetBytes(ticks), 0, data, 1, 8);
            var result = serializer.Deserialize<object>(data);
            Assert.AreEqual(new DateTime(ticks, DateTimeKind.Utc), result);
        }

        [Test]
        public void TestArray()
        {
            var o = new A {S = "zzz", B = new B {S = new object[] {(byte)100, "qxx"}}};
            byte[] data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            Assert.AreEqual("zzz", oo.S);
            var array = oo.B.S as Array;
            Assert.NotNull(array);
            Assert.AreEqual(2, array.Length);
            Assert.AreEqual(100, array.GetValue(0));
            Assert.AreEqual("qxx", array.GetValue(1));
        }

        [Test]
        public void TestHashtable()
        {
            var o = new A {S = new Hashtable {{"1", 1}, {"2", "2"}}};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            var hashtable = oo.S as Hashtable;
            Assert.IsNotNull(hashtable);
            Assert.AreEqual(2, hashtable.Count);
            Assert.AreEqual(1, hashtable["1"]);
            Assert.AreEqual("2", hashtable["2"]);
        }

        [Test]
        public void TestHashtableInArray()
        {
            var o = new A { S = "zzz", B = new B { S = new object[] { (byte)100, "qxx", new Hashtable { { "1", 1 }, { "2", "2" } } } } };
            byte[] data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            Assert.AreEqual("zzz", oo.S);
            var array = oo.B.S as Array;
            Assert.NotNull(array);
            Assert.AreEqual(3, array.Length);
            Assert.AreEqual(100, array.GetValue(0));
            Assert.AreEqual("qxx", array.GetValue(1));
            var hashtable = array.GetValue(2) as Hashtable;
            Assert.IsNotNull(hashtable);
            Assert.AreEqual(2, hashtable.Count);
            Assert.AreEqual(1, hashtable["1"]);
            Assert.AreEqual("2", hashtable["2"]);
        }

        [Test]
        public void TestStrinArray()
        {
            var o = new A {S = new[] {"zzz", "qxx"}};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            Console.WriteLine(oo);
        }

        [Test]
        public void TestBad1()
        {
            var o = new A {S = "zzz", B = new B {S = new A {S = "qxx"}}, Z = "qxx"};
            byte[] data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            Assert.AreEqual("zzz", oo.S);
            Assert.AreEqual(null, oo.B.S);
            Assert.AreEqual("qxx", oo.Z);
        }

        [Test]
        public void TestBad2()
        {
            var o = new Az {S = "zzz", B = new Bz {S = new Cz {S = 100}}, Z = "qxx"};
            byte[] data = serializer.Serialize(o);
            var oo = serializer.Deserialize<A>(data);
            Assert.AreEqual("zzz", oo.S);
            Assert.AreEqual(null, oo.B.S);
            Assert.AreEqual("qxx", oo.Z);
        }

        public class A
        {
            public object S { get; set; }
            public B B { get; set; }
            public object Z { get; set; }
        }

        public class Az
        {
            public object S { get; set; }
            public Bz B { get; set; }
            public string Z { get; set; }
        }

        public class Cz
        {
            public byte S { get; set; }
        }

        public struct B
        {
            public object S { get; set; }
        }

        public struct Bz
        {
            public Cz S { get; set; }
        }

        private Serializer serializer;
    }
}