using System;
using System.Collections;

namespace GroBuf.Writers
{
    internal class WriterCollection : IWriterCollection
    {
        public IWriterBuilder<T> GetWriterBuilder<T>()
        {
            var type = typeof(T);
            var writerBuilder = (IWriterBuilder<T>)writerBuilders[type];
            if(writerBuilder == null)
            {
                lock(writerBuildersLock)
                {
                    writerBuilder = (IWriterBuilder<T>)writerBuilders[type];
                    if(writerBuilder == null)
                    {
                        writerBuilder = GetWriterBuilderInternal<T>();
                        writerBuilders[type] = writerBuilder;
                    }
                }
            }
            return writerBuilder;
        }

        private static IWriterBuilder<T> GetWriterBuilderInternal<T>()
        {
            var type = typeof(T);
            IWriterBuilder<T> writerBuilder;
            if(type == typeof(string))
                writerBuilder = (IWriterBuilder<T>)new StringWriterBuilder();
            else if(type == typeof(DateTime))
                writerBuilder = (IWriterBuilder<T>)new DateTimeWriterBuilder();
            else if(type == typeof(Guid))
                writerBuilder = (IWriterBuilder<T>)new GuidWriterBuilder();
            else if(type.IsEnum)
                writerBuilder = new EnumWriterBuilder<T>();
            else if(type.IsPrimitive)
                writerBuilder = new PrimitivesWriterBuilder<T>();
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                writerBuilder = new NullableWriterBuilder<T>();
            else if(type.IsArray)
                writerBuilder = new ArrayWriterBuilder<T>();
            else
                writerBuilder = new ClassWriterBuilder<T>();
            return writerBuilder;
        }

        private readonly Hashtable writerBuilders = new Hashtable();
        private readonly object writerBuildersLock = new object();
    }
}