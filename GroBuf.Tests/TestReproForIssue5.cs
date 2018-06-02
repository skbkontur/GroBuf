using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [Explicit("repro for https://github.com/skbkontur/GroBuf/issues/5")]
    public class TestReproForIssue5
    {
        [Test]
        public void AbstractProperty()
        {
            var bytes = serializer.Serialize(new DerivedWithAbstractProp(42));
            Assert.That(serializer.Deserialize<DerivedWithAbstractProp>(bytes).Prop, Is.EqualTo(42));
        }

        [Test]
        public void VirtualProperty()
        {
            var bytes = serializer.Serialize(new DerivedWithVirtualProp(42));
            Assert.That(serializer.Deserialize<DerivedWithVirtualProp>(bytes).Prop, Is.EqualTo(42));
        }

        private readonly Serializer serializer = new Serializer(new AllPropertiesExtractor(), null, GroBufOptions.MergeOnRead);

        private abstract class BaseWithVirtualProp
        {
            public virtual int Prop { get; }
        }

        private class DerivedWithVirtualProp : BaseWithVirtualProp
        {
            public DerivedWithVirtualProp(int prop)
            {
                Prop = prop;
            }

            public override int Prop { get; }
        }

        private abstract class BaseWithAbstractProp
        {
            public abstract int Prop { get; }
        }

        private class DerivedWithAbstractProp : BaseWithAbstractProp
        {
            public DerivedWithAbstractProp(int prop)
            {
                Prop = prop;
            }

            public override int Prop { get; }
        }
    }
}