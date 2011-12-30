using System;
using System.Threading;

using NUnit.Framework;

using SKBKontur.GroBuf.Tests.TestData;
using SKBKontur.GroBuf.Tests.TestTools;

namespace SKBKontur.GroBuf.Tests
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
            var datas = new SG43[numberOfMessages];
            datas[0] = TestHelpers.GenerateRandomTrash<SG43>(random);
            for (int i = 1; i < datas.Length; ++i)
                datas[i] = TestHelpers.GenerateRandomTrash<SG43>(random);

            var messages = new byte[numberOfMessages][];
            for(int i = 0; i < datas.Length; ++i)
                messages[i] = serializer.Serialize(datas[i]);

            var deserializedMessages = new SG43[numberOfMessages];
            for(int i = 0; i < messages.Length; ++i)
                deserializedMessages[i] = serializer.Deserialize<SG43>(messages[i]) ?? new SG43();

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
            var datas = new SG43[numberOfMessages];
            datas[0] = TestHelpers.GenerateRandomTrash<SG43>(random);
            for (int i = 1; i < datas.Length; ++i)
                datas[i] = TestHelpers.GenerateRandomTrash<SG43>(random);

            stop = false;
            var thread = new Thread(Collect);
            thread.Start();

            var messages = new byte[numberOfMessages][];
            for(int i = 0; i < datas.Length; ++i)
                messages[i] = serializer.Serialize(datas[i]);

            var deserializedMessages = new SG43[numberOfMessages];
            for(int i = 0; i < messages.Length; ++i)
                deserializedMessages[i] = serializer.Deserialize<SG43>(messages[i]);

            stop = true;
        }

        [Test, Ignore]
        public void TestPerformance()
        {
            const int numberOfMessages = 100000;
            var random = new Random(54717651);
            var datas = new SG43[numberOfMessages];
            datas[0] = TestHelpers.GenerateRandomTrash<SG43>(random);
            for (int i = 1; i < datas.Length; ++i)
                datas[i] = TestHelpers.GenerateRandomTrash<SG43>(random);

            var messages = new byte[numberOfMessages][];
            DateTime start = DateTime.Now;
            for (int i = 0; i < datas.Length; ++i)
            {
                byte[] cur = null;
                for (int j = 0; j < 1000; ++j)
                    cur = serializer.Serialize(datas[(i + j) % datas.Length]);
                messages[i] = cur;
            }
            TimeSpan elapsed = DateTime.Now - start;
            Console.WriteLine("Serializing: " + elapsed.TotalMilliseconds + "ms");

            var deserializedMessages = new SG43[numberOfMessages];
            start = DateTime.Now;
            for (int i = 0; i < messages.Length; ++i)
            {
                SG43 cur = null;
                for (int j = 0; j < 1000; ++j)
                    cur = serializer.Deserialize<SG43>(messages[(i + j) % messages.Length]);
                deserializedMessages[i] = cur;
            }
            elapsed = DateTime.Now - start;
            Console.WriteLine("Deserializing: " + elapsed.TotalMilliseconds + "ms");
        }
    }
}