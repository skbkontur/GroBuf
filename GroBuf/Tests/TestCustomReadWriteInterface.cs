using System;

using GroBuf.DataMembersExtracters;
using GroBuf.Tests.TestTools;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestCustomReadWriteInterface
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor());
        }

        [Test]
        public void TestSizeCounter()
        {
            var a = new A {Z = new C {Z = "zzz"}};
            var size = serializer.GetSize(a);
            Assert.AreEqual((1 + 4 + 8) + (1 + 4 + 2) + (1 + 4 + 8 + 1 + 4 + 6) + 5, size);
        }

        [Test]
        public void TestWriter()
        {
            var a = new A {Z = new C {Z = "zzz"}};
            var data = serializer.Serialize(a);
            Assert.AreEqual((1 + 4 + 8) + (1 + 4 + 2) + (1 + 4 + 8 + 1 + 4 + 6) + 5, data.Length);
        }

        [Test]
        public void TestReaderC()
        {
            var a = new A {Z = new C {Z = "zzz"}, ArrZ = new IZ[] {new C {Z = "qxx"}, new D {Z = "123"}}};
            var data = serializer.Serialize(a);
            var za = serializer.Deserialize<A>(data);
            za.AssertEqualsTo(a);
        }

        [Test]
        public void TestReaderD()
        {
            var a = new A {Z = new D {Z = "146"}, ArrZ = new IZ[] {new C {Z = "qxx"}, new D {Z = "123"}}};
            var data = serializer.Serialize(a);
            var za = serializer.Deserialize<A>(data);
            za.AssertEqualsTo(a);
        }

        [Test]
        public void TestReaderCRoot()
        {
            IZ z = new C{Z = "zzz"};
            var data = serializer.Serialize(z);
            var zz = serializer.Deserialize<IZ>(data);
            zz.AssertEqualsTo(z);
        }

        public class A
        {
            public IZ Z { get; set; }
            public IZ[] ArrZ { get; set; }
        }

        public class B
        {
            [GroBufSizeCounter]
            public static SizeCounterDelegate GetSizeCounter(Func<Type, SizeCounterDelegate> sizeCountersFactory, SizeCounterDelegate baseSizeCounter)
            {
                return (o, writeEmpty) =>
                           {
                               Type type = o.GetType();
                               return sizeCountersFactory(typeof(string))(type.Name, writeEmpty) + sizeCountersFactory(type)(o, writeEmpty);
                           };
            }

            [GroBufWriter]
            public static WriterDelegate GetWriter(Func<Type, WriterDelegate> writersFactory, WriterDelegate baseWriter)
            {
                return (object o, bool writeEmpty, IntPtr result, ref int index, int resultLength) =>
                           {
                               Type type = o.GetType();
                               writersFactory(typeof(string))(type.Name, writeEmpty, result, ref index, resultLength);
                               writersFactory(type)(o, writeEmpty, result, ref index, resultLength);
                           };
            }

            [GroBufReader]
            public static ReaderDelegate GetReader(Func<Type, ReaderDelegate> readersFactory, ReaderDelegate baseReader)
            {
                return (IntPtr data, ref int index, int length, ref object result) =>
                           {
                               object type = null;
                               readersFactory(typeof(string))(data, ref index, length, ref type);
                               if((string)type == typeof(C).Name)
                                   result = new C();
                               else if((string)type == typeof(D).Name)
                                   result = new D();
                               else throw new InvalidOperationException("Unknown type " + type);
                               readersFactory(result.GetType())(data, ref index, length, ref result);
                           };
            }
        }

        public class C : IZ
        {
            public string Z { get; set; }
        }

        public class D : IZ
        {
            public string Z { get; set; }
        }

        [GroBufCustomSerialization(typeof(B))]
        public interface IZ
        {
            string Z { get; set; }
        }

        private Serializer serializer;
    }
}