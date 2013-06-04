using System;
using System.Collections;
using System.Collections.Generic;

namespace GroBuf.Writers
{
    internal class WriterCollection : IWriterCollection
    {
        public WriterCollection(IGroBufCustomSerializerCollection customSerializerCollection, Func<Type, IGroBufCustomSerializer> factory)
        {
            this.customSerializerCollection = customSerializerCollection;
            this.factory = factory;
        }

        public IWriterBuilder GetWriterBuilder(Type type)
        {
            var writerBuilder = (IWriterBuilder)writerBuilders[type];
            if(writerBuilder == null)
            {
                lock(writerBuildersLock)
                {
                    writerBuilder = (IWriterBuilder)writerBuilders[type];
                    if(writerBuilder == null)
                    {
                        writerBuilder = GetWriterBuilderInternal(type);
                        writerBuilders[type] = writerBuilder;
                    }
                }
            }
            return writerBuilder;
        }

        private IWriterBuilder GetWriterBuilderInternal(Type type)
        {
            IWriterBuilder writerBuilder;
            var customSerializer = customSerializerCollection.Get(type, factory);
            if(customSerializer != null)
                writerBuilder = new CustomWriterBuilder(type, customSerializer);
            else if(type == typeof(string))
                writerBuilder = new StringWriterBuilder();
            else if(type == typeof(DateTime))
                writerBuilder = new DateTimeWriterBuilder();
            else if(type == typeof(Guid))
                writerBuilder = new GuidWriterBuilder();
            else if(type.IsEnum)
                writerBuilder = new EnumWriterBuilder(type);
            else if(type.IsPrimitive || type == typeof(decimal))
                writerBuilder = new PrimitivesWriterBuilder(type);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                writerBuilder = new NullableWriterBuilder(type);
            else if(type.IsArray)
                writerBuilder = type.GetElementType().IsPrimitive ? (IWriterBuilder)new PrimitivesArrayWriterBuilder(type) : new ArrayWriterBuilder(type);
            else if(type == typeof(Array))
                writerBuilder = new ArrayWriterBuilder(type);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                writerBuilder = new DictionaryWriterBuilder(type);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                writerBuilder = type.GetGenericArguments()[0].IsPrimitive ? (IWriterBuilder)new PrimitivesListWriterBuilder(type) : new ListWriterBuilder(type);
            else if(type == typeof(object))
                writerBuilder = new ObjectWriterBuilder();
            else
                writerBuilder = new ClassWriterBuilder(type);
            return writerBuilder;
        }

        private readonly IGroBufCustomSerializerCollection customSerializerCollection;
        private readonly Func<Type, IGroBufCustomSerializer> factory;

        private readonly Hashtable writerBuilders = new Hashtable();
        private readonly object writerBuildersLock = new object();
    }
}