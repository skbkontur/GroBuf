using System;
using System.Collections;
using System.Collections.Generic;

namespace GroBuf.Readers
{
    internal class ReaderCollection : IReaderCollection
    {
        public ReaderCollection(IGroBufCustomSerializerCollection customSerializerCollection, Func<Type, IGroBufCustomSerializer> factory)
        {
            this.customSerializerCollection = customSerializerCollection;
            this.factory = factory;
        }

        public IReaderBuilder GetReaderBuilder(Type type)
        {
            var readerBuilder = (IReaderBuilder)readerBuilders[type];
            if(readerBuilder == null)
            {
                lock(readerBuildersLock)
                {
                    readerBuilder = (IReaderBuilder)readerBuilders[type];
                    if(readerBuilder == null)
                    {
                        readerBuilder = GetReaderBuilderInternal(type);
                        readerBuilders[type] = readerBuilder;
                    }
                }
            }
            return readerBuilder;
        }

        private IReaderBuilder GetReaderBuilderInternal(Type type)
        {
            IReaderBuilder readerBuilder;
            var customSerializer = customSerializerCollection.Get(type, factory);
            if(customSerializer != null)
                readerBuilder = new CustomReaderBuilder(type, customSerializer);
            else if(type == typeof(string))
                readerBuilder = new StringReaderBuilder();
            else if(type == typeof(DateTime))
                readerBuilder = new DateTimeReaderBuilder();
            else if(type == typeof(Guid))
                readerBuilder = new GuidReaderBuilder();
            else if(type.IsEnum)
                readerBuilder = new EnumReaderBuilder(type);
            else if(type.IsPrimitive || type == typeof(decimal))
                readerBuilder = new PrimitivesReaderBuilder(type);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                readerBuilder = new NullableReaderBuilder(type);
            else if(type.IsArray)
                readerBuilder = type.GetElementType().IsPrimitive ? (IReaderBuilder)new PrimitivesArrayReaderBuilder(type) : new ArrayReaderBuilder(type);
            else if(type == typeof(Array))
                readerBuilder = new ArrayReaderBuilder(type);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                readerBuilder = new DictionaryReaderBuilder(type);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
                readerBuilder = type.GetGenericArguments()[0].IsPrimitive ? (IReaderBuilder)new PrimitivesHashSetReaderBuilder(type) : new HashSetReaderBuilder(type);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                readerBuilder = type.GetGenericArguments()[0].IsPrimitive ? (IReaderBuilder)new PrimitivesListReaderBuilder(type) : new ListReaderBuilder(type);
            else if(type == typeof(object))
                readerBuilder = new ObjectReaderBuilder();
            else
                readerBuilder = new ClassReaderBuilder(type);
            return readerBuilder;
        }

        private readonly IGroBufCustomSerializerCollection customSerializerCollection;
        private readonly Func<Type, IGroBufCustomSerializer> factory;

        private readonly Hashtable readerBuilders = new Hashtable();
        private readonly object readerBuildersLock = new object();
    }
}