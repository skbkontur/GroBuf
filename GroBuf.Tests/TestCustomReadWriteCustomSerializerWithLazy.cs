using System;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestCustomReadWriteCustomSerializerWithLazy
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor(), new GroBufCustomSerializerCollection());
        }

        [Test]
        public void Test_C()
        {
            var o = new Data { B = new Lazy<B>(() => new C { S = "zzz" }) };
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<Data>(data);
            Assert.AreEqual("zzz", ((C)oo.B.Value).S);
        }

        [Test]
        public void Test_D()
        {
            var o = new Data { B = new Lazy<B>(() => new D { X = 42 }) };
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<Data>(data);
            Assert.AreEqual(42, ((D)oo.B.Value).X);
        }

        [Test]
        public void Test_DataIsNotLostWhenSerializingWithNoContract()
        {
            var o = new Data { B = new Lazy<B>(() => new C { S = "zzz" }) };
            var data = serializer.Serialize(o);
            var serializerWithNoContract = new Serializer(new AllPropertiesExtractor());
            var oo = serializerWithNoContract.Deserialize<Data>(data);
            var ddata = serializerWithNoContract.Serialize(oo);
            Assert.IsNull(oo.B.Value);
            var ooo = serializer.Deserialize<Data>(ddata);
            Assert.AreEqual("zzz", ((C)ooo.B.Value).S);
        }

        private Serializer serializer;

        private class GroBufCustomSerializerCollection : IGroBufCustomSerializerCollection
        {
            public IGroBufCustomSerializer Get(Type declaredType, Func<Type, IGroBufCustomSerializer> factory, IGroBufCustomSerializer baseSerializer)
            {
                if(typeof(B) == (declaredType))
                    return new GroBufCustomSerializer(factory, baseSerializer);
                return null;
            }
        }

        private class GroBufCustomSerializer : IGroBufCustomSerializer
        {
            public GroBufCustomSerializer(Func<Type, IGroBufCustomSerializer> factory, IGroBufCustomSerializer baseSerializer)
            {
                this.factory = factory;
                this.baseSerializer = baseSerializer;
            }

            public int CountSize(object obj, bool writeEmpty, WriterContext context)
            {
                Type type = obj.GetType();
                return factory(typeof(string)).CountSize(type.Name, writeEmpty, context) + factory(type).CountSize(obj, writeEmpty, context);
            }

            public void Write(object obj, bool writeEmpty, IntPtr result, ref int index, WriterContext context)
            {
                Type type = obj.GetType();
                factory(typeof(string)).Write(type.Name, writeEmpty, result, ref index, context);
                factory(type).Write(obj, writeEmpty, result, ref index, context);
            }

            public void Read(IntPtr data, ref int index, ref object result, ReaderContext context)
            {
                object typeName = null;
                Type type;
                factory(typeof(string)).Read(data, ref index, ref typeName, context);
                if((string)typeName == typeof(C).Name)
                    type = typeof(C);
                else if((string)typeName == typeof(D).Name)
                    type = typeof(D);
                else throw new InvalidOperationException("Unknown type " + typeName);
                factory(type).Read(data, ref index, ref result, context);
            }

            private readonly Func<Type, IGroBufCustomSerializer> factory;
            private readonly IGroBufCustomSerializer baseSerializer;
        }

        public abstract class B
        {
        }

        public class C : B
        {
            public string S { get; set; }
        }

        public class D : B
        {
            public int X { get; set; }
        }

        public class Data
        {
            public Lazy<B> B { get; set; }
        }
    }
}