using System;
using System.Collections;

namespace SKBKontur.GroBuf.Readers
{
    internal class ReaderCollection : IReaderCollection
    {
        public IReaderBuilder<T> GetReader<T>()
        {
            var type = typeof(T);
            var readerBuilder = (IReaderBuilder<T>)readerBuilders[type];
            if(readerBuilder == null)
            {
                lock(readerBuildersLock)
                {
                    readerBuilder = (IReaderBuilder<T>)readerBuilders[type];
                    if(readerBuilder == null)
                    {
                        readerBuilder = BuildReader<T>();
                        readerBuilders[type] = readerBuilder;
                    }
                }
            }
            return readerBuilder;
        }

        private static IReaderBuilder<T> BuildReader<T>()
        {
            var type = typeof(T);
            IReaderBuilder<T> readerBuilder;
            if(type == typeof(string))
                readerBuilder = (IReaderBuilder<T>)new StringReaderBuilder();
            else if(type == typeof(DateTime))
                readerBuilder = (IReaderBuilder<T>)new DateTimeReaderBuilder();
            else if(type == typeof(Guid))
                readerBuilder = (IReaderBuilder<T>)new GuidReaderBuilder();
            else if(type.IsEnum)
                readerBuilder = new EnumReaderBuilder<T>();
            else if(type.IsPrimitive)
                readerBuilder = new PrimitivesReaderBuilder<T>();
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                readerBuilder = new NullableReaderBuilder<T>();
            else if(type.IsArray)
                readerBuilder = new ArrayReaderBuilder<T>();
            else
                readerBuilder = new ClassReaderBuilder<T>();
            return readerBuilder;
        }

        private readonly Hashtable readerBuilders = new Hashtable();
        private readonly object readerBuildersLock = new object();
    }
}