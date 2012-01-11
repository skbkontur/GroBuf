using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Orders
{
    [DataContract]
    [ProtoContract]
    public class RangeDetails
    {
        [DataMember]
        [ProtoMember(1)]
        public short[] RangeTypeCodeQualifier { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public Range Range { get; set; }
    }
}