using System.Runtime.Serialization;

using ProtoBuf;

namespace SKBKontur.GroBuf.Tests.TestData.Orders
{
    [DataContract]
    [ProtoContract]
    public class DutyTaxFeeType
    {
        [DataMember]
        [ProtoMember(1)]
        public byte?[] DutyTaxFeeTypeNameCode { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public long[] CodeListIdentificationCode { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public ulong[] CodeListResponsibleAgencyCode { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public ushort?[] DutyTaxFeeTypeName { get; set; }
    }
}