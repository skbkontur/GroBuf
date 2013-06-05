using System;
using System.Collections;
using System.Collections.Generic;

namespace GroBuf.SizeCounters
{
    internal class SizeCounterCollection : ISizeCounterCollection
    {
        public SizeCounterCollection(IGroBufCustomSerializerCollection customSerializerCollection, Func<Type, IGroBufCustomSerializer> factory)
        {
            this.customSerializerCollection = customSerializerCollection;
            this.factory = factory;
        }

        public ISizeCounterBuilder GetSizeCounterBuilder(Type type)
        {
            var sizeCounterBuilder = (ISizeCounterBuilder)sizeCounterBuilders[type];
            if(sizeCounterBuilder == null)
            {
                lock(sizeCounterBuildersLock)
                {
                    sizeCounterBuilder = (ISizeCounterBuilder)sizeCounterBuilders[type];
                    if(sizeCounterBuilder == null)
                    {
                        sizeCounterBuilder = GetSizeCounterBuilderInternal(type);
                        sizeCounterBuilders[type] = sizeCounterBuilder;
                    }
                }
            }
            return sizeCounterBuilder;
        }

        private ISizeCounterBuilder GetSizeCounterBuilderInternal(Type type)
        {
            ISizeCounterBuilder sizeCounterBuilder;
            var customSerializer = customSerializerCollection.Get(type, factory);
            if(customSerializer != null)
                sizeCounterBuilder = new CustomSizeCounterBuilder(type, customSerializer);
            else if(type == typeof(string))
                sizeCounterBuilder = new StringSizeCounterBuilder();
            else if(type == typeof(DateTime))
                sizeCounterBuilder = new DateTimeSizeCounterBuilder();
            else if(type == typeof(Guid))
                sizeCounterBuilder = new GuidSizeCounterBuilder();
            else if(type.IsEnum)
                sizeCounterBuilder = new EnumSizeCounterBuilder(type);
            else if(type.IsPrimitive || type == typeof(decimal))
                sizeCounterBuilder = new PrimitivesSizeCounterBuilder(type);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                sizeCounterBuilder = new NullableSizeCounterBuilder(type);
            else if(type.IsArray)
                sizeCounterBuilder = type.GetElementType().IsPrimitive ? (ISizeCounterBuilder)new PrimitivesArraySizeCounterBuilder(type) : new ArraySizeCounterBuilder(type);
            else if(type == typeof(Array))
                sizeCounterBuilder = new ArraySizeCounterBuilder(type);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                sizeCounterBuilder = new DictionarySizeCounterBuilder(type);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
                sizeCounterBuilder = type.GetGenericArguments()[0].IsPrimitive ? (ISizeCounterBuilder)new PrimitivesHashSetSizeCounterBuilder(type) : new HashSetSizeCounterBuilder(type);
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                sizeCounterBuilder = type.GetGenericArguments()[0].IsPrimitive ? (ISizeCounterBuilder)new PrimitivesListSizeCounterBuilder(type) : new ListSizeCounterBuilder(type);
            else if(type == typeof(object))
                sizeCounterBuilder = new ObjectSizeCounterBuilder();
            else
                sizeCounterBuilder = new ClassSizeCounterBuilder(type);
            return sizeCounterBuilder;
        }

        private readonly IGroBufCustomSerializerCollection customSerializerCollection;
        private readonly Func<Type, IGroBufCustomSerializer> factory;

        private readonly Hashtable sizeCounterBuilders = new Hashtable();
        private readonly object sizeCounterBuildersLock = new object();
    }
}