using System;
using System.Threading;

using GroBuf.Tests.TestData.Orders;
using GroBuf.Tests.TestTools;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class IntegrationTest
    {
        private Serializer serializer;

        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer();
        }

        [Test]
        public void Test()
        {
            const int numberOfMessages = 10000;
            var random = new Random(54717651);
            var datas = new Orders[numberOfMessages];
            datas[0] = TestHelpers.GenerateRandomTrash<Orders>(random, 75, 10, 2);
            for (int i = 1; i < datas.Length; ++i)
                datas[i] = TestHelpers.GenerateRandomTrash<Orders>(random, 75, 10, 2);

            var messages = new byte[numberOfMessages][];
            for(int i = 0; i < datas.Length; ++i)
                messages[i] = serializer.Serialize(datas[i]);

            var deserializedMessages = new Orders[numberOfMessages];
            for(int i = 0; i < messages.Length; ++i)
                deserializedMessages[i] = serializer.Deserialize<Orders>(messages[i]) ?? new Orders();

            for (int i = 0; i < numberOfMessages; ++i)
            {
                TestHelpers.Extend(deserializedMessages[i]);
                TestHelpers.Extend(datas[i]);
                deserializedMessages[i].AssertEqualsTo(datas[i]);
            }
        }

        private volatile bool stop;

        private void Collect()
        {
            while(!stop)
            {
                Thread.Sleep(100);
                GC.Collect();
            }
        }

        [Test]
        public void TestWithGarbageCollection()
        {
            const int numberOfMessages = 100000;
            var random = new Random(54717651);
            var datas = new Orders[numberOfMessages];
            datas[0] = TestHelpers.GenerateRandomTrash<Orders>(random, 75, 10, 2);
            for (int i = 1; i < datas.Length; ++i)
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

    }
}