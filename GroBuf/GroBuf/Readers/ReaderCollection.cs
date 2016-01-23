using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection.Emit;
using System.Runtime.Serialization.Configuration;

namespace GroBuf.Readers
{
    internal class ReaderCollection : IReaderCollection
    {
        public ReaderCollection(IGroBufCustomSerializerCollection customSerializerCollection, Func<Type, IGroBufCustomSerializer> factory, Func<Type, IGroBufCustomSerializer> baseFactory, ModuleBuilder module)
        {
            this.customSerializerCollection = customSerializerCollection;
            this.factory = factory;
            this.baseFactory = baseFactory;
            this.module = module;
        }

        public IReaderBuilder GetReaderBuilder(Type type, bool ignoreCustomSerialization)
        {
            var key = new KeyValuePair<Type, bool>(type, ignoreCustomSerialization);
            var readerBuilder = (IReaderBuilder)readerBuilders[key];
            if(readerBuilder == null)
            {
                lock(readerBuildersLock)
                {
                    readerBuilder = (IReaderBuilder)readerBuilders[key];
                    if(readerBuilder == null)
                    {
                        readerBuilder = GetReaderBuilderInternal(type, ignoreCustomSerialization);
                        readerBuilders[key] = readerBuilder;
                    }
                }
            }
            return readerBuilder;
        }

        private IReaderBuilder GetReaderBuilderInternal(Type type, bool ignoreCustomSerialization)
        {
            IReaderBuilder readerBuilder;
            IGroBufCustomSerializer customSerializer = null;
            if(!ignoreCustomSerialization)
                customSerializer = customSerializerCollection.Get(type, factory, baseFactory(type));
            if(customSerializer != null)
                readerBuilder = new CustomReaderBuilder(type, customSerializer);
            else if(type == typeof(string))
                readerBuilder = new StringReaderBuilder();
            else if(type == typeof(DateTime))
                readerBuilder = new DateTimeReaderBuilder();
            else if(type == typeof(Guid))
                readerBuilder = new GuidReaderBuilder();
            else if(type == typeof(IPAddress))
                readerBuilder = new IPAddressReaderBuilder();
            else if(type == typeof(TimeSpan))
                readerBuilder = new TimeSpanReaderBuilder();
            else if(type == typeof(DateTimeOffset))
                readerBuilder = new DateTimeOffsetReaderBuilder();
            else if(type.IsEnum)
                readerBuilder = new EnumReaderBuilder(type);
            else if(type.IsPrimitive || type == typeof(decimal))
                readerBuilder = new PrimitivesReaderBuilder(type, module);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                readerBuilder = new NullableReaderBuilder(type);
            else if(type.IsArray)
                readerBuilder = type.GetElementType().IsPrimitive ? (IReaderBuilder)new PrimitivesArrayReaderBuilder(type) : new ArrayReaderBuilder(type);
            else if(type == typeof(Hashtable))
                readerBuilder = new HashtableReaderBuilder();
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                readerBuilder = new DictionaryReaderBuilder(type);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
                readerBuilder = type.GetGenericArguments()[0].IsPrimitive ? (IReaderBuilder)new PrimitivesHashSetReaderBuilder(type) : new HashSetReaderBuilder(type);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                readerBuilder = type.GetGenericArguments()[0].IsPrimitive ? (IReaderBuilder)new PrimitivesListReaderBuilder(type) : new ListReaderBuilder(type);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Lazy<>))
                readerBuilder = new LazyReaderBuilder(type, module);
            else if(type == typeof(object))
                readerBuilder = new ObjectReaderBuilder();
            else
                readerBuilder = new ClassReaderBuilder(type);
            return readerBuilder;
        }

        private readonly IGroBufCustomSerializerCollection customSerializerCollection;
        private readonly Func<Type, IGroBufCustomSerializer> factory;
        private readonly Func<Type, IGroBufCustomSerializer> baseFactory;
        private readonly ModuleBuilder module;

        private readonly Hashtable readerBuilders = new Hashtable();
        private readonly object readerBuildersLock = new object();
    }
}