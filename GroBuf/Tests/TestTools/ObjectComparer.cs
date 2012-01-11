using NUnit.Framework;

namespace GroBuf.Tests.TestTools
{
    public static class ObjectComparer
    {
        public static void AssertEqualsTo<T>(this T actual, T expected)
        {
            string badXml = "<root></root>".ReformatXml();
            string expectedStr = expected.ObjectToString();
            Assert.AreNotEqual(expectedStr.ReformatXml(), badXml, "bug(expected)");
            string actualStr = actual.ObjectToString();
            Assert.AreNotEqual(actualStr.ReformatXml(), badXml, "bug(actual)");
            Assert.AreEqual(expectedStr, actualStr);
        }
    }
}