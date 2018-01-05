using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Desadv
{
    [DataContract]
    [ProtoContract]
    public class DateTimePeriodGroup
    {
        [DataMember]
        [ProtoMember(1)]
        public byte[] FunctionCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public byte[] Value { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public byte[] FormatCode { get; set; }
    }
}