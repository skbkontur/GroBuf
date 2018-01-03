using System;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestDateTime
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor());
        }

//        public enum TaskState
//        {
//            Unknown = 0,
//
//            New,
//
//            WaitingForRerun,
//
//            WaitingForRerunAfterError,
//
//            Finished,
//
//            InProcess,
//
//            Fatal,
//
//            Canceled,
//        }
//
//        public class TaskMetaInformation
//        {
//            public override string ToString()
//            {
//                return string.Format("Name: {0}, Id: {1}, Attempts: {2}, ParentTaskId: {3}", Name, Id, Attempts, ParentTaskId);
//            }
//
//            public string Name { get; set; }
//            public string Id { get; set; }
//            public long Ticks { get; set; }
//
//            //[Indexed]
//            public long MinimalStartTicks { get; set; }
//            //[Indexed]
//            public long? StartExecutingTicks { get; set; }
//            //[Indexed]
//            public TaskState State { get; set; }
//            //[Indexed]
//            public int Attempts { get; set; }
//            //[Indexed]
//            public string ParentTaskId { get; set; }
//        }
//
//        [Test]
//        public void Test()
//        {
//            var bytes = Convert.FromBase64String("AcYAAAD0Zlbntp8ivA4kAAAAUwB0AGEAcgB0AEMAaABlAGMAawBQAGEAYwBrAFQAYQBzAGsAUt22K2BnLl8OSAAAADAAYwBlADIAMwBmADAAOAAtADkANgA3AGMALQA0AGYAOQBkAC0AOQA2ADEAMAAtADQANAAwADUAYQAzAGUAMQA2AGYANwAxAKmp6gI2dO8BCeRQWdO72M8IzsUB5ibZfjkJ5VBZ07vYzwhoFV/g/66KdhAk+/Hk+IRuEE/gBJLjUzaiBwAAAAA=");
////            unsafe
////            {
////                fixed(byte* b = &bytes[0])
////                {
////                    //sbyte* sb = (sbyte*)b;
////                    //Console.WriteLine(new string(sb, 67, 72, Encoding.Unicode));
////                }
////            }
////            Console.WriteLine(BitConverter.ToUInt64(bytes, 190));
//            var meta = serializer.Deserialize<TaskMetaInformation>(bytes);
//            var hashes = GroBufHelpers.CalcHashAndCheck(new PropertiesExtractor().GetMembers(typeof(TaskMetaInformation)).Select(member => member.Name));
//            foreach(var hash in hashes)
//                Console.WriteLine((ulong)hash);
//
//            Console.WriteLine(meta);
//        }

        [Test]
        public void TestUtc()
        {
            var o = new DateTime(12735641765, DateTimeKind.Utc);
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<DateTime>(data);
            Assert.AreEqual(DateTimeKind.Utc, oo.Kind);
            Assert.AreEqual(12735641765, oo.Ticks);
        }

        [Test]
        public void TestLong()
        {
            var o = 12735641765;
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<DateTime>(data);
            Assert.AreEqual(DateTimeKind.Utc, oo.Kind);
            Assert.AreEqual(12735641765, oo.Ticks);
        }

        [Test]
        public void TestLong2()
        {
            var data = serializer.Serialize(long.MinValue);
            Assert.Throws<DataCorruptedException>(() => serializer.Deserialize<DateTime>(data));
        }

        [Test]
        public void TestLocal()
        {
            var o = new DateTime(12735641765, DateTimeKind.Local);
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<DateTime>(data);
            Assert.AreEqual(DateTimeKind.Local, oo.Kind);
            Assert.AreEqual(12735641765, oo.Ticks);
        }

        [Test]
        public void TestLocalOldFormat()
        {
            var data = new byte[10];
            data[0] = (byte)GroBufTypeCode.DateTimeOld;
            Array.Copy(BitConverter.GetBytes(1234567891234 | long.MinValue), 0, data, 1, 8);
            data[9] = (byte)DateTimeKind.Local;
            var o = serializer.Deserialize<DateTime>(data);
            Assert.AreEqual(DateTimeKind.Local, o.Kind);
            Assert.AreEqual(1234567891234, o.Ticks);
        }

        [Test]
        public void TestUnspecified()
        {
            var o = new DateTime(12735641765, DateTimeKind.Unspecified);
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<DateTime>(data);
            Assert.AreEqual(DateTimeKind.Unspecified, oo.Kind);
            Assert.AreEqual(12735641765, oo.Ticks);
        }

        [Test]
        public void TestSkipDateTimeLocal()
        {
            var o = new TestClassA {S = "zzz", DateTime = new DateTime(12387401892734, DateTimeKind.Local), Z = "qxx"};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<TestClassB>(data);
            Assert.AreEqual("zzz", oo.S);
            Assert.AreEqual("qxx", oo.Z);
        }

        [Test]
        public void TestSkipDateTimeUtc()
        {
            var o = new TestClassA {S = "zzz", DateTime = new DateTime(12387401892734, DateTimeKind.Utc), Z = "qxx"};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<TestClassB>(data);
            Assert.AreEqual("zzz", oo.S);
            Assert.AreEqual("qxx", oo.Z);
        }

        [Test]
        public void TestSkipDateTimeUnspecified()
        {
            var o = new TestClassA {S = "zzz", DateTime = new DateTime(12387401892734, DateTimeKind.Unspecified), Z = "qxx"};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<TestClassB>(data);
            Assert.AreEqual("zzz", oo.S);
            Assert.AreEqual("qxx", oo.Z);
        }

        public class TestClassA
        {
            public string S { get; set; }
            public DateTime DateTime { get; set; }
            public string Z { get; set; }
        }

        public class TestClassB
        {
            public string S { get; set; }
            public string Z { get; set; }
        }

        private Serializer serializer;
    }
}