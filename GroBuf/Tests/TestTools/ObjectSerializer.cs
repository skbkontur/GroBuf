using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml;

namespace SKBKontur.GroBuf.Tests.TestTools
{
    public static class ObjectSerializer
    {
        public static string ObjectToString<T>(this T instance)
        {
            Type type = typeof(T);
            if (type.IsInterface)
                type = instance.GetType();
            var builder = new StringBuilder();
            using(XmlWriter writer = XmlWriter.Create(builder, new XmlWriterSettings {Indent = true, OmitXmlDeclaration = true}))
                new ObjectWriter(writer).Write(type, instance, "root");
            return builder.ToString();
        }

        private class ObjectWriter
        {
            public ObjectWriter(XmlWriter writer)
            {
                this.writer = writer;
            }

            public void Write(Type type, object value, string name)
            {
                writer.WriteStartElement(name);
                DoWrite(type, value);
                writer.WriteEndElement();
            }

            private void DoWrite(Type type, object value)
            {
                if(TryWriteNullValue(value)) return;
                if(TryWriteNullableTypeValue(type, value)) return;
                if(TryWriteSimpleTypeValue(type, value)) return;
                if(TryWriteArrayTypeValue(type, value)) return;
                WriteComplexTypeValue(type, value);
            }

            private void WriteComplexTypeValue(Type type, object value)
            {
                foreach(var fieldInfo in GetFields(type))
                    Write(fieldInfo.FieldType, fieldInfo.GetValue(value), FieldNameToTagName(fieldInfo.Name));
            }

            private static IEnumerable<FieldInfo> GetFields(Type type)
            {
                var result = new List<FieldInfo>();
                while(type != null)
                {
                    FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.Instance);
                    result.AddRange(fields.Where(fieldInfo => !fieldInfo.IsLiteral && !fieldInfo.FieldType.IsInterface));
                    type = type.BaseType;
                }
                return result;
            }

            private static string FieldNameToTagName(string name)
            {
                if(name.EndsWith(">k__BackingField"))
                    return name.Substring(1, name.IndexOf('>') - 1);
                return name;
            }

            private bool TryWriteArrayTypeValue(Type type, object value)
            {
                if(!type.IsArray)
                    return false;
                writer.WriteAttributeString("type", "array");
                var array = (Array)value;
                if(array.Rank > 1)
                    throw new NotSupportedException("array with rank > 1");
                Type elementType = type.GetElementType();
                for(int i = 0; i < array.Length; ++i)
                    Write(elementType, array.GetValue(i), "item");
                return true;
            }

            private bool TryWriteSimpleTypeValue(Type type, object value)
            {
                string result = FindSimpleValue(type, value);
                if(result == null)
                    return false;
                writer.WriteValue(result);
                return true;
            }

            private static string FindSimpleValue(Type type, object value)
            {
                if(type.IsEnum || type.IsPrimitive || value is string || value is Guid || value is IPEndPoint || value is decimal)
                    return value.ToString();
                if(value is DateTime)
                    return ((DateTime)value).Ticks.ToString();
                return null;
            }

            private bool TryWriteNullableTypeValue(Type type, object value)
            {
                if(!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Nullable<>))
                    return false;
                MethodInfo getMethodHasValue = type.GetProperty("HasValue").GetGetMethod();
                var hasValue = (bool)getMethodHasValue.Invoke(value, new object[0]);
                if(!hasValue)
                    WriteNull();
                else
                {
                    MethodInfo getMethodValue = type.GetProperty("Value").GetGetMethod();
                    object nullableValue = getMethodValue.Invoke(value, new object[0]);
                    DoWrite(type.GetGenericArguments()[0], nullableValue);
                }
                return true;
            }

            private bool TryWriteNullValue(object value)
            {
                if(value != null)
                    return false;
                WriteNull();
                return true;
            }

            private void WriteNull()
            {
                writer.WriteAttributeString("type", "null");
            }

            private readonly XmlWriter writer;
        }
    }
}