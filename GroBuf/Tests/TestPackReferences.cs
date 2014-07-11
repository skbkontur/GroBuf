using System;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestPackReferences
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor());
        }

        [Test]
        public void Test1()
        {
            Console.WriteLine(serializer.GetSize(new TestClassA { S = "zzz", B = new TestClassB { S = "qxx" } }));
            Console.WriteLine(serializer.GetSize(new TestClassA { S = "zzz", B = new TestClassB { S = "zzz" } }));
            var data = serializer.Serialize(new TestClassA { S = "zzz", B = new TestClassB { S = "qxx" } });
            Console.WriteLine(data.Length);
            var data2 = serializer.Serialize(new TestClassA { S = "zzz", B = new TestClassB { S = "zzz" } });
            Console.WriteLine(data2.Length);
            var obj = serializer.Deserialize<TestClassA>(data2);
            Assert.AreEqual("zzz", obj.S);
            Assert.IsNotNull(obj.B);
            Assert.AreEqual("zzz", obj.B.S);
        }

        [Test]
        public void Test2()
        {
            var o = new TestClassA();
            o.A = o;
            var data = serializer.Serialize(o);
            Console.WriteLine(data.Length);
            var oo = serializer.Deserialize<TestClassA>(data);
            Assert.AreSame(oo, oo.A);
        }

        [Test]
        public void Test3()
        {
            var b = new TestClassB{S = "zzz"};
            var a = new TestClassA{B = b, ArrayB = new[] {b}, S = "zzz"};
            a.A = a;
            var data = serializer.Serialize(a);
            Console.WriteLine(data.Length);
            var aa = serializer.Deserialize<TestClassA>(data);
            Assert.AreSame(aa, aa.A);
            Assert.IsNotNull(aa.ArrayB);
            Assert.AreEqual(1, aa.ArrayB.Length);
            Assert.IsNotNull(aa.B);
            Assert.AreEqual("zzz", aa.B.S);
            Assert.AreEqual("zzz", aa.S);
            Assert.AreSame(aa.B, aa.ArrayB[0]);
        }

        [Test]
        public void TestRemovedProperty()
        {
            var b = new TestClassB { S = "zzz" };
            var a = new TestClassA { B = b, ArrayB = new[] { b }, S = "zzz" };
            a.A = a;
            var data = serializer.Serialize(a);
            Console.WriteLine(data.Length);
            var aa = serializer.Deserialize<TestClassAChanged>(data);
            Assert.AreSame(aa, aa.A);
            Assert.IsNotNull(aa.ArrayB);
            Assert.AreEqual(1, aa.ArrayB.Length);
            Assert.AreEqual("zzz", aa.S);
            Assert.IsNotNull(aa.ArrayB[0]);
            Assert.AreEqual("zzz", aa.ArrayB[0].S);
        }

        [Test]
        public void TestTwoObjects()
        {
            var b = new TestClassB { S = "zzz" };
            var a = new TestClassA { B = b, ArrayB = new[] { b }, S = "zzz" };
            a.A = a;
            var data = serializer.Serialize(a, a);
            int index = 0;
            var aa = serializer.Deserialize<TestClassA>(data, ref index, data.Length);
            var aaa = serializer.Deserialize<TestClassAChanged>(data, ref index, data.Length);
            Assert.AreSame(aa, aa.A);
            Assert.IsNotNull(aa.ArrayB);
            Assert.AreEqual(1, aa.ArrayB.Length);
            Assert.IsNotNull(aa.B);
            Assert.AreEqual("zzz", aa.B.S);
            Assert.AreEqual("zzz", aa.S);
            Assert.AreSame(aa.B, aa.ArrayB[0]);
            Assert.AreSame(aaa, aaa.A);
            Assert.IsNotNull(aaa.ArrayB);
            Assert.AreEqual(1, aaa.ArrayB.Length);
            Assert.AreEqual("zzz", aaa.S);
            Assert.IsNotNull(aaa.ArrayB[0]);
            Assert.AreEqual("zzz", aaa.ArrayB[0].S);
        }

        public class TestClassA
        {
            public TestClassB B { get; set; }
            public TestClassA A { get; set; }
            public string S { get; set; }
            public TestClassB[] ArrayB { get; set; }
        }

        public class TestClassAChanged
        {
            public TestClassAChanged A { get; set; }
            public string S { get; set; }
            public TestClassB[] ArrayB { get; set; }
        }

        public class TestClassB
        {
            public string S { get; set; }
        }

        private Serializer serializer;
    }
}