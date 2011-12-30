using System;
using System.Collections;

namespace SKBKontur.GroBuf.Readers
{
    internal class ReaderCollection : IReaderCollection
    {
        public unsafe ReaderDelegate<T> GetReader<T>()
        {
            var type = typeof(T);
            var reader = (ReaderDelegate<T>)readers[type];
            if(reader == null)
            {
                lock(readersLock)
                {
                    reader = (ReaderDelegate<T>)readers[type];
                    if(reader == null)
                    {
                        reader = BuildReader<T>();
                        readers[type] = reader;
                    }
                }
            }
            return reader;
        }

        private unsafe ReaderDelegate<T> BuildReader<T>()
        {
            var type = typeof(T);
            IReaderBuilder<T> readerBuilder;
            if(type == typeof(string))
                readerBuilder = (IReaderBuilder<T>)new StringReaderBuilder(this);
            else if(type == typeof(DateTime))
                readerBuilder = (IReaderBuilder<T>)new DateTimeReaderBuilder(this);
            else if(type == typeof(Guid))
                readerBuilder = (IReaderBuilder<T>)new GuidReaderBuilder(this);
            else if(type.IsEnum)
                readerBuilder = new EnumReaderBuilder<T>(this);
            else if(type.IsPrimitive)
                readerBuilder = new PrimitivesReaderBuilder<T>(this);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                readerBuilder = new NullableReaderBuilder<T>(this);
            else if(type.IsArray)
                readerBuilder = new ArrayReaderBuilder<T>(this);
            else
                readerBuilder = new ClassReaderBuilder<T>(this);
            return readerBuilder.BuildReader();
        }

        private readonly Hashtable readers = new Hashtable();
        private readonly object readersLock = new object();
    }
}