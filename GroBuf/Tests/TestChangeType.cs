using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestChangeType
    {
        private Serializer serializer;

        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer();
        }

        [Test]
        public void TestCopyGeneric()
        {
            var obj = new TestClassA {S = "qxx", B = new TestClassB {S = "zzz"}};
            var copiedObj = serializer.Copy(obj);
            Assert.IsNotNull(copiedObj);
            Assert.AreNotSame(obj, copiedObj);
            Assert.AreEqual("qxx", copiedObj.S);
            Assert.IsNotNull(copiedObj.B);
            Assert.AreEqual("zzz", copiedObj.B.S);
        }

        [Test]
        public void TestCopyNonGeneric()
        {
            var obj = new TestClassA {S = "qxx", B = new TestClassB {S = "zzz"}};
            var copiedObj = (TestClassA)serializer.Copy(typeof(TestClassA), obj);
            Assert.IsNotNull(copiedObj);
            Assert.AreNotSame(obj, copiedObj);
            Assert.AreEqual("qxx", copiedObj.S);
            Assert.IsNotNull(copiedObj.B);
            Assert.AreEqual("zzz", copiedObj.B.S);
        }

        [Test]
        public void TestChangeTypeGeneric()
        {
            var obj = new TestClassA {S = "qxx", B = new TestClassB {S = "zzz"}};
            var copiedObj = serializer.ChangeType<TestClassA, TestClassADerived>(obj);
            Assert.IsNotNull(copiedObj);
            Assert.AreNotSame(obj, copiedObj);
            Assert.AreEqual("qxx", copiedObj.S);
            Assert.IsNotNull(copiedObj.B);
            Assert.AreEqual("zzz", copiedObj.B.S);
        }

        [Test]
        public void TestChangeTypeNonGeneric()
        {
            var obj = new TestClassA {S = "qxx", B = new TestClassB {S = "zzz"}};
            var copiedObj = (TestClassADerived)serializer.ChangeType(typeof(TestClassA), typeof(TestClassADerived), obj);
            Assert.IsNotNull(copiedObj);
            Assert.AreNotSame(obj, copiedObj);
            Assert.AreEqual("qxx", copiedObj.S);
            Assert.IsNotNull(copiedObj.B);
            Assert.AreEqual("zzz", copiedObj.B.S);
        }

        public class TestClassA
        {
            public string S { get; set; }
            public TestClassB B { get; set; }
        }

        public class TestClassB
        {
            public string S { get; set; }
        }

        public class TestClassADerived: TestClassA
        {
        }
    }
}