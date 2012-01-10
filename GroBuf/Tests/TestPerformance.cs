using System;
using System.IO;
using System.Xml.Serialization;

using NUnit.Framework;

using SKBKontur.GroBuf.Tests.TestData.Invoic;
using SKBKontur.GroBuf.Tests.TestData.Orders;
using SKBKontur.GroBuf.Tests.TestTools;

namespace SKBKontur.GroBuf.Tests
{
    [TestFixture]
    public class TestPerformance
    {
        [SetUp]
        public void SetUp()
        {
            groBuf = new Serializer();
            ordersXmlSerializer = new XmlSerializer(typeof(Orders));
            invoicXmlSerializer = new XmlSerializer(typeof(Invoic));
        }

        [Test]
        public void TestGroBuf()
        {
            Console.WriteLine("GroBuf all types:");
            DoTest(100, data => groBuf.Serialize(data), data => groBuf.Deserialize<Orders>(data));
            /*Console.WriteLine();
            Console.WriteLine("GroBuf strings:");
            DoTest(100, data => groBuf.Serialize(data), data => groBuf.Deserialize<Invoic>(data));*/
        }

        [Test]
        public void TestProtoBuf()
        {
            Console.WriteLine("ProtoBuf all types:");
            DoTest(10, SerializeProtoBuf, DeserializeProtoBuf<Orders>);
            Console.WriteLine();
            Console.WriteLine("ProtoBuf strings:");
            DoTest(10, SerializeProtoBuf, DeserializeProtoBuf<Invoic>);
        }

        [Test]
        public void TestXmlSerializer()
        {
            Console.WriteLine("XmlSerializer all types:");
            DoTest(10, SerializeXmlOrders, DeserializeXmlOrders);
            Console.WriteLine();
            Console.WriteLine("XmlSerializer strings:");
            DoTest(10, SerializeXmlInvoic, DeserializeXmlInvoic);
        }

        static readonly MemoryStream ztream = new MemoryStream(128 * 1024);

        private static byte[] SerializeProtoBuf<T>(T obj)
        {
            //var stream = new MemoryStream(1024);
            ztream.Position = 0;
            ztream.SetLength(0);
            ProtoBuf.Serializer.Serialize(ztream, obj);
            return ztream.ToArray();
        }

        private static T DeserializeProtoBuf<T>(byte[] data)
        {
            return ProtoBuf.Serializer.Deserialize<T>(new MemoryStream(data));
        }

        private byte[] SerializeXmlOrders(Orders obj)
        {
            var stream = new MemoryStream(1024);
            ordersXmlSerializer.Serialize(stream, obj);
            return stream.ToArray();
        }

        private byte[] SerializeXmlInvoic(Invoic obj)
        {
            var stream = new MemoryStream(1024);
            invoicXmlSerializer.Serialize(stream, obj);
            return stream.ToArray();
        }

        private Orders DeserializeXmlOrders(byte[] data)
        {
            var stream = new MemoryStream(data);
            return (Orders)ordersXmlSerializer.Deserialize(stream);
        }

        private Invoic DeserializeXmlInvoic(byte[] data)
        {
            var stream = new MemoryStream(data);
            return (Invoic)invoicXmlSerializer.Deserialize(stream);
        }

        private static void DoTest<TData>(int iterations, Func<TData, byte[]> serializer, Func<byte[], TData> deserializer) where TData : class, new()
        {
            const int numberOfObjects = 1000;
            var random = new Random(54717651);
            var objects = new TData[numberOfObjects];
            objects[0] = TestHelpers.GenerateRandomTrash<TData>(random);
            for(int i = 1; i < objects.Length; ++i)
                objects[i] = TestHelpers.GenerateRandomTrash<TData>(random);

            var datas = new byte[numberOfObjects][];
            long size = 0;
            DateTime start = DateTime.Now;
            for(int i = 0; i < objects.Length; ++i)
            {
                byte[] cur = null;
                for(int j = 0; j < iterations; ++j)
                {
                    cur = serializer(objects[(i + j) % objects.Length]);
                    size += cur.Length;
                }
                datas[i] = cur;
            }
            TimeSpan elapsed = DateTime.Now - start;
            Console.WriteLine("Serializing: " + elapsed.TotalMilliseconds * 1000 / numberOfObjects / iterations + " microseconds (" + Math.Round(1000.0 * numberOfObjects * iterations / elapsed.TotalMilliseconds) + " serializations per second)");
            Console.WriteLine("Size: " + ((double)size) / numberOfObjects / iterations + " bytes");

            var deserializedDatas = new TData[numberOfObjects];
            start = DateTime.Now;
            for(int i = 0; i < datas.Length; ++i)
            {
                TData cur = null;
                for(int j = 0; j < iterations; ++j)
                    cur = deserializer(datas[(i + j) % datas.Length]);
                deserializedDatas[i] = cur;
            }
            elapsed = DateTime.Now - start;
            Console.WriteLine("Deserializing: " + elapsed.TotalMilliseconds * 1000 / numberOfObjects / iterations + " microseconds (" + Math.Round(1000.0 * numberOfObjects * iterations / elapsed.TotalMilliseconds) + " deserializations per second)");
        }

        private Serializer groBuf;
        private XmlSerializer ordersXmlSerializer;
        private XmlSerializer invoicXmlSerializer;
    }
}