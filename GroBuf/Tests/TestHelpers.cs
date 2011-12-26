using System;
using System.Reflection;

using SKBKontur.GroBuf.Tests.TestTools;

namespace SKBKontur.GroBuf.Tests
{
    public static class TestHelpers
    {
        private static string RandomString(Random random, int length, char first, char last)
        {
            var arr = new char[length];
            for (int i = 0; i < length; ++i)
                arr[i] = (char)random.Next(first, last + 1);
            return new string(arr);
        }

        static readonly ExtenderImpl extender = new ExtenderImpl();

        public static void Extend<T>(T obj)
        {
            extender.Extend(typeof(T), obj);
        }

        public static T GenerateRandomTrash<T>(Random random) where T : new()
        {
            var result = new T();
            FillWithRandomTrash(result, random);
            return result;
        }

        private static void FillWithRandomTrash(object obj, Random random)
        {
            Type type = obj.GetType();
            PropertyInfo[] properties = typePropertiesCache.Get(type);
            var bools = new bool[properties.Length];
            for (int i = 0; i < bools.Length; ++i)
                bools[i] = random.Next(91) > 60;
            for (int index = 0; index < properties.Length; index++)
            {
                PropertyInfo property = properties[index];
                Type propertyType = property.PropertyType;
                MethodInfo setter = property.GetSetMethod();
                if(!bools[index])
                {
                    if(!propertyType.IsArray)
                    {
                        if(propertyType == typeof(string))
                            setter.Invoke(obj, new[] {RandomString(random, 10, 'a', 'z')});
                        else
                        {
                            ConstructorInfo constructorInfo = typeConstructorCache.Get(propertyType);
                            object child = constructorInfo.Invoke(new object[0]);
                            setter.Invoke(obj, new[] {child});
                            FillWithRandomTrash(child, random);
                        }
                    }
                    else
                    {
                        Type elementType = propertyType.GetElementType();
                        int length = random.Next(1, 3);
                        Array array = Array.CreateInstance(elementType, length);
                        setter.Invoke(obj, new[] {array});
                        if(elementType == typeof(string))
                        {
                            for(int i = 0; i < length; ++i)
                                array.SetValue(RandomString(random, 10, 'a', 'z'), i);
                        }
                        else
                        {
                            ConstructorInfo constructorInfo = typeConstructorCache.Get(elementType);
                            for(int i = 0; i < length; ++i)
                                array.SetValue(constructorInfo.Invoke(new object[0]), i);
                            for(int i = 0; i < length; ++i)
                                FillWithRandomTrash(array.GetValue(i), random);
                        }
                    }
                }
            }
        }

        private static readonly ConcurrentCache<Type, PropertyInfo[]> typePropertiesCache = new ConcurrentCache<Type, PropertyInfo[]>(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public));
        private static readonly ConcurrentCache<Type, ConstructorInfo> typeConstructorCache = new ConcurrentCache<Type, ConstructorInfo>(type => type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null));
        
    }
}