using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace GroBuf.Readers
{
    internal class ReaderCollection : IReaderCollection
    {
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

        private static IReaderBuilder GetReaderBuilderInternal(Type type)
        {
            IReaderBuilder readerBuilder;
            if (type.GetCustomAttributes(typeof(GroBufCustomSerializationAttribute), false).Any())
            {
                MethodInfo customReader = GroBufHelpers.GetMethod<GroBufReaderAttribute>(type);
                if (customReader == null)
                    throw new MissingMethodException("Missing grobuf custom reader for type '" + type + "'");
                readerBuilder = new CustomReaderBuilder(type, customReader);
            }
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
            else if(type == typeof(object))
                readerBuilder = new ObjectReaderBuilder();
            else
                readerBuilder = new ClassReaderBuilder(type);
            return readerBuilder;
        }

        private readonly Hashtable readerBuilders = new Hashtable();
        private readonly object readerBuildersLock = new object();
    }
}