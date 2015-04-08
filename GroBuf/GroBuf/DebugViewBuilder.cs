using System;
using System.Reflection.Emit;
using System.Text;

namespace GroBuf
{
    internal class DebugViewBuilder
    {
        private int margin;

        public unsafe void Print(byte* data, ref int index, int length, StringBuilder result)
        {
            if(index >= length)
                throw new InvalidOperationException("Unexpected end of data");
            var typeCode = (GroBufTypeCode)data[index++];
            switch(typeCode)
            {
            case GroBufTypeCode.Empty:
                result.Append("<null>");
                break;
            case GroBufTypeCode.Object:
                {
                    result.AppendLine("<object>");

                }
            case GroBufTypeCode.Boolean:
                if(index >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append("<bool:");
                result.Append(*(data + index) > 0);
                result.Append(">");
                ++index;
                break;
            case GroBufTypeCode.Int8:
                if(index >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append("<sbyte:");
                result.Append(*(sbyte*)(data + index));
                result.Append(">");
                ++index;
                break;
            case GroBufTypeCode.UInt8:
                if(index >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append("<byte:");
                result.Append(*(data + index));
                result.Append(">");
                ++index;
                break;
            case GroBufTypeCode.Int16:
                if(index + 1 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append("<short:");
                result.Append(*(short*)(data + index));
                result.Append(">");
                index += 2;
                break;
            case GroBufTypeCode.UInt16:
                if(index + 1 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append("<ushort:");
                result.Append(*(ushort*)(data + index));
                result.Append(">");
                index += 2;
                break;
            case GroBufTypeCode.Int32:
                if(index + 3 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append("<int:");
                result.Append(*(int*)(data + index));
                result.Append(">");
                index += 4;
                break;
            case GroBufTypeCode.UInt32:
                if(index + 3 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append("<uint:");
                result.Append(*(uint*)(data + index));
                result.Append(">");
                index += 4;
                break;
            case GroBufTypeCode.Int64:
                if(index + 7 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append("<long:");
                result.Append(*(long*)(data + index));
                result.Append(">");
                index += 8;
                break;
            case GroBufTypeCode.UInt64:
                if(index + 7 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append("<ulong:");
                result.Append(*(ulong*)(data + index));
                result.Append(">");
                index += 8;
                break;
            case GroBufTypeCode.Single:
                if(index + 3 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append("<float:");
                result.Append(*(float*)(data + index));
                result.Append(">");
                index += 4;
                break;
            case GroBufTypeCode.Double:
                if(index + 7 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append("<double:");
                result.Append(*(double*)(data + index));
                result.Append(">");
                index += 8;
                break;
            case GroBufTypeCode.Decimal:
                if(index + 15 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append("<decimal:");
                result.Append(*(decimal*)(data + index));
                result.Append(">");
                index += 16;
                break;
            case GroBufTypeCode.Guid:
                if(index + 15 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
                result.Append("<Guid:");
                result.Append(*(Guid*)(data + index));
                result.Append(">");
                index += 16;
                break;
            case GroBufTypeCode.Enum:
                if(index + 7 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
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
                    if(index + dataLength >= length)
                        throw new InvalidOperationException("Unexpected end of data");
                    var arr = new char[dataLength / 2];
                    fixed(char* dest = &arr[0])
                        memoryCopier((IntPtr)dest, (IntPtr)(data + index), dataLength);
                    result.Append("<string:");
                    result.Append(new string(arr));
                    result.Append(">");
                    index += dataLength;
                }
                break;
            case GroBufTypeCode.DateTimeOld:
                throw new NotSupportedException("Old DateTime format is not supported");
            case GroBufTypeCode.DateTimeNew:
                if (index + 7 >= length)
                    throw new InvalidOperationException("Unexpected end of data");
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
                    if(index + dataLength >= length)
                        throw new InvalidOperationException("Unexpected end of data");
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
                    if(index + dataLength >= length)
                        throw new InvalidOperationException("Unexpected end of data");
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
                    if(index + dataLength >= length)
                        throw new InvalidOperationException("Unexpected end of data");
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
                    if(index + dataLength >= length)
                        throw new InvalidOperationException("Unexpected end of data");
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
                    if(index + dataLength >= length)
                        throw new InvalidOperationException("Unexpected end of data");
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
                    if(index + dataLength >= length)
                        throw new InvalidOperationException("Unexpected end of data");
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
                    if(index + dataLength >= length)
                        throw new InvalidOperationException("Unexpected end of data");
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
                    if(index + dataLength >= length)
                        throw new InvalidOperationException("Unexpected end of data");
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
                    if(index + dataLength >= length)
                        throw new InvalidOperationException("Unexpected end of data");
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
                    if(index + dataLength >= length)
                        throw new InvalidOperationException("Unexpected end of data");
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
                    if(index + dataLength >= length)
                        throw new InvalidOperationException("Unexpected end of data");
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
                    if(index + dataLength >= length)
                        throw new InvalidOperationException("Unexpected end of data");
                    result.Append("<custom data:length=");
                    result.Append(dataLength);
                    result.Append(">");
                    index += dataLength;
                }
                break;
            }
        }

        private static Action<IntPtr, IntPtr, int> BuildMemoryCopier()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(IntPtr), typeof(IntPtr), typeof(int)}, typeof(string), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Unaligned);
            il.Emit(OpCodes.Cpblk); // dest, source, number of bytes
            il.Emit(OpCodes.Ret);
            return (Action<IntPtr, IntPtr, int>)method.CreateDelegate(typeof(Action<IntPtr, IntPtr, int>));
        }

        private static readonly Action<IntPtr, IntPtr, int> memoryCopier = BuildMemoryCopier();
    }
}