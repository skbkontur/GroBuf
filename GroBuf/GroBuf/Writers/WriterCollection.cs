using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace GroBuf.Writers
{
    internal class WriterCollection : IWriterCollection
    {
        public WriterCollection(IGroBufCustomSerializerCollection customSerializerCollection, Func<Type, IGroBufCustomSerializer> factory, Func<Type, IGroBufCustomSerializer> baseFactory)
        {
            this.customSerializerCollection = customSerializerCollection;
            this.factory = factory;
            this.baseFactory = baseFactory;
        }

        public IWriterBuilder GetWriterBuilder(Type type, bool ignoreCustomSerialization)
        {
            var key = new KeyValuePair<Type, bool>(type, ignoreCustomSerialization);
            var writerBuilder = (IWriterBuilder)writerBuilders[key];
            if(writerBuilder == null)
            {
                lock(writerBuildersLock)
                {
                    writerBuilder = (IWriterBuilder)writerBuilders[key];
                    if(writerBuilder == null)
                    {
                        writerBuilder = GetWriterBuilderInternal(type, ignoreCustomSerialization);
                        writerBuilders[key] = writerBuilder;
                    }
                }
            }
            return writerBuilder;
        }

        private IWriterBuilder GetWriterBuilderInternal(Type type, bool ignoreCustomSerialization)
        {
            IWriterBuilder writerBuilder;
            IGroBufCustomSerializer customSerializer = null;
            if(!ignoreCustomSerialization)
                customSerializer = customSerializerCollection.Get(type, factory, baseFactory(type));
            if(customSerializer != null)
                writerBuilder = new CustomWriterBuilder(type, customSerializer);
            else if(type == typeof(string))
                writerBuilder = new StringWriterBuilder();
            else if(type == typeof(DateTime))
                writerBuilder = new DateTimeWriterBuilder();
            else if(type == typeof(Guid))
                writerBuilder = new GuidWriterBuilder();
            else if(type == typeof(IPAddress))
                writerBuilder = new IPAddressWriterBuilder();
            else if(type == typeof(TimeSpan))
                writerBuilder = new TimeSpanWriterBuilder();
            else if(type == typeof(DateTimeOffset))
                writerBuilder = new DateTimeOffsetWriterBuilder();
            else if(type.IsEnum)
                writerBuilder = new EnumWriterBuilder(type);
            else if(type.IsPrimitive || type == typeof(decimal))
                writerBuilder = new PrimitivesWriterBuilder(type);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                writerBuilder = new NullableWriterBuilder(type);
            else if(type.IsArray)
                writerBuilder = type.GetElementType().IsPrimitive ? (IWriterBuilder)new PrimitivesArrayWriterBuilder(type) : new ArrayWriterBuilder(type);
            else if(type == typeof(Hashtable))
                writerBuilder = new HashtableWriterBuilder();
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                writerBuilder = new DictionaryWriterBuilder(type);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ArraySegment<>))
                writerBuilder = type.GetGenericArguments()[0].IsPrimitive ? (IWriterBuilder)new PrimitivesArraySegmentWriterBuilder(type) : new ArraySegmentWriterBuilder(type);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
                writerBuilder = type.GetGenericArguments()[0].IsPrimitive ? (IWriterBuilder)new PrimitivesHashSetWriterBuilder(type) : new HashSetWriterBuilder(type);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                writerBuilder = type.GetGenericArguments()[0].IsPrimitive ? (IWriterBuilder)new PrimitivesListWriterBuilder(type) : new ListWriterBuilder(type);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Lazy<>))
                writerBuilder = new LazyWriterBuilder(type);
            else if(type == typeof(object))
                writerBuilder = new ObjectWriterBuilder();
            else
                writerBuilder = new ClassWriterBuilder(type);
            return writerBuilder;
        }

        private readonly IGroBufCustomSerializerCollection customSerializerCollection;
        private readonly Func<Type, IGroBufCustomSerializer> factory;
        private readonly Func<Type, IGroBufCustomSerializer> baseFactory;

        private readonly Hashtable writerBuilders = new Hashtable();
        private readonly object writerBuildersLock = new object();
    }
}