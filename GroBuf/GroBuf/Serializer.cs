using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GroBuf.DataMembersExtracters;

namespace GroBuf
{
    public class Serializer : ISerializer
    {
        public Serializer()
        {
            impl = new SerializerImpl(new PropertiesExtracter());
        }

        public int GetSize<T>(T obj)
        {
            return impl.GetSize(obj);
        }

        public void Serialize<T>(T obj, byte[] result, ref int index)
        {
            impl.Serialize(obj, result, ref index);
        }

        public byte[] Serialize<T>(T obj)
        {
            return impl.Serialize(obj);
        }

        public T Deserialize<T>(byte[] data)
        {
            return impl.Deserialize<T>(data);
        }

        public T Deserialize<T>(byte[] data, ref int index)
        {
            return impl.Deserialize<T>(data, ref index);
        }

        public void Merge<T>(T from, ref T to)
        {
            impl.Merge(from, ref to);
        }

        public TTo ChangeType<TFrom, TTo>(TFrom obj)
        {
            return impl.ChangeType<TFrom, TTo>(obj);
        }

        public T Copy<T>(T obj)
        {
            return impl.Copy(obj);
        }

        #region Чужь от Федора

        public object Deserialize(Type type, byte[] data)
        {
            var deserialize = hashtableDeserializeFunction[type];
            if(deserialize == null)
            {
                lock(lockObject)
                {
                    deserialize = hashtableDeserializeFunction[type];
                    if(deserialize == null)
                    {
                        deserialize = MakeDeserializeFunction(type);
                        hashtableDeserializeFunction[type] = deserialize;
                    }
                }
            }
            return ((Func<byte[], object>)deserialize)(data);
        }

        public byte[] Serialize(Type type, object o)
        {
            var serialize = hashtableSerializeFunction[type];
            if(serialize == null)
            {
                lock(lockObject)
                {
                    serialize = hashtableSerializeFunction[type];
                    if(serialize == null)
                    {
                        serialize = MakeSerializeFunction(type);
                        hashtableSerializeFunction[type] = serialize;
                    }
                }
            }
            return ((Func<object, byte[]>)serialize)(o);
        }

        private Func<object, byte[]> MakeSerializeFunction(Type type)
        {
            var methodInfos = typeof(ISerializer).GetMethods().Where(info => info.Name == "Serialize" && Check(info)).Single().MakeGenericMethod(type);
            ParameterExpression parameter = Expression.Parameter(typeof(object), "obj");
            var call = Expression.Call(Expression.Constant(this), methodInfos, new[] {Expression.Convert(parameter, type)});
            var serializeFunc = Expression.Lambda<Func<object, byte[]>>(call, parameter).Compile();
            return serializeFunc;
        }

        private Func<byte[], object> MakeDeserializeFunction(Type type)
        {
            var methodInfos = typeof(ISerializer).GetMethods().Where(info => info.Name == "Deserialize" && Check(info)).Single().MakeGenericMethod(type);
            ParameterExpression parameter = Expression.Parameter(typeof(byte[]), "data");
            var call = Expression.Call(Expression.Constant(this), methodInfos, new[] {parameter});
            var deserializeFunc = Expression.Lambda<Func<byte[], object>>(call, parameter).Compile();
            return deserializeFunc;
        }

        private bool Check(MethodInfo methodInfo)
        {
            if(!methodInfo.IsGenericMethod)
                return false;
            var genericMethodDefinition = methodInfo.GetGenericMethodDefinition();
            return genericMethodDefinition.GetParameters().Length == 1;
        }

        private readonly object lockObject = new object();
        private readonly Hashtable hashtableSerializeFunction = new Hashtable();
        private readonly Hashtable hashtableDeserializeFunction = new Hashtable();

        #endregion

        private readonly SerializerImpl impl;
    }
}