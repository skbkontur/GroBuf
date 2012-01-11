using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestPrimitives
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer();
        }

        [Test]
        public void TestBool()
        {
            Assert.AreEqual(1, SerializeDeserialize<bool, int>(true));
            Assert.AreEqual(1, SerializeDeserialize<bool, uint>(true));
            Assert.AreEqual(1, SerializeDeserialize<bool, sbyte>(true));
            Assert.AreEqual(1, SerializeDeserialize<bool, byte>(true));
            Assert.AreEqual(1, SerializeDeserialize<bool, short>(true));
            Assert.AreEqual(1, SerializeDeserialize<bool, ushort>(true));
            Assert.AreEqual(1, SerializeDeserialize<bool, long>(true));
            Assert.AreEqual(1, SerializeDeserialize<bool, ulong>(true));
            Assert.AreEqual((float)1, SerializeDeserialize<bool, float>(true));
            Assert.AreEqual((double)1, SerializeDeserialize<bool, double>(true));
            Assert.IsTrue(SerializeDeserialize<int, bool>(int.MaxValue));
            Assert.IsTrue(SerializeDeserialize<long, bool>(1L << 32));
            Assert.IsTrue(SerializeDeserialize<long, bool>(1L << 48));
            Assert.IsFalse(SerializeDeserialize<long, bool>(0));
            Assert.IsFalse(SerializeDeserialize<int, bool>(0));
            Assert.IsFalse(SerializeDeserialize<short, bool>(0));
            Assert.IsFalse(SerializeDeserialize<float, bool>(0));
            Assert.IsFalse(SerializeDeserialize<double, bool>(0));
            Assert.IsTrue(SerializeDeserialize<float, bool>(0.1f));
            Assert.IsTrue(SerializeDeserialize<double, bool>(0.1));

            Assert.AreEqual(0, SerializeDeserialize<bool, int>(false));
            Assert.AreEqual(0, SerializeDeserialize<bool, uint>(false));
            Assert.AreEqual(0, SerializeDeserialize<bool, sbyte>(false));
            Assert.AreEqual(0, SerializeDeserialize<bool, byte>(false));
            Assert.AreEqual(0, SerializeDeserialize<bool, short>(false));
            Assert.AreEqual(0, SerializeDeserialize<bool, ushort>(false));
            Assert.AreEqual(0, SerializeDeserialize<bool, long>(false));
            Assert.AreEqual(0, SerializeDeserialize<bool, ulong>(false));
            Assert.AreEqual((float)0, SerializeDeserialize<bool, float>(false));
            Assert.AreEqual((double)0, SerializeDeserialize<bool, double>(false));
            Assert.IsFalse(SerializeDeserialize<bool, bool>(false));
        }

        [Test]
        public void TestInt8Negative()
        {
            const sbyte x = -13;
            const byte y = -x - 1;
            Assert.AreEqual(x, SerializeDeserialize<sbyte, int>(x));
            Assert.AreEqual(uint.MaxValue - y, SerializeDeserialize<sbyte, uint>(x));
            Assert.AreEqual(x, SerializeDeserialize<sbyte, sbyte>(x));
            Assert.AreEqual(byte.MaxValue - y, SerializeDeserialize<sbyte, byte>(x));
            Assert.AreEqual(x, SerializeDeserialize<sbyte, short>(x));
            Assert.AreEqual(ushort.MaxValue - y, SerializeDeserialize<sbyte, ushort>(x));
            Assert.AreEqual(x, SerializeDeserialize<sbyte, long>(x));
            Assert.AreEqual(ulong.MaxValue - y, SerializeDeserialize<sbyte, ulong>(x));
            Assert.AreEqual((float)x, SerializeDeserialize<sbyte, float>(x));
            Assert.AreEqual((double)x, SerializeDeserialize<sbyte, double>(x));
            Assert.IsTrue(SerializeDeserialize<sbyte, bool>(x));
        }

        [Test]
        public void TestInt8Positive()
        {
            const sbyte x = 13;
            Assert.AreEqual(x, SerializeDeserialize<sbyte, int>(x));
            Assert.AreEqual(x, SerializeDeserialize<sbyte, uint>(x));
            Assert.AreEqual(x, SerializeDeserialize<sbyte, sbyte>(x));
            Assert.AreEqual(x, SerializeDeserialize<sbyte, byte>(x));
            Assert.AreEqual(x, SerializeDeserialize<sbyte, short>(x));
            Assert.AreEqual(x, SerializeDeserialize<sbyte, ushort>(x));
            Assert.AreEqual(x, SerializeDeserialize<sbyte, long>(x));
            Assert.AreEqual(x, SerializeDeserialize<sbyte, ulong>(x));
            Assert.AreEqual((float)x, SerializeDeserialize<sbyte, float>(x));
            Assert.AreEqual((double)x, SerializeDeserialize<sbyte, double>(x));
            Assert.IsTrue(SerializeDeserialize<sbyte, bool>(x));
        }

        [Test]
        public void TestInt16Negative()
        {
            const short x = -13;
            const ushort y = -x - 1;
            Assert.AreEqual(x, SerializeDeserialize<short, int>(x));
            Assert.AreEqual(uint.MaxValue - y, SerializeDeserialize<short, uint>(x));
            Assert.AreEqual(x, SerializeDeserialize<short, sbyte>(x));
            Assert.AreEqual(byte.MaxValue - y, SerializeDeserialize<short, byte>(x));
            Assert.AreEqual(x, SerializeDeserialize<short, short>(x));
            Assert.AreEqual(ushort.MaxValue - y, SerializeDeserialize<short, ushort>(x));
            Assert.AreEqual(x, SerializeDeserialize<short, long>(x));
            Assert.AreEqual(ulong.MaxValue - y, SerializeDeserialize<short, ulong>(x));
            Assert.AreEqual((float)x, SerializeDeserialize<short, float>(x));
            Assert.AreEqual((double)x, SerializeDeserialize<short, double>(x));
            Assert.IsTrue(SerializeDeserialize<short, bool>(x));
        }

        [Test]
        public void TestInt16Positive()
        {
            const short x = 13;
            Assert.AreEqual(x, SerializeDeserialize<short, int>(x));
            Assert.AreEqual(x, SerializeDeserialize<short, uint>(x));
            Assert.AreEqual(x, SerializeDeserialize<short, sbyte>(x));
            Assert.AreEqual(x, SerializeDeserialize<short, byte>(x));
            Assert.AreEqual(x, SerializeDeserialize<short, short>(x));
            Assert.AreEqual(x, SerializeDeserialize<short, ushort>(x));
            Assert.AreEqual(x, SerializeDeserialize<short, long>(x));
            Assert.AreEqual(x, SerializeDeserialize<short, ulong>(x));
            Assert.AreEqual((float)x, SerializeDeserialize<short, float>(x));
            Assert.AreEqual((double)x, SerializeDeserialize<short, double>(x));
            Assert.IsTrue(SerializeDeserialize<short, bool>(x));
        }

        [Test]
        public void TestInt32Negative()
        {
            const int x = -13;
            const uint y = -x - 1;
            Assert.AreEqual(x, SerializeDeserialize<int, int>(x));
            Assert.AreEqual(uint.MaxValue - y, SerializeDeserialize<int, uint>(x));
            Assert.AreEqual(x, SerializeDeserialize<int, sbyte>(x));
            Assert.AreEqual(byte.MaxValue - y, SerializeDeserialize<int, byte>(x));
            Assert.AreEqual(x, SerializeDeserialize<int, short>(x));
            Assert.AreEqual(ushort.MaxValue - y, SerializeDeserialize<int, ushort>(x));
            Assert.AreEqual(x, SerializeDeserialize<int, long>(x));
            Assert.AreEqual(ulong.MaxValue - y, SerializeDeserialize<int, ulong>(x));
            Assert.AreEqual((float)x, SerializeDeserialize<int, float>(x));
            Assert.AreEqual((double)x, SerializeDeserialize<int, double>(x));
            Assert.IsTrue(SerializeDeserialize<int, bool>(x));
        }

        [Test]
        public void TestInt32Positive()
        {
            const int x = 13;
            Assert.AreEqual(x, SerializeDeserialize<int, int>(x));
            Assert.AreEqual(x, SerializeDeserialize<int, uint>(x));
            Assert.AreEqual(x, SerializeDeserialize<int, sbyte>(x));
            Assert.AreEqual(x, SerializeDeserialize<int, byte>(x));
            Assert.AreEqual(x, SerializeDeserialize<int, short>(x));
            Assert.AreEqual(x, SerializeDeserialize<int, ushort>(x));
            Assert.AreEqual(x, SerializeDeserialize<int, long>(x));
            Assert.AreEqual(x, SerializeDeserialize<int, ulong>(x));
            Assert.AreEqual((float)x, SerializeDeserialize<int, float>(x));
            Assert.AreEqual((double)x, SerializeDeserialize<int, double>(x));
            Assert.IsTrue(SerializeDeserialize<int, bool>(x));
        }

        [Test]
        public void TestUInt8()
        {
            const byte x = 250;
            Assert.AreEqual(x, SerializeDeserialize<byte, int>(x));
            Assert.AreEqual(x, SerializeDeserialize<byte, uint>(x));
            Assert.AreEqual(-(byte.MaxValue - x + 1), SerializeDeserialize<byte, sbyte>(x));
            Assert.AreEqual(x, SerializeDeserialize<byte, byte>(x));
            Assert.AreEqual(x, SerializeDeserialize<byte, short>(x));
            Assert.AreEqual(x, SerializeDeserialize<byte, ushort>(x));
            Assert.AreEqual(x, SerializeDeserialize<byte, long>(x));
            Assert.AreEqual(x, SerializeDeserialize<byte, ulong>(x));
            Assert.AreEqual((float)x, SerializeDeserialize<byte, float>(x));
            Assert.AreEqual((double)x, SerializeDeserialize<byte, double>(x));
            Assert.IsTrue(SerializeDeserialize<byte, bool>(x));
        }

        [Test]
        public void TestUInt16()
        {
            const ushort x = 54321;
            Assert.AreEqual(x, SerializeDeserialize<ushort, int>(x));
            Assert.AreEqual(x, SerializeDeserialize<ushort, uint>(x));
            Assert.AreEqual(13, SerializeDeserialize<ushort, sbyte>(13));
            Assert.AreEqual(13, SerializeDeserialize<ushort, byte>(13));
            Assert.AreEqual(-(ushort.MaxValue - x + 1), SerializeDeserialize<ushort, short>(x));
            Assert.AreEqual(x, SerializeDeserialize<ushort, ushort>(x));
            Assert.AreEqual(x, SerializeDeserialize<ushort, long>(x));
            Assert.AreEqual(x, SerializeDeserialize<ushort, ulong>(x));
            Assert.AreEqual((float)x, SerializeDeserialize<ushort, float>(x));
            Assert.AreEqual((double)x, SerializeDeserialize<ushort, double>(x));
            Assert.IsTrue(SerializeDeserialize<ushort, bool>(x));
        }

        [Test]
        public void TestUInt32()
        {
            const uint x = 3000000000;
            Assert.AreEqual(-(uint.MaxValue - x + 1), SerializeDeserialize<uint, int>(x));
            Assert.AreEqual(x, SerializeDeserialize<uint, uint>(x));
            Assert.AreEqual(13, SerializeDeserialize<uint, sbyte>(13));
            Assert.AreEqual(13, SerializeDeserialize<uint, byte>(13));
            Assert.AreEqual(1000, SerializeDeserialize<uint, short>(1000));
            Assert.AreEqual(1000, SerializeDeserialize<uint, ushort>(1000));
            Assert.AreEqual(x, SerializeDeserialize<uint, long>(x));
            Assert.AreEqual(x, SerializeDeserialize<uint, ulong>(x));
            Assert.AreEqual((float)x, SerializeDeserialize<uint, float>(x));
            Assert.AreEqual((double)x, SerializeDeserialize<uint, double>(x));
            Assert.IsTrue(SerializeDeserialize<uint, bool>(x));
        }

        [Test]
        public void TestUInt64()
        {
            unchecked
            {
                const ulong x = 10000000000000000000;
                Assert.AreEqual(13, SerializeDeserialize<ulong, sbyte>(13));
                Assert.AreEqual(13, SerializeDeserialize<ulong, byte>(13));
                Assert.AreEqual(1000, SerializeDeserialize<ulong, short>(1000));
                Assert.AreEqual(1000, SerializeDeserialize<ulong, ushort>(1000));
                Assert.AreEqual(1000000000, SerializeDeserialize<ulong, int>(1000000000));
                Assert.AreEqual(1000000000, SerializeDeserialize<ulong, uint>(1000000000));
                Assert.AreEqual((long)(0L - (ulong.MaxValue - x + 1)), SerializeDeserialize<ulong, long>(x));
                Assert.AreEqual(x, SerializeDeserialize<ulong, ulong>(x));
                Assert.AreEqual((float)x, SerializeDeserialize<ulong, float>(x));
                Assert.AreEqual((double)x, SerializeDeserialize<ulong, double>(x));
                Assert.IsTrue(SerializeDeserialize<ulong, bool>(x));
            }
        }

        [Test]
        public void TestInt64Positive()
        {
            unchecked
            {
                const long x = 1000000000000000000;
                Assert.AreEqual(13, SerializeDeserialize<long, sbyte>(13));
                Assert.AreEqual(13, SerializeDeserialize<long, byte>(13));
                Assert.AreEqual(1000, SerializeDeserialize<long, short>(1000));
                Assert.AreEqual(1000, SerializeDeserialize<long, ushort>(1000));
                Assert.AreEqual(1000000000, SerializeDeserialize<long, int>(1000000000));
                Assert.AreEqual(1000000000, SerializeDeserialize<long, uint>(1000000000));
                Assert.AreEqual(x, SerializeDeserialize<long, long>(x));
                Assert.AreEqual(x, SerializeDeserialize<long, ulong>(x));
                Assert.AreEqual((float)x, SerializeDeserialize<long, float>(x));
                Assert.AreEqual((double)x, SerializeDeserialize<long, double>(x));
                Assert.IsTrue(SerializeDeserialize<long, bool>(x));
            }
        }

        [Test]
        public void TestInt64Negative()
        {
            unchecked
            {
                const long x = -13;
                const ulong y = -x - 1;
                Assert.AreEqual(x, SerializeDeserialize<long, int>(x));
                Assert.AreEqual(uint.MaxValue - y, SerializeDeserialize<long, uint>(x));
                Assert.AreEqual(x, SerializeDeserialize<long, sbyte>(x));
                Assert.AreEqual(byte.MaxValue - y, SerializeDeserialize<long, byte>(x));
                Assert.AreEqual(x, SerializeDeserialize<long, short>(x));
                Assert.AreEqual(ushort.MaxValue - y, SerializeDeserialize<long, ushort>(x));
                Assert.AreEqual(x, SerializeDeserialize<long, long>(x));
                Assert.AreEqual(ulong.MaxValue - y, SerializeDeserialize<long, ulong>(x));
                Assert.AreEqual((float)x, SerializeDeserialize<long, float>(x));
                Assert.AreEqual((double)x, SerializeDeserialize<long, double>(x));
                Assert.IsTrue(SerializeDeserialize<long, bool>(x));
            }
        }

        [Test]
        public void TestFloat()
        {
            unchecked
            {
                Assert.AreEqual(13, SerializeDeserialize<float, sbyte>(13.123f));
                Assert.AreEqual(13, SerializeDeserialize<float, byte>(13.123f));
                Assert.AreEqual(1000, SerializeDeserialize<float, short>(1000.382456f));
                Assert.AreEqual(1000, SerializeDeserialize<float, ushort>(1000.382456f));
                Assert.AreEqual(1000000000, SerializeDeserialize<float, int>(1000000000.73465f));
                Assert.AreEqual(1000000000, SerializeDeserialize<float, uint>(1000000000.73465f));
                Assert.AreEqual(10000000000, SerializeDeserialize<float, long>(10000000000.73465f));
                Assert.AreEqual(10000000000, SerializeDeserialize<float, ulong>(10000000000.73465f));
                Assert.AreEqual(3.1415926f, SerializeDeserialize<float, float>(3.1415926f));
                Assert.AreEqual((double)3.1415926f, SerializeDeserialize<float, double>(3.1415926f));
                Assert.IsTrue(SerializeDeserialize<float, bool>(3.1415926f));
            }
        }

        [Test]
        public void TestDouble()
        {
            unchecked
            {
                Assert.AreEqual(13, SerializeDeserialize<double, sbyte>(13.123));
                Assert.AreEqual(13, SerializeDeserialize<double, byte>(13.123));
                Assert.AreEqual(1000, SerializeDeserialize<double, short>(1000.382456));
                Assert.AreEqual(1000, SerializeDeserialize<double, ushort>(1000.382456));
                Assert.AreEqual(1000000000, SerializeDeserialize<double, int>(1000000000.73465));
                Assert.AreEqual(1000000000, SerializeDeserialize<double, uint>(1000000000.73465));
                Assert.AreEqual(12345678912345678, SerializeDeserialize<double, long>(12345678912345678.73465));
                Assert.AreEqual(12345678912345678, SerializeDeserialize<double, ulong>(12345678912345678.73465));
                Assert.AreEqual((float)3.1415926, SerializeDeserialize<double, float>(3.1415926));
                Assert.AreEqual(3.1415926, SerializeDeserialize<double, double>(3.1415926));
                Assert.IsTrue(SerializeDeserialize<double, bool>(3.1415926f));
            }
        }

        private TOut SerializeDeserialize<TIn, TOut>(TIn value)
        {
            return serializer.Deserialize<TOut>(serializer.Serialize(value));
        }

        private Serializer serializer;
    }
}