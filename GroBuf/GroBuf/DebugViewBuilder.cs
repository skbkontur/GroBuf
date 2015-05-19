using System;
using System.Reflection.Emit;
using System.Text;

namespace GroBuf
{
    public class DebugViewBuilder
    {
        public static unsafe string DebugView(byte[] data)
        {
            if(data == null || data.Length == 0)
                throw new ArgumentNullException("data");
            var result = new StringBuilder();
            var debugViewBuilder = new DebugViewBuilder();
            fixed(byte* ptr = &data[0])
            {
                var index = 0;
                while(index < data.Length)
                {
                    debugViewBuilder.Print(ptr, ref index, data.Length, result);
                    result.AppendLine();
                }
            }
            return result.ToString();
        }

        private static string[] BuildMargins()
        {
            var result = new string[1024];
            result[0] = "";
            for(var i = 1; i < 1024; ++i)
                result[i] = new string(' ', i);
            return result;
        }

        private unsafe void Print(byte* data, ref int index, int length, StringBuilder result)
        {
            if(index >= length)
                throw new InvalidOperationException("Unexpected end of data");
            var typeCode = (GroBufTypeCode)data[index++];
            switch(typeCode)
            {
            case GroBufTypeCode.Empty:
                result.Append(margins[margin]);
                result.Append("<null>");
                break;
            case GroBufTypeCode.Object:
                {
                    if(index + 3 >= length)
                        throw new InvalidOperationException("Unexpected end of data");
                    var dataLength = *(int*)(data + index);
                    index += 4;
                    if(index + dataLength > length)
                        throw new InvalidOperationException("Unexpected end of data");
                    result.Append(margins[margin]);
                    result.AppendLine("<object>");
                    margin += 2;
                    var start = index;
                    while(index < start + dataLength)
                    {
                        result.Append(margins[margin]);
                        result.Append("<field:hash=");
                        result.Append(*(ulong*)(data + index));
                        result.AppendLine(">");
                        result.Append(margins[margin]);
                        result.AppendLine("<value>");
                        margin += 2;
                        index += 8;
                        Print(data, ref index, length, result);
                        margin -= 2;
                        result.Append(margins[margin]);
                        result.AppendLine("</value>");
                    }
                    margin -= 2;
                    result.Append(margins[margin]);
                    result.Append("</object>");
                }
                break;
            case GroBufTypeCode.Array:
                {
                    if(index + 3 >= length)
                        throw new InvalidOperationException("Unexpected end of data");
                    var dataLength = *(int*)(data + index);
                    index += 4;
                    if(index + dataLength > length)
                        throw new InvalidOperationException("Unexpected end of data");
                    var arrayLength = *(int*)(data + index);
                    index += 4;
                    result.Append(margins[margin]);
                    result.AppendLine("<array>");
                    margin += 2;
                    for(var i = 0; i < arrayLength; ++i)
                    {
                        result.Append(margins[margin]);
                        result.Append("<item:index=");
                        result.Append(i);
                        result.AppendLine(">");
                        result.Append(margins[margin]);
                        result.AppendLine("<value>");
                        margin += 2;
                        Print(data, ref index, length, result);
                        margin -= 2;
                        result.Append(margins[margin]);
                        result.AppendLine("</value>");
                    }
                    margin -= 2;
                    result.Append(margins[margin]);
                    result.Append("</array>");
                }
                break;
            case GroBufTypeCode.Boolean:
                if(index >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append(margins[margin]);
                result.Append("<bool:");
                result.Append(*(data + index) > 0);
                result.Append(">");
                ++index;
                break;
            case GroBufTypeCode.Int8:
                if(index >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append(margins[margin]);
                result.Append("<sbyte:");
                result.Append(*(sbyte*)(data + index));
                result.Append(">");
                ++index;
                break;
            case GroBufTypeCode.UInt8:
                if(index >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append(margins[margin]);
                result.Append("<byte:");
                result.Append(*(data + index));
                result.Append(">");
                ++index;
                break;
            case GroBufTypeCode.Int16:
                if(index + 1 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append(margins[margin]);
                result.Append("<short:");
                result.Append(*(short*)(data + index));
                result.Append(">");
                index += 2;
                break;
            case GroBufTypeCode.UInt16:
                if(index + 1 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append(margins[margin]);
                result.Append("<ushort:");
                result.Append(*(ushort*)(data + index));
                result.Append(">");
                index += 2;
                break;
            case GroBufTypeCode.Int32:
                if(index + 3 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append(margins[margin]);
                result.Append("<int:");
                result.Append(*(int*)(data + index));
                result.Append(">");
                index += 4;
                break;
            case GroBufTypeCode.UInt32:
                if(index + 3 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append(margins[margin]);
                result.Append("<uint:");
                result.Append(*(uint*)(data + index));
                result.Append(">");
                index += 4;
                break;
            case GroBufTypeCode.Int64:
                if(index + 7 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append(margins[margin]);
                result.Append("<long:");
                result.Append(*(long*)(data + index));
                result.Append(">");
                index += 8;
                break;
            case GroBufTypeCode.UInt64:
                if(index + 7 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append(margins[margin]);
                result.Append("<ulong:");
                result.Append(*(ulong*)(data + index));
                result.Append(">");
                index += 8;
                break;
            case GroBufTypeCode.Single:
                if(index + 3 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append(margins[margin]);
                result.Append("<float:");
                result.Append(*(float*)(data + index));
                result.Append(">");
                index += 4;
                break;
            case GroBufTypeCode.Double:
                if(index + 7 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append(margins[margin]);
                result.Append("<double:");
                result.Append(*(double*)(data + index));
                result.Append(">");
                index += 8;
                break;
            case GroBufTypeCode.Decimal:
                if(index + 15 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append(margins[margin]);
                result.Append("<decimal:");
                result.Append(*(decimal*)(data + index));
                result.Append(">");
                index += 16;
                break;
            case GroBufTypeCode.Guid:
                if(index + 15 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append(margins[margin]);
                result.Append("<Guid:");
                result.Append(*(Guid*)(data + index));
                result.Append(">");
                index += 16;
                break;
            case GroBufTypeCode.Enum:
                if(index + 7 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append(margins[margin]);
                result.Append("<enum:hash=");
                result.Append(*(ulong*)(data + index));
                result.Append(">");
                index += 8;
                break;
            case GroBufTypeCode.String:
                {
                    if(index + 3 >= length)
                        throw new InvalidOperationException("Unexpected end of data");
                    var dataLength = *(int*)(data + index);
                    index += 4;
                    if(index + dataLength > length)
                        throw new InvalidOperationException("Unexpected end of data");
                    var arr = new char[dataLength / 2];
                    fixed(char* dest = &arr[0])
                        memoryCopier((IntPtr)dest, (IntPtr)(data + index), dataLength);
                    result.Append(margins[margin]);
                    result.Append("<string:");
                    result.Append(new string(arr));
                    result.Append(">");
                    index += dataLength;
                }
                break;
            case GroBufTypeCode.DateTimeOld:
                throw new NotSupportedException("Old DateTime format is not supported");
            case GroBufTypeCode.DateTimeNew:
                if(index + 7 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append(margins[margin]);
                result.Append("<DateTime:");
                result.Append(DateTime.FromBinary(*(long*)(data + index)));
                result.Append(">");
                index += 8;
                break;
            case GroBufTypeCode.BooleanArray:
                {
                    if(index + 3 >= length)
                        throw new InvalidOperationException("Unexpected end of data");
                    var dataLength = *(int*)(data + index);
                    index += 4;
                    if(index + dataLength > length)
                        throw new InvalidOperationException("Unexpected end of data");
                    result.Append(margins[margin]);
                    result.Append("<bool[]:length=");
                    result.Append(dataLength);
                    result.Append(">");
                    index += dataLength;
                }
                break;
            case GroBufTypeCode.Int8Array:
                {
                    if(index + 3 >= length)
                        throw new InvalidOperationException("Unexpected end of data");
                    var dataLength = *(int*)(data + index);
                    index += 4;
                    if(index + dataLength > length)
                        throw new InvalidOperationException("Unexpected end of data");
                    result.Append(margins[margin]);
                    result.Append("<sbyte[]:length=");
                    result.Append(dataLength);
                    result.Append(">");
                    index += dataLength;
                }
                break;
            case GroBufTypeCode.UInt8Array:
                {
                    if(index + 3 >= length)
                        throw new InvalidOperationException("Unexpected end of data");
                    var dataLength = *(int*)(data + index);
                    index += 4;
                    if(index + dataLength > length)
                        throw new InvalidOperationException("Unexpected end of data");
                    result.Append(margins[margin]);
                    result.Append("<byte[]:length=");
                    result.Append(dataLength);
                    result.Append(">");
                    index += dataLength;
                }
                break;
            case GroBufTypeCode.Int16Array:
                {
                    if(index + 3 >= length)
                        throw new InvalidOperationException("Unexpected end of data");
                    var dataLength = *(int*)(data + index);
                    index += 4;
                    if(index + dataLength > length)
                        throw new InvalidOperationException("Unexpected end of data");
                    result.Append(margins[margin]);
                    result.Append("<short[]:length=");
                    result.Append(dataLength / 2);
                    result.Append(">");
                    index += dataLength;
                }
                break;
            case GroBufTypeCode.UInt16Array:
                {
                    if(index + 3 >= length)
                        throw new InvalidOperationException("Unexpected end of data");
                    var dataLength = *(int*)(data + index);
                    index += 4;
                    if(index + dataLength > length)
                        throw new InvalidOperationException("Unexpected end of data");
                    result.Append(margins[margin]);
                    result.Append("<ushort[]:length=");
                    result.Append(dataLength / 2);
                    result.Append(">");
                    index += dataLength;
                }
                break;
            case GroBufTypeCode.Int32Array:
                {
                    if(index + 3 >= length)
                        throw new InvalidOperationException("Unexpected end of data");
                    var dataLength = *(int*)(data + index);
                    index += 4;
                    if(index + dataLength > length)
                        throw new InvalidOperationException("Unexpected end of data");
                    result.Append(margins[margin]);
                    result.Append("<int[]:length=");
                    result.Append(dataLength / 4);
                    result.Append(">");
                    index += dataLength;
                }
                break;
            case GroBufTypeCode.UInt32Array:
                {
                    if(index + 3 >= length)
                        throw new InvalidOperationException("Unexpected end of data");
                    var dataLength = *(int*)(data + index);
                    index += 4;
                    if(index + dataLength > length)
                        throw new InvalidOperationException("Unexpected end of data");
                    result.Append(margins[margin]);
                    result.Append("<uint[]:length=");
                    result.Append(dataLength / 4);
                    result.Append(">");
                    index += dataLength;
                }
                break;
            case GroBufTypeCode.Int64Array:
                {
                    if(index + 3 >= length)
                        throw new InvalidOperationException("Unexpected end of data");
                    var dataLength = *(int*)(data + index);
                    index += 4;
                    if(index + dataLength > length)
                        throw new InvalidOperationException("Unexpected end of data");
                    result.Append(margins[margin]);
                    result.Append("<long[]:length=");
                    result.Append(dataLength / 8);
                    result.Append(">");
                    index += dataLength;
                }
                break;
            case GroBufTypeCode.UInt64Array:
                {
                    if(index + 3 >= length)
                        throw new InvalidOperationException("Unexpected end of data");
                    var dataLength = *(int*)(data + index);
                    index += 4;
                    if(index + dataLength > length)
                        throw new InvalidOperationException("Unexpected end of data");
                    result.Append(margins[margin]);
                    result.Append("<ulong[]:length=");
                    result.Append(dataLength / 8);
                    result.Append(">");
                    index += dataLength;
                }
                break;
            case GroBufTypeCode.SingleArray:
                {
                    if(index + 3 >= length)
                        throw new InvalidOperationException("Unexpected end of data");
                    var dataLength = *(int*)(data + index);
                    index += 4;
                    if(index + dataLength > length)
                        throw new InvalidOperationException("Unexpected end of data");
                    result.Append(margins[margin]);
                    result.Append("<float[]:length=");
                    result.Append(dataLength / 4);
                    result.Append(">");
                    index += dataLength;
                }
                break;
            case GroBufTypeCode.DoubleArray:
                {
                    if(index + 3 >= length)
                        throw new InvalidOperationException("Unexpected end of data");
                    var dataLength = *(int*)(data + index);
                    index += 4;
                    if(index + dataLength > length)
                        throw new InvalidOperationException("Unexpected end of data");
                    result.Append(margins[margin]);
                    result.Append("<double[]:length=");
                    result.Append(dataLength / 8);
                    result.Append(">");
                    index += dataLength;
                }
                break;
            case GroBufTypeCode.CustomData:
                {
                    if(index + 3 >= length)
                        throw new InvalidOperationException("Unexpected end of data");
                    var dataLength = *(int*)(data + index);
                    index += 4;
                    if(index + dataLength > length)
                        throw new InvalidOperationException("Unexpected end of data");
                    result.Append(margins[margin]);
                    result.Append("<custom data:length=");
                    result.Append(dataLength);
                    result.Append(">");
                    index += dataLength;
                }
                break;
            }
            result.AppendLine();
        }

        private static Action<IntPtr, IntPtr, int> BuildMemoryCopier()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(IntPtr), typeof(IntPtr), typeof(int)}, typeof(string), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Cpblk); // dest, source, number of bytes
            il.Emit(OpCodes.Ret);
            return (Action<IntPtr, IntPtr, int>)method.CreateDelegate(typeof(Action<IntPtr, IntPtr, int>));
        }

        private int margin;

        private static readonly string[] margins = BuildMargins();

        private static readonly Action<IntPtr, IntPtr, int> memoryCopier = BuildMemoryCopier();
    }
}