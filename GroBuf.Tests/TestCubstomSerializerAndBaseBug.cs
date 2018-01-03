using System;

using GroBuf.DataMembersExtracters;

using NUnit.Framework;

namespace GroBuf.Tests
{
    [TestFixture]
    public class TestCubstomSerializerAndBaseBug
    {
        [SetUp]
        public void SetUp()
        {
            serializer = new Serializer(new PropertiesExtractor(), new GroBufCustomSerializerCollection());
        }

        [Test]
        public void TestBug()
        {
            I1 i1 = new I1Impl {Person = new Person {Date = new Date2 {Xxx = 1}}};
            i1.Person.Date.Do();
            var deserialize = serializer.Deserialize<I1>(serializer.Serialize(i1));
            Assert.AreEqual(i1.Person.Date.Xxx, deserialize.Person.Date.Xxx);
            Assert.AreEqual(i1.Person.Date.qxx, deserialize.Person.Date.qxx);
        }

        [Test]
        public void TestNoBug()
        {
            var i1 = new I1Impl {Person = new Person {Date = new Date2 {Xxx = 1}}};
            i1.Person.Date.Do();
            var deserialize = serializer.Deserialize<I1Impl>(serializer.Serialize(i1));
            Assert.AreEqual(i1.Person.Date.Xxx, deserialize.Person.Date.Xxx);
            Assert.AreEqual(i1.Person.Date.qxx, deserialize.Person.Date.qxx);
        }

        private Serializer serializer;

        private class DateGroBufCustomSerializer : IGroBufCustomSerializer
        {
            public DateGroBufCustomSerializer(IGroBufCustomSerializer baseSerializer)
            {
                this.baseSerializer = baseSerializer;
            }

            #region IGroBufCustomSerializer Members

            public int CountSize(object obj, bool writeEmpty, WriterContext context)
            {
                return baseSerializer.CountSize(obj, writeEmpty, context);
            }

            public void Write(object obj, bool writeEmpty, IntPtr result, ref int index, WriterContext context)
            {
                baseSerializer.Write(obj, writeEmpty, result, ref index, context);
            }

            public void Read(IntPtr data, ref int index, ref object result, ReaderContext context)
            {
                var date = new Date2();
                result = date;
                baseSerializer.Read(data, ref index, ref result, context);
                date.Do(); //hack
            }

            #endregion

            private readonly IGroBufCustomSerializer baseSerializer;
        }

        private class I1Impl : I1
        {
            public Person Person { get; set; }
        }

        private class Person
        {
            public Date2 Date { get; set; }
        }

        private class Date2
        {
            public void Do()
            {
                qxx = 1;
            }

            public int qxx;
            public int Xxx { get; set; }
        }

        private class GroBufCustomSerializerCollection : IGroBufCustomSerializerCollection
        {
            public IGroBufCustomSerializer Get(Type declaredType, Func<Type, IGroBufCustomSerializer> factory, IGroBufCustomSerializer baseSerializer)
            {
                if(declaredType == typeof(I1))
                    return new GroBufCustomSerializerI1(factory, baseSerializer);
                if(declaredType == typeof(Date2))
                    return new DateGroBufCustomSerializer(baseSerializer);
                return null;
            }
        }

        private class GroBufCustomSerializerI1 : IGroBufCustomSerializer
        {
            public GroBufCustomSerializerI1(Func<Type, IGroBufCustomSerializer> factory, IGroBufCustomSerializer baseSerializer)
            {
                this.factory = factory;
                this.baseSerializer = baseSerializer;
            }

            public int CountSize(object obj, bool writeEmpty, WriterContext context)
            {
                return baseSerializer.CountSize(obj, writeEmpty, context);
            }

            public void Write(object obj, bool writeEmpty, IntPtr result, ref int index, WriterContext context)
            {
                baseSerializer.Write(obj, writeEmpty, result, ref index, context);
            }

            public void Read(IntPtr data, ref int index, ref object result, ReaderContext context)
            {
                result = Activator.CreateInstance(typeof(I1Impl));
                factory(typeof(I1Impl)).Read(data, ref index, ref result, context);
            }

            private readonly Func<Type, IGroBufCustomSerializer> factory;
            private readonly IGroBufCustomSerializer baseSerializer;
        }

        private interface I1
        {
            Person Person { get; set; }
        }
    }
}