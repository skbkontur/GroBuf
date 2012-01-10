using System;
using System.Runtime.Serialization;

using ProtoBuf;

namespace SKBKontur.GroBuf.Tests.TestData.Orders
{
    [DataContract]
    [ProtoContract]
    public class DateTimePeriodGroup
    {
        [DataMember]
        [ProtoMember(1)]
        public sbyte? FunctionCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public DateTime Value { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public byte? FormatCode { get; set; }
    }
}