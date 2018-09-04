using System;
using System.Reflection;

namespace GroBuf
{
    public static class PlatformHelpers
    {
        static PlatformHelpers()
        {
            IsMono = Type.GetType("Mono.Runtime") != null;
        }

        public static bool IsMono { get; }

        public static string DelegateTargetFieldName => IsMono ? "m_target" : "_target";
        public static string[] LazyValueFactoryFieldNames => new[] {"m_valueFactory", "_factory"};
        public static string[] DateTimeOffsetDateTimeFieldNames => new[] {"m_dateTime", "_dateTime"};
        public static string[] DateTimeOffsetOffsetMinutesFieldNames => new[] {"m_offsetMinutes", "_offsetMinutes"};
        public static string[] HashtableCountFieldNames => new[] {"count", "_count"};
        public static string[] HashtableBucketsFieldNames => new[] {"buckets", "_buckets"};
        public static string[] HashSetSlotsFieldNames => new[] {"m_slots", "_slots"};
        public static string[] HashSetLastIndexFieldNames => new[] {"m_lastIndex", "_lastIndex"};
        public static string[] DictionaryCountFieldNames => new[] {"count", "_count"};
        public static string[] DictionaryEntriesFieldNames => new[] {"entries", "_entries"};

        public static FieldInfo GetPrivateInstanceField(this Type type, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                var fieldInfo = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfo != null)
                    return fieldInfo;
            }
            throw new InvalidOperationException($"Failed to get filedInfo for type {type} with name candidates: {string.Join(", ", fieldNames)}");
        }
    }
}