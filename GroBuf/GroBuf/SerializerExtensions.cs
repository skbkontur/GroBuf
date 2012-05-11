namespace GroBuf
{
    public static class SerializerExtensions
    {
        public static byte[] Serialize<T1, T2>(this ISerializer serializer, T1 param1, T2 param2)
        {
            int size = serializer.GetSize(param1) + serializer.GetSize(param2);
            var result = new byte[size];
            int index = 0;
            serializer.Serialize(param1, result, ref index);
            serializer.Serialize(param2, result, ref index);
            return result;
        }

        public static byte[] Serialize<T1, T2, T3>(this ISerializer serializer, T1 param1, T2 param2, T3 param3)
        {
            int size = serializer.GetSize(param1) + serializer.GetSize(param2) + serializer.GetSize(param3);
            var result = new byte[size];
            int index = 0;
            serializer.Serialize(param1, result, ref index);
            serializer.Serialize(param2, result, ref index);
            serializer.Serialize(param3, result, ref index);
            return result;
        }

        public static byte[] Serialize<T1, T2, T3, T4>(this ISerializer serializer, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            int size = serializer.GetSize(param1) + serializer.GetSize(param2) + serializer.GetSize(param3) + serializer.GetSize(param4);
            var result = new byte[size];
            int index = 0;
            serializer.Serialize(param1, result, ref index);
            serializer.Serialize(param2, result, ref index);
            serializer.Serialize(param3, result, ref index);
            serializer.Serialize(param4, result, ref index);
            return result;
        }
    }
}