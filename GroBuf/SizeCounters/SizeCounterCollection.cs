using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace GroBuf.SizeCounters
{
    internal class SizeCounterCollection : ISizeCounterCollection
    {
        public SizeCounterCollection(IGroBufCustomSerializerCollection customSerializerCollection, Func<Type, IGroBufCustomSerializer> factory, Func<Type, IGroBufCustomSerializer> baseFactory)
        {
            this.customSerializerCollection = customSerializerCollection;
            this.factory = factory;
            this.baseFactory = baseFactory;
        }

        public ISizeCounterBuilder GetSizeCounterBuilder(Type type, bool ignoreCustomSerialization)
        {
            var key = new KeyValuePair<Type, bool>(type, ignoreCustomSerialization);
            var sizeCounterBuilder = (ISizeCounterBuilder)sizeCounterBuilders[key];
            if (sizeCounterBuilder == null)
            {
                lock (sizeCounterBuildersLock)
                {
                    sizeCounterBuilder = (ISizeCounterBuilder)sizeCounterBuilders[key];
                    if (sizeCounterBuilder == null)
                    {
                        sizeCounterBuilder = GetSizeCounterBuilderInternal(type, ignoreCustomSerialization);
                        sizeCounterBuilders[key] = sizeCounterBuilder;
                    }
                }
            }
            return sizeCounterBuilder;
        }

        private ISizeCounterBuilder GetSizeCounterBuilderInternal(Type type, bool ignoreCustomSerialization)
        {
            ISizeCounterBuilder sizeCounterBuilder;
            IGroBufCustomSerializer customSerializer = null;
            if (!ignoreCustomSerialization)
                customSerializer = customSerializerCollection.Get(type, factory, baseFactory(type));
            if (customSerializer != null)
                sizeCounterBuilder = new CustomSizeCounterBuilder(type, customSerializer);
            else if (type == typeof(string))
                sizeCounterBuilder = new StringSizeCounterBuilder();
            else if (type == typeof(DateTime))
                sizeCounterBuilder = new DateTimeSizeCounterBuilder();
            else if (type == typeof(Guid))
                sizeCounterBuilder = new GuidSizeCounterBuilder();
            else if (type == typeof(IPAddress))
                sizeCounterBuilder = new IPAddressSizeCounterBuilder();
            else if (type == typeof(TimeSpan))
                sizeCounterBuilder = new TimeSpanSizeCounterBuilder();
            else if (type == typeof(DateTimeOffset))
                sizeCounterBuilder = new DateTimeOffsetSizeCounterBuilder();
            else if (type.IsEnum)
                sizeCounterBuilder = new EnumSizeCounterBuilder(type);
            else if (type.IsPrimitive || type == typeof(decimal))
                sizeCounterBuilder = new PrimitivesSizeCounterBuilder(type);
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                sizeCounterBuilder = new NullableSizeCounterBuilder(type);
            else if (type.IsArray)
                sizeCounterBuilder = type.GetElementType().IsPrimitive ? (ISizeCounterBuilder)new PrimitivesArraySizeCounterBuilder(type) : new ArraySizeCounterBuilder(type);
            else if (type == typeof(Hashtable))
                sizeCounterBuilder = new HashtableSizeCounterBuilder();
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                sizeCounterBuilder = new DictionarySizeCounterBuilder(type);
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ArraySegment<>))
                sizeCounterBuilder = type.GetGenericArguments()[0].IsPrimitive ? (ISizeCounterBuilder)new PrimitivesArraySegmentSizeCounterBuilder(type) : new ArraySegmentSizeCounterBuilder(type);
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
                sizeCounterBuilder = type.GetGenericArguments()[0].IsPrimitive ? (ISizeCounterBuilder)new PrimitivesHashSetSizeCounterBuilder(type) : new HashSetSizeCounterBuilder(type);
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                sizeCounterBuilder = type.GetGenericArguments()[0].IsPrimitive ? (ISizeCounterBuilder)new PrimitivesListSizeCounterBuilder(type) : new ListSizeCounterBuilder(type);
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Lazy<>))
                sizeCounterBuilder = new LazySizeCounterBuilder(type);
            else if (type.IsTuple())
                sizeCounterBuilder = new TupleSizeCounterBuilder(type);
            else if (type == typeof(object))
                sizeCounterBuilder = new ObjectSizeCounterBuilder();
            else
                sizeCounterBuilder = new ClassSizeCounterBuilder(type);
            return sizeCounterBuilder;
        }

        private readonly IGroBufCustomSerializerCollection customSerializerCollection;
        private readonly Func<Type, IGroBufCustomSerializer> factory;
        private readonly Func<Type, IGroBufCustomSerializer> baseFactory;

        private readonly Hashtable sizeCounterBuilders = new Hashtable();
        private readonly object sizeCounterBuildersLock = new object();
    }
}