using System;
using System.Linq;
using System.Reflection;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestEnum
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor());
        }

        [Test]
        public void TestSerializeDeserialize()
        {
            var o = Enum1.Million;
            byte[] data = serializer.Serialize(o);
            var oo = serializer.Deserialize<Enum1>(data);
            Assert.AreEqual(o, oo);
        }

        [Test]
        public void TestSerializeEnumAsInt()
        {
            var o = (Enum1)200;
            byte[] data = serializer.Serialize(o);
            Assert.AreEqual(200, serializer.Deserialize<int>(data));
        }

        [Test]
        public void TestDeserializeIntAsEnum()
        {
            int o = 123456789;
            byte[] data = serializer.Serialize(o);
            var oo = serializer.Deserialize<Enum1>(data);
            Assert.AreEqual((Enum1)123456789, oo);
            o = (int)Enum1.Thousand;
            data = serializer.Serialize(o);
            oo = serializer.Deserialize<Enum1>(data);
            Assert.AreEqual(Enum1.Thousand, oo);
        }

        [Test]
        public void TestDeserializeStringAsEnum()
        {
            string o = Enum1.Thousand.ToString();
            byte[] data = serializer.Serialize(o);
            var oo = serializer.Deserialize<Enum1>(data);
            Assert.AreEqual(Enum1.Thousand, oo);
            o = "zzz";
            data = serializer.Serialize(o);
            oo = serializer.Deserialize<Enum1>(data);
            Assert.AreEqual((Enum1)0, oo);
        }

        [Test]
        public void EnumItemAdded()
        {
            var o = Enum2_Old.Seven;
            byte[] data = serializer.Serialize(o);
            var oo = serializer.Deserialize<Enum2_New>(data);
            Assert.AreEqual(Enum2_New.Seven, oo);
        }

        [Test]
        public void EnumEnumBug1()
        {
            byte[] data = serializer.Serialize(new PrintParameters {});
            var oo = serializer.Deserialize<PrintParameters>(data);
            Assert.AreEqual((KopfPrintType)0, oo.PrintType);
        }

        [Test]
        public void EnumEnumBug2()
        {
            byte[] data = serializer.Serialize(new PrintParameters2 {});
            var oo = serializer.Deserialize<PrintParameters2>(data);
            Assert.AreEqual((KopfPrintType2)0, oo.PrintType);
        }

        [Test]
        public void TestMultipleItemsHasSameValue()
        {
            var o = Enum3.Одиннадцать;
            byte[] data = serializer.Serialize(o);
            var oo = serializer.Deserialize<Enum3>(data);
            Assert.IsTrue(oo == Enum3.Eleven || oo == Enum3.Одиннадцать);
        }

        [Test]
        public void TestReadWriteToId()
        {
            var o = Enum1.One;
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<Enum1x>(data);
            Assert.AreEqual(Enum1x.OneX, oo);
        }

        [Test]
        public void TestReadWriteFromId()
        {
            var o = Enum1x.OneX;
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<Enum1>(data);
            Assert.AreEqual(Enum1.One, oo);
        }

        [Test]
        public void Test()
        {
            var o = Enum_BadSort1.X;
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<Enum_BadSort2>(data);
            Assert.AreEqual(Enum_BadSort2.X, oo);
        }

        [Test]
        public void TestZ()
        {
            var values = Enum.GetValues(typeof(TaskState));
            for (int i = 0; i < values.Length; ++i)
                Console.WriteLine(values.GetValue(i));
            Console.WriteLine(typeof(TaskState).GetField("Finished", BindingFlags.Public | BindingFlags.Static));
            var fields = typeof(TaskState).GetFields(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < fields.Length; ++i)
                Console.WriteLine(fields.GetValue(i));
        }

        [Test]
        public void TestEnumWithNegative()
        {
            var o = serializer.Serialize(EnumWithNegative.Positive);
            var oo = serializer.Deserialize<EnumWithNegative>(o);
            Assert.AreEqual(EnumWithNegative.Positive, oo);
        }

        [Test]
        public void TestInt8()
        {
            foreach (var o in Enum.GetValues(typeof(Int8Enum)).Cast<Int8Enum>().Concat(new[] {(Int8Enum)sbyte.MaxValue}))
            {
                var data = serializer.Serialize(o);
                var oo = serializer.Deserialize<Int8Enum>(data);
                Assert.That(oo, Is.EqualTo(o));
            }
        }

        [Test]
        public void TestUInt8()
        {
            foreach (var o in Enum.GetValues(typeof(UInt8Enum)).Cast<UInt8Enum>().Concat(new[] {(UInt8Enum)byte.MaxValue}))
            {
                var data = serializer.Serialize(o);
                var oo = serializer.Deserialize<UInt8Enum>(data);
                Assert.That(oo, Is.EqualTo(o));
            }
        }

        [Test]
        public void TestInt16()
        {
            foreach (var o in Enum.GetValues(typeof(Int16Enum)).Cast<Int16Enum>().Concat(new[] {(Int16Enum)short.MaxValue}))
            {
                var data = serializer.Serialize(o);
                var oo = serializer.Deserialize<Int16Enum>(data);
                Assert.That(oo, Is.EqualTo(o));
            }
        }

        [Test]
        public void TestUInt16()
        {
            foreach (var o in Enum.GetValues(typeof(UInt16Enum)).Cast<UInt16Enum>().Concat(new[] {(UInt16Enum)ushort.MaxValue}))
            {
                var data = serializer.Serialize(o);
                var oo = serializer.Deserialize<UInt16Enum>(data);
                Assert.That(oo, Is.EqualTo(o));
            }
        }

        [Test]
        public void TestInt32()
        {
            foreach (var o in Enum.GetValues(typeof(Int32Enum)).Cast<Int32Enum>().Concat(new[] {(Int32Enum)int.MaxValue}))
            {
                var data = serializer.Serialize(o);
                var oo = serializer.Deserialize<Int32Enum>(data);
                Assert.That(oo, Is.EqualTo(o));
            }
        }

        [Test]
        public void TestUInt32()
        {
            foreach (var o in Enum.GetValues(typeof(UInt32Enum)).Cast<UInt32Enum>().Concat(new[] {(UInt32Enum)uint.MaxValue}))
            {
                var data = serializer.Serialize(o);
                var oo = serializer.Deserialize<UInt32Enum>(data);
                Assert.That(oo, Is.EqualTo(o));
            }
        }

        public enum TaskState
        {
            Unknown = 0,

            New,

            WaitingForRerun,

            WaitingForRerunAfterError,

            Finished,

            InProcess,

            Fatal,

            Canceled,
        }

        public enum Enum_BadSort1
        {
            X = 3,
            Y = 1
        }

        public enum Enum_BadSort2
        {
            X,
            Z,
            Y
        }

        public enum Enum1
        {
            One = 1,
            Three = 3,
            Five = 5,
            Seven = 7,
            Nine = 9,
            Eleven = 11,
            Hundred = 100,
            Thousand = 1000,
            Million = 1000000,
            Billion = 1000000000
        }

        public enum Enum1x
        {
            [GroboMember(9186681566302519059)]
            OneX = 1
        }

        public enum Enum2_Old
        {
            One,
            Three,
            Five,
            Seven,
            Nine
        }

        public enum Enum2_New
        {
            One,
            Two,
            Three,
            Four,
            Five,
            Six,
            Seven,
            Eight,
            Nine
        }

        public enum Enum3
        {
            One = 1,
            Один = 1,
            Three = 3,
            Три = 3,
            Five = 5,
            Пять = 5,
            Seven = 7,
            Семь = 7,
            Nine = 9,
            Девять = 9,
            Eleven = 11,
            Одиннадцать = 11,
            Hundred = 100,
            Сто = 100,
            Thousand = 1000,
            Тысяча = 1000,
            Million = 1000000,
            Миллион = 1000000,
            Billion = 1000000000,
            Миллиард = 1000000000
        }

        private enum Int8Enum : sbyte
        {
            X = -1,
            Y = 1,
            Z = 100,
            Q = -100
        }

        private enum UInt8Enum : byte
        {
            X = 1,
            Y = 2,
            Z = 200
        }

        private enum Int16Enum : short
        {
            X = -1,
            Y = 1,
            Z = 1000,
            Q = -1000
        }

        private enum UInt16Enum : ushort
        {
            X = 1,
            Y = 2,
            Z = 60000
        }

        private enum Int32Enum : int
        {
            X = -1,
            Y = 1,
            Z = 2000000000,
            Q = -2000000000
        }

        private enum UInt32Enum : uint
        {
            X = 1,
            Y = 2,
            Z = 4000000000
        }

        private enum KopfPrintType
        {
            Pdf = 1,
            Excel = 2
        }

        private enum KopfPrintType2
        {
            Pdf = 4,
            Excel = 5
        }

        private enum EnumWithNegative
        {
            Negative = -1,
            Zero = 0,
            Positive = 1
        }

        private Serializer serializer;

        private class PrintParameters
        {
            public KopfPrintType PrintType { get; set; }
        }

        private class PrintParameters2
        {
            public KopfPrintType2 PrintType { get; set; }
        }
    }
}