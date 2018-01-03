using GroBuf.DataMembersExtracters;

using NUnit.Framework;

using System.Linq;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestPropertiesExtractor
    {
        [Test]
        public void TestPrivatePropertyInBaseClass()
        {
            CollectionAssert.AreEquivalent(new[] {"S", "X"}, new AllPropertiesExtractor().GetMembers(typeof(B)).Select(member => member.Name));
        }

        public class A
        {
            private string S { get; set; }
        }

        public class B: A
        {
            public int X { get; set; }
        }
    }
}