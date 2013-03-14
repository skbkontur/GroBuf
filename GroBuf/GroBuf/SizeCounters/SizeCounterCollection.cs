using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GroBuf.SizeCounters
{
    internal class SizeCounterCollection : ISizeCounterCollection
    {
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

        private static ISizeCounterBuilder GetSizeCounterBuilderInternal(Type type)
        {
            ISizeCounterBuilder sizeCounterBuilder;
            if(type.GetCustomAttributes(typeof(GroBufCustomSerializationAttribute), false).Any())
            {
                MethodInfo customSizeCounter = GroBufHelpers.GetMethod<GroBufSizeCounterAttribute>(type);
                if(customSizeCounter == null)
                    throw new MissingMethodException("Missing grobuf custom size counter for type '" + type + "'");
                sizeCounterBuilder = new CustomSizeCounterBuilder(type, customSizeCounter);
            }
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
            else if(type == typeof(object))
                sizeCounterBuilder = new ObjectSizeCounterBuilder();
            else
                sizeCounterBuilder = new ClassSizeCounterBuilder(type);
            return sizeCounterBuilder;
        }

        private readonly Hashtable sizeCounterBuilders = new Hashtable();
        private readonly object sizeCounterBuildersLock = new object();
    }
}