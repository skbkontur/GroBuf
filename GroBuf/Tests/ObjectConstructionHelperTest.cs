using System;

using GroBuf.Readers;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class ObjectConstructionHelperTest
    {
        [Test]
        public void TestEnptyConstructor()
        {
            Func<object> constructType = ObjectConstructionHelper.ConstructType(typeof(QueryWithEntities));
            Assert.IsNotNull(constructType());
            Assert.IsNotNull(constructType());
        }

        [Test]
        public void TestNoEmptyConstructor()
        {
            Func<object> constructType = ObjectConstructionHelper.ConstructType(typeof(QueryNotEmptyCtror));
            object anObject = constructType();
            Assert.IsNotNull(anObject);
            Assert.AreEqual(0, ((QueryNotEmptyCtror)anObject).A);
        }

        private class QueryWithEntities
        {
        }

        private class QueryNotEmptyCtror
        {
            public QueryNotEmptyCtror(int a)
            {
                A = a;
            }

            public readonly int A;
        }
    }
}