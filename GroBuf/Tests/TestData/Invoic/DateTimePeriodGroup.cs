using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Invoic
{
    [DataContract]
    [ProtoContract]
    public class DateTimePeriodGroup
    {
        [DataMember]
        [ProtoMember(1)]
        public string FunctionCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public string Value { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public string FormatCode { get; set; }
    }
}