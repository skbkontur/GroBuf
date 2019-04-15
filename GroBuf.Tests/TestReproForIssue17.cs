using System.Collections.Generic;

using FluentAssertions;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    public class TestReproForIssue17
    {
        [Test]
        public void AllFieldsExtractor_Supports_ValueTuple_As_DictionaryKey()
        {
            TestValueTupleAsDictionaryKey(new Serializer(new AllFieldsExtractor()));
        }

        [Test]
        [Explicit("repro for https://github.com/skbkontur/GroBuf/issues/17")]
        public void AllPropertiesExtractor_DoesNotSupport_ValueTuple_As_DictionaryKey()
        {
            TestValueTupleAsDictionaryKey(new Serializer(new AllPropertiesExtractor()));
        }

        private static void TestValueTupleAsDictionaryKey(Serializer serializer)
        {
            var key1 = ("k11", "k12");
            var key2 = ("k21", "k22");

            Assert.That(key1, Is.Not.EqualTo(key2));
            Assert.That(key1.GetHashCode(), Is.Not.EqualTo(key2.GetHashCode()));

            var dict = new Dictionary<(string, string), string>
                {
                    {key1, "v1"},
                    {key2, "v2"}
                };

            var bytes = serializer.Serialize(dict);
            var deserializedDict = serializer.Deserialize<Dictionary<(string, string), string>>(bytes);

            deserializedDict.Should().BeEquivalentTo(dict);
        }
    }
}