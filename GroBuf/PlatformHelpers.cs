using System;

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
        public static string LazyValueFactoryFieldName => SelectName("_factory", "m_valueFactory");
        public static string DateTimeOffsetDateTimeFieldName => SelectName("_dateTime", "m_dateTime");
        public static string DateTimeOffsetOffsetMinutesFieldName => SelectName("_offsetMinutes", "m_offsetMinutes");
        public static string HashtableCountFieldName => SelectName("_count", "count");
        public static string HashtableBucketsFieldName => SelectName("_buckets", "buckets");
        public static string HashSetSlotsFieldName => SelectName("_slots", "m_slots");
        public static string HashSetLastIndexFieldName => SelectName("_lastIndex", "m_lastIndex");

        private static string SelectName(string netcoreName, string net45Name)
        {
#if NETSTANDARD2_0
            return netcoreName;
#else
            return net45Name;
#endif
        }
    }
}