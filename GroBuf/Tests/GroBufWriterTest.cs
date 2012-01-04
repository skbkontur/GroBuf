using System;

using NUnit.Framework;

namespace SKBKontur.GroBuf.Tests
{
    [TestFixture]
    public class GroBufWriterTest
    {
        [SetUp]
        public void Setup()
        {
            writer = new GroBufWriter();
            reader = new GroBufReader();
        }

        [Test]
        public void Test()
        {
//            var o = Zenum.Ten;
//            var oi = (int)o;
            var o = /*new int?[]{1, null, 2};*/ /*new A {S = "zzz", B = new B {S = "xxx", Arr = new int?[] {123, null}}, zzz = new[] {new B {S = "qzz", Arr = new int?[] {null, null, 1}},}}*/ new A {S = "qxx"};
            var data = writer.Write(o);
            Console.WriteLine(data);
            var oo = reader.Read<A>(data);
            //var ooi = (int)oo;
            Console.WriteLine(oo);
            //Console.WriteLine(oi + " " + ooi);
        }

        public class Z
        {
            public string S { get; set; }
        }

        public struct A
        {
            public Guid Guid { get; set; }
            public string S { get; set; }
            public int?[] Arr { get; set; }
            public B B { get; set; }
            public B[] zzz { get; set; }
        }

        public struct B
        {
            public string S { get; set; }
            public int?[] Arr { get; set; }
        }

        private GroBufWriter writer;
        private GroBufReader reader;

        private enum Zenum
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

        private enum Zenum2
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
    }
}