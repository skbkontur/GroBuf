using System;

using NUnit.Framework;

namespace SKBKontur.GroBuf.Tests
{
    [TestFixture]
    public class GroBufReaderTest
    {
        [SetUp]
        public void SetUp()
        {
            reader = new GroBufReader();
        }

        [Test]
        public void TestReadTrash()
        {
            var random = new Random(12345678);
            for(int i = 0; i < 10000; ++i)
            {
                var length = random.Next(1, 100);
                var data = new byte[length];
                random.NextBytes(data);
                TryRead<A>(data);
                TryRead<sbyte>(data);
                TryRead<byte>(data);
                TryRead<short>(data);
                TryRead<ushort>(data);
                TryRead<int>(data);
                TryRead<uint>(data);
                TryRead<long>(data);
                TryRead<ulong>(data);
                TryRead<Guid>(data);
                TryRead<A[]>(data);
                TryRead<sbyte[]>(data);
                TryRead<byte[]>(data);
                TryRead<short[]>(data);
                TryRead<ushort[]>(data);
                TryRead<int[]>(data);
                TryRead<uint[]>(data);
                TryRead<long[]>(data);
                TryRead<ulong[]>(data);
                TryRead<Guid[]>(data);
            }
        }

        private void TryRead<T>(byte[] data)
        {
            try
            {
                reader.Read<T>(data);
            }
            catch(DataCorruptedException)
            {
            }
        }

        private GroBufReader reader;

        private class A
        {
            public int Int { get; set; }
            public bool Bool { get; set; }
            public string Str { get; set; }
            public double Double { get; set; }
            public int[] Arr { get; set; }
        }
    }
}