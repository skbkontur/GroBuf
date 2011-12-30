using System;

using NUnit.Framework;

namespace SKBKontur.GroBuf.Tests
{
    [TestFixture]
    internal class GroBufWriterTest
    {
        [SetUp]
        public void Setup()
        {
            writer = new GroBufWriter();
            reader = new GroBufReader();
        }

        enum Zenum
        {
            Zero,
            One,
            Two,
            Four,
            Six,
            Seven,
            Nine,
            Ten
        }

        enum Zenum2
        {
            Zero,
            One,
            Two,
            Three,
            Four,
            Six,
            Seven,
            Nine,
            Ten
        }

        [Test]
        public void Test()
        {
            var o = Zenum.Ten;///*new int?[]{1, null, 2};*/ new A {S = "zzz", B = new B {S = "xxx", Arr = new int?[] {123, null}}, zzz = new[] {new B {S = "qzz", Arr = new int?[] {null, null, 1}},}} /*new A{S = "qxx"}*/;
            var oi = (int)o;
            var data = writer.Write(o);
            Console.WriteLine(data);
            var oo = reader.Read<Zenum2>(data);
            var ooi = (int)oo;
            Console.WriteLine(oo);
            Console.WriteLine(oi + " " + ooi);
        }

        private GroBufWriter writer;
        private GroBufReader reader;

        private class Z
        {
            public string S { get; set; }
        }

        private struct A
        {
            public Guid Guid { get; set; }
            public string S { get; set; }
            public int?[] Arr { get; set; }
            public B B { get; set; }
            public B[] zzz { get; set; }
        }

        private struct B
        {
            public string S { get; set; }
            public int?[] Arr { get; set; }
        }
    }
}