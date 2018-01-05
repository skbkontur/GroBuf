using System;
using System.Threading;

using GroBuf.DataMembersExtracters;
using GroBuf.Tests.TestData.Desadv;
using GroBuf.Tests.TestData.Orders;
using GroBuf.Tests.TestTools;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class IntegrationTest
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor());
        }

        [Test]
        [Category("LongRunning")]
        [Ignore("Is used for debugging")]
        public void TestChangeObjectDuringSerialization()
        {
            var random = new Random(54717651);
            var data = TestHelpers.GenerateRandomTrash<Orders>(random, 75, 10, 2);
            var thread = new Thread(FillWithRandomTrash);
            thread.Start(data);
            while(true)
            {
                try
                {
                    var serializedData = serializer.Serialize(data);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public void FillWithRandomTrash(object param)
        {
            var data = (Orders)param;
            var random = new Random(1231241);
            while(true)
                TestHelpers.FillWithRandomTrash(data, random, 75, 10, 2);
        }

        [Test]
        [Category("LongRunning")]
        public void Test()
        {
            const int numberOfMessages = 10000;
            var random = new Random(54717651);
            var datas = new Orders[numberOfMessages];
            datas[0] = TestHelpers.GenerateRandomTrash<Orders>(random, 75, 10, 2);
            for(int i = 1; i < datas.Length; ++i)
                datas[i] = TestHelpers.GenerateRandomTrash<Orders>(random, 75, 10, 2);

            var messages = new byte[numberOfMessages][];
            for(int i = 0; i < datas.Length; ++i)
                messages[i] = serializer.Serialize(datas[i]);

            var deserializedMessages = new Orders[numberOfMessages];
            for(int i = 0; i < messages.Length; ++i)
                deserializedMessages[i] = serializer.Deserialize<Orders>(messages[i]) ?? new Orders();

            for(int i = 0; i < numberOfMessages; ++i)
            {
                TestHelpers.Extend(deserializedMessages[i]);
                TestHelpers.Extend(datas[i]);
                deserializedMessages[i].AssertEqualsTo(datas[i]);
            }
        }

        [Test]
        [Category("LongRunning")]
        public void TestWithGarbageCollection()
        {
            const int numberOfMessages = 100000;
            var random = new Random(54717651);
            var datas = new Orders[numberOfMessages];
            datas[0] = TestHelpers.GenerateRandomTrash<Orders>(random, 75, 10, 2);
            for(int i = 1; i < datas.Length; ++i)
                datas[i] = TestHelpers.GenerateRandomTrash<Orders>(random, 75, 10, 2);

            stop = false;
            var thread = new Thread(Collect);
            thread.Start();

            var messages = new byte[numberOfMessages][];
            for(int i = 0; i < datas.Length; ++i)
                messages[i] = serializer.Serialize(datas[i]);

            var deserializedMessages = new Orders[numberOfMessages];
            for(int i = 0; i < messages.Length; ++i)
                deserializedMessages[i] = serializer.Deserialize<Orders>(messages[i]);

            stop = true;
        }

        [Test]
        [Category("LongRunning")]
        [Ignore("Is used for debugging")]
        public void TestWithGarbageCollection2()
        {
            const int numberOfMessages = 1000000;

            stop = false;
            var thread = new Thread(Collect);
            thread.Start();

            for (int i = 0; i < 10; ++i )
                new Thread(Zzz).Start(serializer);
            Zzz(serializer);

            stop = true;
        }

        private void Zzz(object param)
        {
            var serializer = (Serializer)param;
            var random = new Random(Guid.NewGuid().GetHashCode());
            for (int i = 0; i < 1000000; ++i)
            {
                var data = TestHelpers.GenerateRandomTrash<Desadv>(random, 75, 10, 2);
                var message = serializer.Serialize(data);
                var deserializedData = serializer.Deserialize<Desadv>(message);
            }
        }

        private void Collect()
        {
            while(!stop)
            {
                Thread.Sleep(100);
                GC.Collect();
            }
        }

        private Serializer serializer;
        private volatile bool stop;
    }
}