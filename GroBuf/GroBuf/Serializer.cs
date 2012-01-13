﻿using System.Runtime.InteropServices;

using GroBuf.DataMembersExtracters;

namespace GroBuf
{
    public class Serializer : ISerializer
    {
        public Serializer()
            : this(new PropertiesExtracter())
        {
        }

        public Serializer(IDataMembersExtracter dataMembersExtracter)
        {
            writer = new GroBufWriter(dataMembersExtracter);
            reader = new GroBufReader(dataMembersExtracter);
        }

        public int GetSize<T>(T obj)
        {
            return writer.GetSize(obj);
        }

        public void Serialize<T>(T obj, byte[] result, int index)
        {
            writer.Write(obj, result, index);
        }

        public byte[] Serialize<T>(T obj)
        {
            return writer.Write(obj);
        }

        public T Deserialize<T>(byte[] data)
        {
            return reader.Read<T>(data);
        }

        public TTo ChangeType<TFrom, TTo>(TFrom obj)
        {
            var size = writer.GetSize(obj);
            if(size <= 768)
            {
                var buf = new byte[size];
                writer.Write(obj, buf, 0);
                return reader.Read<TTo>(buf);
            }
            else
            {
                var buf = Marshal.AllocHGlobal(size);
                writer.Write(obj, buf);
                var result = reader.Read<TTo>(buf, size);
                Marshal.FreeHGlobal(buf);
                return result;
            }
        }

        private readonly GroBufWriter writer;
        private readonly GroBufReader reader;
    }
}