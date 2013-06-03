using System;
using System.Linq;
using System.Reflection;

namespace GroBuf
{
    public class DefaultGroBufCustomSerializerCollection : IGroBufCustomSerializerCollection
    {
        public IGroBufCustomSerializer Get(Type declaredType, Func<Type, IGroBufCustomSerializer> factory)
        {
            var attribute = declaredType.GetCustomAttributes(typeof(GroBufCustomSerializationAttribute), false).FirstOrDefault() as GroBufCustomSerializationAttribute;
            if(attribute == null) return null;
            Type customSerializerType = attribute.CustomSerializerType ?? declaredType;
            MethodInfo customSizeCounter = GroBufHelpers.GetMethod<GroBufSizeCounterAttribute>(customSerializerType);
            if(customSizeCounter == null)
                throw new MissingMethodException("Missing grobuf custom size counter for type '" + customSerializerType + "'");
            MethodInfo writer = GroBufHelpers.GetMethod<GroBufWriterAttribute>(customSerializerType);
            if(writer == null)
                throw new MissingMethodException("Missing grobuf custom writer for type '" + customSerializerType + "'");
            MethodInfo reader = GroBufHelpers.GetMethod<GroBufReaderAttribute>(customSerializerType);
            if(reader == null)
                throw new MissingMethodException("Missing grobuf custom reader for type '" + customSerializerType + "'");
            var sizeCounterDelegate = (SizeCounterDelegate)customSizeCounter.Invoke(
                null,
                new object[] {(Func<Type, SizeCounterDelegate>)(type => ((o, empty) => factory(type).CountSize(o, empty)))});
            var writerDelegate = (WriterDelegate)writer.Invoke(
                null,
                new object[] {(Func<Type, WriterDelegate>)(type => ((object o, bool empty, IntPtr result, ref int index) => factory(type).Write(o, empty, result, ref index)))});
            var readerDelegate = (ReaderDelegate)reader.Invoke(
                null,
                new object[] {(Func<Type, ReaderDelegate>)(type => ((IntPtr data, ref int index, int length, ref object result) => factory(type).Read(data, ref index, length, ref result)))});
            return new GroBufCustomSerializerByAttribute(sizeCounterDelegate, writerDelegate, readerDelegate);
        }
    }
}