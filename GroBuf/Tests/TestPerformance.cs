using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Xml.Serialization;

using GroBuf.Tests.TestData.Invoic;
using GroBuf.Tests.TestData.Orders;
using GroBuf.Tests.TestTools;

using NUnit.Framework;

namespace GroBuf.Tests
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
            ordersJsonSerializer = new DataContractJsonSerializer(typeof(Orders));
            invoicJsonSerializer = new DataContractJsonSerializer(typeof(Invoic));
        }

        [Test]
        public void TestBigGroBuf()
        {
            Console.WriteLine("GroBuf big data: all types");
            DoTest(10, 1000, 5, data => groBuf.Serialize(data), data => groBuf.Deserialize<Orders>(data));
            Console.WriteLine();
            Console.WriteLine("GroBuf big data: strings");
            DoTest(10, 1000, 5, data => groBuf.Serialize(data), data => groBuf.Deserialize<Invoic>(data));
        }           

        [Test]
        public void TestSmallGroBuf()
        {
            Console.WriteLine("GroBuf small data: all types");
            DoTest(1000, 10, 2, data => groBuf.Serialize(data), data => groBuf.Deserialize<Orders>(data));
            Console.WriteLine();
            Console.WriteLine("GroBuf small data: strings");
            DoTest(1000, 10, 2, data => groBuf.Serialize(data), data => groBuf.Deserialize<Invoic>(data));
        }

        [Test]
        public void TestGroBufGetSizeBig()
        {
            Console.WriteLine("GroBuf.GetSize big data: all types");
            DoTestGetSize<Orders>(1000, 1000, 5, data => groBuf.GetSize(data));
            Console.WriteLine();
            Console.WriteLine("GroBuf.GetSize big data: strings");
            DoTestGetSize<Invoic>(1000, 1000, 5, data => groBuf.GetSize(data));
        }

        [Test]
        public void TestGroBufGetSizeSmall()
        {
            Console.WriteLine("GroBuf.GetSize small data: all types");
            DoTestGetSize<Orders>(10000, 10, 2, data => groBuf.GetSize(data));
            Console.WriteLine();
            Console.WriteLine("GroBuf.GetSize small data: strings");
            DoTestGetSize<Invoic>(10000, 10, 2, data => groBuf.GetSize(data));
        }

        [Test]
        public void TestBigProtoBuf()
        {
            Console.WriteLine("ProtoBuf big data: all types");
            DoTest(10, 1000, 5, SerializeProtoBuf, DeserializeProtoBuf<Orders>);
            Console.WriteLine();
            Console.WriteLine("ProtoBuf big data: strings:");
            DoTest(10, 1000, 5, SerializeProtoBuf, DeserializeProtoBuf<Invoic>);
        }

        [Test]
        public void TestSmallProtoBuf()
        {
            Console.WriteLine("ProtoBuf small data: all types");
            DoTest(1000, 10, 2, SerializeProtoBuf, DeserializeProtoBuf<Orders>);
            Console.WriteLine();
            Console.WriteLine("ProtoBuf small data: strings:");
            DoTest(1000, 10, 2, SerializeProtoBuf, DeserializeProtoBuf<Invoic>);
        }

        [Test]
        public void TestBigXmlSerializer()
        {
            Console.WriteLine("XmlSerializer big data: all types");
            DoTest(10, 1000, 5, SerializeXmlOrders, DeserializeXmlOrders);
            Console.WriteLine();
            Console.WriteLine("XmlSerializer big data: strings");
            DoTest(10, 1000, 5, SerializeXmlInvoic, DeserializeXmlInvoic);
        }

        [Test]
        public void TestSmallXmlSerializer()
        {
            Console.WriteLine("XmlSerializer small data: all types");
            DoTest(100, 10, 2, SerializeXmlOrders, DeserializeXmlOrders);
            Console.WriteLine();
            Console.WriteLine("XmlSerializer small data: strings");
            DoTest(100, 10, 2, SerializeXmlInvoic, DeserializeXmlInvoic);
        }

        [Test]
        public void TestBigJsonSerializer()
        {
            Console.WriteLine("JsonSerializer big data: all types");
            DoTest(10, 1000, 5, SerializeJsonOrders, DeserializeJsonOrders);
            Console.WriteLine();
            Console.WriteLine("JsonSerializer big data: strings");
            DoTest(10, 1000, 5, SerializeJsonInvoic, DeserializeJsonInvoic);
        }

        [Test]
        public void TestSmallJsonSerializer()
        {
            Console.WriteLine("JsonSerializer small data: all types");
            DoTest(100, 10, 2, SerializeJsonOrders, DeserializeJsonOrders);
            Console.WriteLine();
            Console.WriteLine("JsonSerializer small data: strings");
            DoTest(100, 10, 2, SerializeJsonInvoic, DeserializeJsonInvoic);
        }

        private static byte[] SerializeProtoBuf<T>(T obj)
        {
            stream.Position = 0;
            stream.SetLength(0);
            ProtoBuf.Serializer.Serialize(stream, obj);
            return stream.ToArray();
        }

        private static T DeserializeProtoBuf<T>(byte[] data)
        {
            return ProtoBuf.Serializer.Deserialize<T>(new MemoryStream(data));
        }

        private byte[] SerializeXmlOrders(Orders obj)
        {
            stream.Position = 0;
            stream.SetLength(0);
            ordersXmlSerializer.Serialize(stream, obj);
            return stream.ToArray();
        }

        private byte[] SerializeXmlInvoic(Invoic obj)
        {
            stream.Position = 0;
            stream.SetLength(0);
            invoicXmlSerializer.Serialize(stream, obj);
            return stream.ToArray();
        }

        private Orders DeserializeXmlOrders(byte[] data)
        {
            return (Orders)ordersXmlSerializer.Deserialize(new MemoryStream(data));
        }

        private Invoic DeserializeXmlInvoic(byte[] data)
        {
            return (Invoic)invoicXmlSerializer.Deserialize(new MemoryStream(data));
        }

        private byte[] SerializeJsonOrders(Orders obj)
        {
            stream.Position = 0;
            stream.SetLength(0);
            ordersJsonSerializer.WriteObject(stream, obj);
            return stream.ToArray();
        }

        private byte[] SerializeJsonInvoic(Invoic obj)
        {
            stream.Position = 0;
            stream.SetLength(0);
            invoicJsonSerializer.WriteObject(stream, obj);
            return stream.ToArray();
        }

        private Orders DeserializeJsonOrders(byte[] data)
        {
            return (Orders)ordersJsonSerializer.ReadObject(new MemoryStream(data));
        }

        private Invoic DeserializeJsonInvoic(byte[] data)
        {
            return (Invoic)invoicJsonSerializer.ReadObject(new MemoryStream(data));
        }

        private static void DoTest<TData>(int iterations, int stringsLength, int arraysSize, Func<TData, byte[]> serializer, Func<byte[], TData> deserializer) where TData : class, new()
        {
            const int numberOfObjects = 1000;
            var random = new Random(54717651);
            var objects = new TData[numberOfObjects];
            objects[0] = TestHelpers.GenerateRandomTrash<TData>(random, stringsLength, arraysSize);
            for(int i = 1; i < objects.Length; ++i)
                objects[i] = TestHelpers.GenerateRandomTrash<TData>(random, stringsLength, arraysSize);

            deserializer(serializer(objects[0]));

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

        private static void DoTestGetSize<TData>(int iterations, int stringsLength, int arraysSize, Func<TData, int> counter) where TData : class, new()
        {
            const int numberOfObjects = 1000;
            var random = new Random(54717651);
            var objects = new TData[numberOfObjects];
            objects[0] = TestHelpers.GenerateRandomTrash<TData>(random, stringsLength, arraysSize);
            for(int i = 1; i < objects.Length; ++i)
                objects[i] = TestHelpers.GenerateRandomTrash<TData>(random, stringsLength, arraysSize);

            var sizes = new int[numberOfObjects];
            long size = 0;
            DateTime start = DateTime.Now;
            for(int i = 0; i < objects.Length; ++i)
            {
                int cur = 0;
                for(int j = 0; j < iterations; ++j)
                {
                    cur = counter(objects[(i + j) % objects.Length]);
                    size += cur;
                }
                sizes[i] = cur;
            }
            TimeSpan elapsed = DateTime.Now - start;
            Console.WriteLine("Size counting: " + elapsed.TotalMilliseconds * 1000 / numberOfObjects / iterations + " microseconds (" + Math.Round(1000.0 * numberOfObjects * iterations / elapsed.TotalMilliseconds) + " size counts per second)");
            Console.WriteLine("Size: " + ((double)size) / numberOfObjects / iterations + " bytes");
        }

        private static readonly MemoryStream stream = new MemoryStream(128 * 1024);

        private Serializer groBuf;
        private XmlSerializer ordersXmlSerializer;
        private XmlSerializer invoicXmlSerializer;
        private DataContractJsonSerializer ordersJsonSerializer;
        private DataContractJsonSerializer invoicJsonSerializer;
    }
}