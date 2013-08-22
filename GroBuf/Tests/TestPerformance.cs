using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Xml.Serialization;

using GroBuf.Tests.TestData.Invoic;
using GroBuf.Tests.TestData.Orders;
using GroBuf.Tests.TestTools;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture, Ignore]
    public class TestPerformance
    {
        [SetUp]
        public void SetUp()
        {
            groBuf = new SerializerImpl();
            ordersXmlSerializer = new XmlSerializer(typeof(Orders));
            invoicXmlSerializer = new XmlSerializer(typeof(Invoic));
            ordersJsonSerializer = new DataContractJsonSerializer(typeof(Orders));
            invoicJsonSerializer = new DataContractJsonSerializer(typeof(Invoic));
        }

        [Test]
        public void TestGroBuf()
        {
            Console.WriteLine("GroBuf big data: all types");
            DoTestBig(100, data => groBuf.Serialize(data), data => groBuf.Deserialize<Orders>(data));
            Console.WriteLine();
            Console.WriteLine("GroBuf big data: strings");
            DoTestBig(100, data => groBuf.Serialize(data), data => groBuf.Deserialize<Invoic>(data));
            Console.WriteLine();
            Console.WriteLine("GroBuf small data: all types");
            DoTestSmall(1000, data => groBuf.Serialize(data), data => groBuf.Deserialize<Orders>(data));
            Console.WriteLine();
            Console.WriteLine("GroBuf small data: strings");
            DoTestSmall(1000, data => groBuf.Serialize(data), data => groBuf.Deserialize<Invoic>(data));
            Console.WriteLine();
            Console.WriteLine("GroBuf tiny data: all types");
            DoTestTiny(10000, data => groBuf.Serialize(data), data => groBuf.Deserialize<Orders>(data));
            Console.WriteLine();
            Console.WriteLine("GroBuf tiny data: strings");
            DoTestTiny(10000, data => groBuf.Serialize(data), data => groBuf.Deserialize<Invoic>(data));
        }

        [Test]
        public void TestProtoBuf()
        {
            Console.WriteLine("ProtoBuf big data: all types");
            DoTestBig(3, SerializeProtoBuf<Orders>, DeserializeProtoBuf<Orders>);
            Console.WriteLine();
            Console.WriteLine("ProtoBuf big data: strings:");
            DoTestBig(3, SerializeProtoBuf<Invoic>, DeserializeProtoBuf<Invoic>);
            Console.WriteLine();
            Console.WriteLine("ProtoBuf small data: all types");
            DoTestSmall(300, SerializeProtoBuf<Orders>, DeserializeProtoBuf<Orders>);
            Console.WriteLine();
            Console.WriteLine("ProtoBuf small data: strings");
            DoTestSmall(300, SerializeProtoBuf<Invoic>, DeserializeProtoBuf<Invoic>);
            Console.WriteLine();
            Console.WriteLine("ProtoBuf tiny data: all types");
            DoTestTiny(3000, SerializeProtoBuf<Orders>, DeserializeProtoBuf<Orders>);
            Console.WriteLine();
            Console.WriteLine("ProtoBuf tiny data: strings");
            DoTestTiny(3000, SerializeProtoBuf<Invoic>, DeserializeProtoBuf<Invoic>);
        }

        [Test]
        public void TestXmlSerializer()
        {
            Console.WriteLine("XmlSerializer big data: all types");
            DoTestBig(1, SerializeXmlOrders, DeserializeXmlOrders);
            Console.WriteLine();
            Console.WriteLine("XmlSerializer big data: strings");
            DoTestBig(1, SerializeXmlInvoic, DeserializeXmlInvoic);
            Console.WriteLine();
            Console.WriteLine("XmlSerializer small data: all types");
            DoTestSmall(10, SerializeXmlOrders, DeserializeXmlOrders);
            Console.WriteLine();
            Console.WriteLine("XmlSerializer small data: strings");
            DoTestSmall(10, SerializeXmlInvoic, DeserializeXmlInvoic);
            Console.WriteLine();
            Console.WriteLine("XmlSerializer tiny data: all types");
            DoTestTiny(100, SerializeXmlOrders, DeserializeXmlOrders);
            Console.WriteLine();
            Console.WriteLine("XmlSerializer tiny data: strings");
            DoTestTiny(100, SerializeXmlInvoic, DeserializeXmlInvoic);
        }

        [Test]
        public void TestJsonSerializer()
        {
            Console.WriteLine("JsonSerializer big data: all types");
            DoTestBig(1, SerializeJsonOrders, DeserializeJsonOrders);
            Console.WriteLine();
            Console.WriteLine("JsonSerializer big data: strings");
            DoTestBig(1, SerializeJsonInvoic, DeserializeJsonInvoic);
            Console.WriteLine();
            Console.WriteLine("JsonSerializer small data: all types");
            DoTestSmall(10, SerializeJsonOrders, DeserializeJsonOrders);
            Console.WriteLine();
            Console.WriteLine("JsonSerializer small data: strings");
            DoTestSmall(10, SerializeJsonInvoic, DeserializeJsonInvoic);
            Console.WriteLine();
            Console.WriteLine("JsonSerializer tiny data: all types");
            DoTestTiny(100, SerializeJsonOrders, DeserializeJsonOrders);
            Console.WriteLine();
            Console.WriteLine("JsonSerializer tiny data: strings");
            DoTestTiny(100, SerializeJsonInvoic, DeserializeJsonInvoic);
        }

        [Test]
        public void TestGroBufGetSize()
        {
            Console.WriteLine("GroBuf.GetSize big data: all types");
            DoTestGetSizeBig<Orders>(100, data => groBuf.GetSize(data));
            Console.WriteLine();
            Console.WriteLine("GroBuf.GetSize big data: strings");
            DoTestGetSizeBig<Invoic>(100, data => groBuf.GetSize(data));
            Console.WriteLine();
            Console.WriteLine("GroBuf.GetSize small data: all types");
            DoTestGetSizeSmall<Orders>(1000, data => groBuf.GetSize(data));
            Console.WriteLine();
            Console.WriteLine("GroBuf.GetSize small data: strings");
            DoTestGetSizeSmall<Invoic>(1000, data => groBuf.GetSize(data));
            Console.WriteLine();
            Console.WriteLine("GroBuf.GetSize tiny data: all types");
            DoTestGetSizeTiny<Orders>(10000, data => groBuf.GetSize(data));
            Console.WriteLine();
            Console.WriteLine("GroBuf.GetSize tiny data: strings");
            DoTestGetSizeTiny<Invoic>(10000, data => groBuf.GetSize(data));
        }

        [Test]
        public void TestGroBufChangeType()
        {
            Console.WriteLine("GroBuf.ChangeType big data: all types");
            DoTestChangeTypeBig<Orders, Orders>(10, obj => groBuf.ChangeType<Orders, Orders>(obj));
            Console.WriteLine();
            Console.WriteLine("GroBuf.ChangeType big data: strings");
            DoTestChangeTypeBig<Invoic, Invoic>(10, obj => groBuf.ChangeType<Invoic, Invoic>(obj));
            Console.WriteLine();
            Console.WriteLine("GroBuf.ChangeType small data: all types");
            DoTestChangeTypeSmall<Orders, Orders>(100, obj => groBuf.ChangeType<Orders, Orders>(obj));
            Console.WriteLine();
            Console.WriteLine("GroBuf.ChangeType small data: strings");
            DoTestChangeTypeSmall<Invoic, Invoic>(100, obj => groBuf.ChangeType<Invoic, Invoic>(obj));
            Console.WriteLine();
            Console.WriteLine("GroBuf.ChangeType tiny data: all types");
            DoTestChangeTypeTiny<Orders, Orders>(1000, obj => groBuf.ChangeType<Orders, Orders>(obj));
            Console.WriteLine();
            Console.WriteLine("GroBuf.ChangeType tiny data: strings");
            DoTestChangeTypeTiny<Invoic, Invoic>(1000, obj => groBuf.ChangeType<Invoic, Invoic>(obj));
        }

        [Test]
        public void TestProtoBufChangeType()
        {
            Console.WriteLine("ProtoBuf.ChangeType big data: all types");
            DoTestChangeTypeBig<Orders, Orders>(3, ProtoBuf.Serializer.ChangeType<Orders, Orders>);
            Console.WriteLine();
            Console.WriteLine("ProtoBuf.ChangeType big data: strings");
            DoTestChangeTypeBig<Invoic, Invoic>(3, ProtoBuf.Serializer.ChangeType<Invoic, Invoic>);
            Console.WriteLine();
            Console.WriteLine("ProtoBuf.ChangeType small data: all types");
            DoTestChangeTypeSmall<Orders, Orders>(30, ProtoBuf.Serializer.ChangeType<Orders, Orders>);
            Console.WriteLine();
            Console.WriteLine("ProtoBuf.ChangeType small data: strings");
            DoTestChangeTypeSmall<Invoic, Invoic>(30, ProtoBuf.Serializer.ChangeType<Invoic, Invoic>);
            Console.WriteLine();
            Console.WriteLine("ProtoBuf.ChangeType tiny data: all types");
            DoTestChangeTypeTiny<Orders, Orders>(300, ProtoBuf.Serializer.ChangeType<Orders, Orders>);
            Console.WriteLine();
            Console.WriteLine("ProtoBuf.ChangeType tiny data: strings");
            DoTestChangeTypeTiny<Invoic, Invoic>(300, ProtoBuf.Serializer.ChangeType<Invoic, Invoic>);
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

        private static void DoTestBig<TData>(int iterations, Func<TData, byte[]> serializer, Func<byte[], TData> deserializer) where TData : class, new()
        {
            DoTest(iterations, 80, 100, 10, serializer, deserializer);
        }

        private static void DoTestSmall<TData>(int iterations, Func<TData, byte[]> serializer, Func<byte[], TData> deserializer) where TData : class, new()
        {
            DoTest(iterations, 60, 10, 5, serializer, deserializer);
        }

        private static void DoTestTiny<TData>(int iterations, Func<TData, byte[]> serializer, Func<byte[], TData> deserializer) where TData : class, new()
        {
            DoTest(iterations, 30, 5, 2, serializer, deserializer);
        }

        private static void DoTest<TData>(int iterations, int fillRate, int stringsLength, int arraysSize, Func<TData, byte[]> serializer, Func<byte[], TData> deserializer) where TData : class, new()
        {
            const int numberOfObjects = 1000;
            var random = new Random(54717651);
            var objects = new TData[numberOfObjects];
            objects[0] = TestHelpers.GenerateRandomTrash<TData>(random, fillRate, stringsLength, arraysSize);
            for(int i = 1; i < objects.Length; ++i)
                objects[i] = TestHelpers.GenerateRandomTrash<TData>(random, fillRate, stringsLength, arraysSize);

            deserializer(serializer(objects[0]));

            var datas = new byte[numberOfObjects][];
            long size = 0;
            var stopwatch = Stopwatch.StartNew();
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
            stopwatch.Stop();
            var elapsed = stopwatch.Elapsed;
            Console.WriteLine("Serializing: " + elapsed.TotalMilliseconds * 1000 / numberOfObjects / iterations + " microseconds (" + Math.Round(1000.0 * numberOfObjects * iterations / elapsed.TotalMilliseconds) + " serializations per second)");
            Console.WriteLine("Size: " + ((double)size) / numberOfObjects / iterations + " bytes");

            var deserializedDatas = new TData[numberOfObjects];
            stopwatch = Stopwatch.StartNew();
            for(int i = 0; i < datas.Length; ++i)
            {
                TData cur = null;
                for(int j = 0; j < iterations; ++j)
                    cur = deserializer(datas[(i + j) % datas.Length]);
                deserializedDatas[i] = cur;
            }
            stopwatch.Stop();
            elapsed = stopwatch.Elapsed;
            Console.WriteLine("Deserializing: " + elapsed.TotalMilliseconds * 1000 / numberOfObjects / iterations + " microseconds (" + Math.Round(1000.0 * numberOfObjects * iterations / elapsed.TotalMilliseconds) + " deserializations per second)");
        }

        private static void DoTestGetSizeBig<TData>(int iterations, Func<TData, int> counter) where TData : class, new()
        {
            DoTestGetSize(iterations, 80, 1000, 8, counter);
        }

        private static void DoTestGetSizeSmall<TData>(int iterations, Func<TData, int> counter) where TData : class, new()
        {
            DoTestGetSize(iterations, 60, 10, 5, counter);
        }

        private static void DoTestGetSizeTiny<TData>(int iterations, Func<TData, int> counter) where TData : class, new()
        {
            DoTestGetSize(iterations, 30, 5, 2, counter);
        }

        private static void DoTestGetSize<TData>(int iterations, int fillRate, int stringsLength, int arraysSize, Func<TData, int> counter) where TData : class, new()
        {
            const int numberOfObjects = 1000;
            var random = new Random(54717651);
            var objects = new TData[numberOfObjects];
            objects[0] = TestHelpers.GenerateRandomTrash<TData>(random, fillRate, stringsLength, arraysSize);
            for(int i = 1; i < objects.Length; ++i)
                objects[i] = TestHelpers.GenerateRandomTrash<TData>(random, fillRate, stringsLength, arraysSize);

            counter(objects[0]);

            var sizes = new int[numberOfObjects];
            long size = 0;
            var stopwatch = Stopwatch.StartNew();
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
            stopwatch.Stop();
            TimeSpan elapsed = stopwatch.Elapsed;
            Console.WriteLine("Size counting: " + elapsed.TotalMilliseconds * 1000 / numberOfObjects / iterations + " microseconds (" + Math.Round(1000.0 * numberOfObjects * iterations / elapsed.TotalMilliseconds) + " size counts per second)");
            Console.WriteLine("Size: " + ((double)size) / numberOfObjects / iterations + " bytes");
        }

        private static void DoTestChangeTypeBig<TFrom, TTo>(int iterations, Func<TFrom, TTo> typeChanger) where TFrom : class, new()
        {
            DoTestChangeType(iterations, 80, 1000, 8, typeChanger);
        }

        private static void DoTestChangeTypeSmall<TFrom, TTo>(int iterations, Func<TFrom, TTo> typeChanger) where TFrom : class, new()
        {
            DoTestChangeType(iterations, 60, 10, 5, typeChanger);
        }

        private static void DoTestChangeTypeTiny<TFrom, TTo>(int iterations, Func<TFrom, TTo> typeChanger) where TFrom : class, new()
        {
            DoTestChangeType(iterations, 30, 5, 2, typeChanger);
        }

        private static void DoTestChangeType<TFrom, TTo>(int iterations, int fillRate, int stringsLength, int arraysSize, Func<TFrom, TTo> typeChanger) where TFrom : class, new()
        {
            const int numberOfObjects = 1000;
            var random = new Random(54717651);
            var from = new TFrom[numberOfObjects];
            from[0] = TestHelpers.GenerateRandomTrash<TFrom>(random, fillRate, stringsLength, arraysSize);
            for(int i = 1; i < from.Length; ++i)
                from[i] = TestHelpers.GenerateRandomTrash<TFrom>(random, fillRate, stringsLength, arraysSize);

            typeChanger(from[0]);

            var to = new TTo[numberOfObjects];
            var stopwatch = Stopwatch.StartNew();
            for(int i = 0; i < from.Length; ++i)
            {
                TTo cur = default(TTo);
                for(int j = 0; j < iterations; ++j)
                    cur = typeChanger(from[(i + j) % from.Length]);
                to[i] = cur;
            }
            stopwatch.Stop();
            TimeSpan elapsed = stopwatch.Elapsed;
            Console.WriteLine("Type changing: " + elapsed.TotalMilliseconds * 1000 / numberOfObjects / iterations + " microseconds (" + Math.Round(1000.0 * numberOfObjects * iterations / elapsed.TotalMilliseconds) + " type changes per second)");
        }

        private static readonly MemoryStream stream = new MemoryStream(128 * 1024);

        private Serializer groBuf;
        private XmlSerializer ordersXmlSerializer;
        private XmlSerializer invoicXmlSerializer;
        private DataContractJsonSerializer ordersJsonSerializer;
        private DataContractJsonSerializer invoicJsonSerializer;
    }
}