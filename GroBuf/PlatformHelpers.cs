using System;
using System.Reflection;

namespace GroBuf
{
    public static class PlatformHelpers
    {
        static PlatformHelpers()
        {
            IsMono = HasSystemType("Mono.Runtime");
            IsDotNetCore30OrGreater = HasSystemType("System.Range");
            IsDotNet50OrGreater = HasSystemType("System.Half");
        }

        public static bool IsMono { get; }
        public static bool IsDotNetCore30OrGreater { get; }
        public static bool IsDotNet50OrGreater { get; }

        public static string DelegateTargetFieldName => IsMono ? "m_target" : "_target";
        public static string[] LazyValueFactoryFieldNames => new[] {"m_valueFactory", "_factory"};
        public static string[] DateTimeOffsetDateTimeFieldNames => new[] {"m_dateTime", "_dateTime"};
        public static string[] DateTimeOffsetOffsetMinutesFieldNames => new[] {"m_offsetMinutes", "_offsetMinutes"};
        public static string[] HashtableCountFieldNames => new[] {"count", "_count"};
        public static string[] HashtableBucketsFieldNames => new[] {"buckets", "_buckets"};
        public static string[] HashSetCountFieldNames => IsDotNet50OrGreater ? new[] {"_count"} : new[] {"m_lastIndex", "_lastIndex"};
        public static string[] HashSetSlotsFieldNames => IsDotNet50OrGreater ? new[] {"_entries"} : new[] {"m_slots", "_slots"};
        public static string[] DictionaryCountFieldNames => new[] {"count", "_count"};
        public static string[] DictionaryEntriesFieldNames => new[] {"entries", "_entries"};

        public static string HashSetSlotTypeName => IsDotNet50OrGreater ? "Entry" : "Slot";
        public static string HashSetSlotValueFieldName => IsDotNet50OrGreater ? "Value" : "value";

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

        private static bool HasSystemType(string typeName)
        {
            try
            {
                return Type.GetType(typeName) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}