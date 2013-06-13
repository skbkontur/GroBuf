using System;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestCustomReadWriteCustomSerializer
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new SerializerImpl(new PropertiesExtractor(), new GroBufCustomSerializerCollection());
        }

        [Test]
        public void Test()
        {
            var o = new C1<int> {Data = 42};
            var data = serializer.Serialize<I1<int>>(o);
            var oo = serializer.Deserialize<I1<int>>(data);
            Assert.AreEqual(42, oo.Data);
        }

        [Test]
        public void Test2()
        {
            var o = new C2<int> {Arr = new I1<int>[] {new C1<int> {Data = 42}}};
            var data = serializer.Serialize(o);
            var oo = serializer.Deserialize<C2<int>>(data);
            Assert.IsNotNull(oo.Arr);
            Assert.AreEqual(1, oo.Arr.Length);
            Assert.AreEqual(42, oo.Arr[0].Data);
        }

        private SerializerImpl serializer;

        private class C1<T> : I1<T>
        {
            public T Data { get; set; }
        }

        private class C2<T>
        {
            public I1<T>[] Arr { get; set; }
        }

        private class GroBufCustomSerializerCollection : IGroBufCustomSerializerCollection
        {
            public IGroBufCustomSerializer Get(Type declaredType, Func<Type, IGroBufCustomSerializer> factory, IGroBufCustomSerializer baseSerializer)
            {
                if(declaredType.IsGenericType && declaredType.GetGenericTypeDefinition() == typeof(I1<>))
                    return new GroBufCustomSerializer(declaredType, factory, baseSerializer);
                return null;
            }
        }

        private class GroBufCustomSerializer : IGroBufCustomSerializer
        {
            public GroBufCustomSerializer(Type declaredType, Func<Type, IGroBufCustomSerializer> factory, IGroBufCustomSerializer baseSerializer)
            {
                argumentType = declaredType.GetGenericArguments()[0];
                this.factory = factory;
                this.baseSerializer = baseSerializer;
            }

            public int CountSize(object obj, bool writeEmpty)
            {
                return baseSerializer.CountSize(obj, writeEmpty);
            }

            public void Write(object obj, bool writeEmpty, IntPtr result, ref int index)
            {
                baseSerializer.Write(obj, writeEmpty, result, ref index);
            }

            public void Read(IntPtr data, ref int index, int length, ref object result)
            {
                var resultType = typeof(C1<>).MakeGenericType(argumentType);
                result = Activator.CreateInstance(resultType);
                factory(resultType).Read(data, ref index, length, ref result);
            }

            private readonly Type argumentType;
            private readonly Func<Type, IGroBufCustomSerializer> factory;
            private readonly IGroBufCustomSerializer baseSerializer;
        }

        private interface I1<T>
        {
            T Data { get; set; }
        }
    }
}