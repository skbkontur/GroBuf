using System;
using System.Runtime.InteropServices;

using GroBuf.DataMembersExtracters;

namespace GroBuf
{
    internal class InternalSerializer : IGroBufCustomSerializer
    {
        public InternalSerializer(GroBufWriter groBufWriter, GroBufReader groBufReader, Type type, bool ignoreCustomSerializer)
        {
            this.groBufWriter = groBufWriter;
            this.groBufReader = groBufReader;
            this.type = type;
            this.ignoreCustomSerializer = ignoreCustomSerializer;
        }

        public int CountSize(object obj, bool writeEmpty)
        {
            return groBufWriter.GetSize(type, ignoreCustomSerializer, obj, writeEmpty);
        }

        public void Write(object obj, bool writeEmpty, IntPtr result, ref int index)
        {
            groBufWriter.Write(type, ignoreCustomSerializer, obj, writeEmpty, result, ref index);
        }

        public void Read(IntPtr data, ref int index, int length, ref object result)
        {
            groBufReader.Read(type, ignoreCustomSerializer, data, ref index, length, ref result);
        }

        private readonly GroBufWriter groBufWriter;
        private readonly GroBufReader groBufReader;
        private readonly Type type;
        private readonly bool ignoreCustomSerializer;
    }

    public class SerializerImpl
    {
        public SerializerImpl(IDataMembersExtractor dataMembersExtractor, IGroBufCustomSerializerCollection customSerializerCollection = null, GroBufOptions options = GroBufOptions.None)
        {
            customSerializerCollection = customSerializerCollection ?? new DefaultGroBufCustomSerializerCollection();
            Func<Type, IGroBufCustomSerializer> factory = type => new InternalSerializer(writer, reader, type, false);
            Func<Type, IGroBufCustomSerializer> baseFactory = type => new InternalSerializer(writer, reader, type, true);
            writer = new GroBufWriter(dataMembersExtractor, customSerializerCollection, options, factory, baseFactory);
            reader = new GroBufReader(dataMembersExtractor, customSerializerCollection, factory, baseFactory);
        }

        public int GetSize<T>(T obj)
        {
            return writer.GetSize(obj);
        }

        public void Serialize<T>(T obj, byte[] result, ref int index)
        {
            writer.Write(obj, result, ref index);
        }

        public byte[] Serialize<T>(T obj)
        {
            return writer.Write(obj);
        }

        public T Deserialize<T>(byte[] data)
        {
            return reader.Read<T>(data);
        }

        public T Deserialize<T>(byte[] data, ref int index)
        {
            return reader.Read<T>(data, ref index);
        }

        public T Deserialize<T>(byte[] data, int length)
        {
            return reader.Read<T>(data, length);
        }

        public T Deserialize<T>(byte[] data, ref int index, int length)
        {
            return reader.Read<T>(data, ref index, length);
        }

        public void Deserialize<T>(byte[] data, ref T result)
        {
            reader.Read(data, ref result);
        }

        public int GetSize(Type type, object obj)
        {
            return writer.GetSize(type, obj);
        }

        public void Serialize(Type type, object obj, byte[] result, ref int index)
        {
            writer.Write(type, obj, result, ref index);
        }

        public byte[] Serialize(Type type, object obj)
        {
            return writer.Write(type, obj);
        }

        public object Deserialize(Type type, byte[] data)
        {
            return reader.Read(type, data);
        }

        public object Deserialize(Type type, byte[] data, ref int index)
        {
            return reader.Read(type, data, ref index);
        }

        public object Deserialize(Type type, byte[] data, int length)
        {
            return reader.Read(type, data, length);
        }

        public object Deserialize(Type type, byte[] data, ref int index, int length)
        {
            return reader.Read(type, data, ref index, length);
        }

        public void Merge<T>(T from, ref T to)
        {
            ChangeType(from, ref to);
        }

        public TTo ChangeType<TFrom, TTo>(TFrom obj)
        {
            TTo result = default(TTo);
            ChangeType(obj, ref result);
            return result;
        }

        public T Copy<T>(T obj)
        {
            return ChangeType<T, T>(obj);
        }

        public object ChangeType(Type from, Type to, object obj)
        {
            object result = null;
            ChangeType(from, to, obj, ref result);
            return result;
        }

        public object Copy(Type type, object obj)
        {
            return ChangeType(type, type, obj);
        }

        private void ChangeType<TFrom, TTo>(TFrom obj, ref TTo result)
        {
            var size = writer.GetSize(obj);
            if(size <= 768)
            {
                var buf = new byte[size];
                int index = 0;
                writer.Write(obj, buf, ref index);
                reader.Read(buf, ref result);
            }
            else
            {
                var buf = Marshal.AllocHGlobal(size);
                try
                {
                    writer.Write(obj, buf);
                    int index = 0;
                    reader.Read(buf, ref index, size, ref result);
                }
                finally
                {
                    Marshal.FreeHGlobal(buf);
                }
            }
        }

        private void ChangeType(Type from, Type to, object obj, ref object result)
        {
            var size = writer.GetSize(from, obj);
            if(size <= 768)
            {
                var buf = new byte[size];
                int index = 0;
                writer.Write(from, obj, buf, ref index);
                reader.Read(to, buf, ref result);
            }
            else
            {
                var buf = Marshal.AllocHGlobal(size);
                try
                {
                    writer.Write(from, obj, buf);
                    int index = 0;
                    reader.Read(to, buf, ref index, size, ref result);
                }
                finally
                {
                    Marshal.FreeHGlobal(buf);
                }
            }
        }

        private readonly GroBufWriter writer;
        private readonly GroBufReader reader;
    }
}