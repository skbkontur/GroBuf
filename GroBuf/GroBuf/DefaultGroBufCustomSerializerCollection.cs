using System;
using System.Linq;
using System.Reflection;

namespace GroBuf
{
    public class DefaultGroBufCustomSerializerCollection : IGroBufCustomSerializerCollection
    {
        public IGroBufCustomSerializer Get(Type declaredType, Func<Type, IGroBufCustomSerializer> factory, IGroBufCustomSerializer baseSerializer)
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
                new object[] {(Func<Type, SizeCounterDelegate>)(type => ((o, empty) => factory(type).CountSize(o, empty))), (SizeCounterDelegate)(baseSerializer.CountSize)});
            var writerDelegate = (WriterDelegate)writer.Invoke(
                null,
                new object[] {(Func<Type, WriterDelegate>)(type => ((object o, bool empty, IntPtr result, ref int index, int resultLength) => factory(type).Write(o, empty, result, ref index, resultLength))), (WriterDelegate)(baseSerializer.Write)});
            var readerDelegate = (ReaderDelegate)reader.Invoke(
                null,
                new object[] {(Func<Type, ReaderDelegate>)(type => ((IntPtr data, ref int index, ref object result, ReaderContext context) => factory(type).Read(data, ref index, ref result, context))), (ReaderDelegate)(baseSerializer.Read)});
            return new GroBufCustomSerializerByAttribute(sizeCounterDelegate, writerDelegate, readerDelegate);
        }
    }
}