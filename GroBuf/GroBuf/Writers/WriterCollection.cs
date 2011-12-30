using System;
using System.Collections;

namespace SKBKontur.GroBuf.Writers
{
    internal class WriterCollection : IWriterCollection
    {
        public unsafe WriterDelegate<T> GetWriter<T>()
        {
            var type = typeof(T);
            var writer = (WriterDelegate<T>)writers[type];
            if(writer == null)
            {
                lock(writersLock)
                {
                    writer = (WriterDelegate<T>)writers[type];
                    if(writer == null)
                    {
                        writer = BuildWriter<T>();
                        writers[type] = writer;
                    }
                }
            }
            return writer;
        }

        private unsafe WriterDelegate<T> BuildWriter<T>()
        {
            var type = typeof(T);
            IWriterBuilder<T> writerBuilder;
            if(type == typeof(string))
                writerBuilder = (IWriterBuilder<T>)new StringWriterBuilder(this);
            else if(type == typeof(DateTime))
                writerBuilder = (IWriterBuilder<T>)new DateTimeWriterBuilder(this);
            else if(type == typeof(Guid))
                writerBuilder = (IWriterBuilder<T>)new GuidWriterBuilder(this);
            else if(type.IsEnum)
                writerBuilder = new EnumWriterBuilder<T>(this);
            else if(type.IsPrimitive)
                writerBuilder = new PrimitivesWriterBuilder<T>(this);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                writerBuilder = new NullableWriterBuilder<T>(this);
            else if(type.IsArray)
                writerBuilder = new ArrayWriterBuilder<T>(this);
            else
                writerBuilder = new ClassWriterBuilder<T>(this);
            return writerBuilder.BuildWriter();
        }

        private readonly Hashtable writers = new Hashtable();
        private readonly object writersLock = new object();
    }
}