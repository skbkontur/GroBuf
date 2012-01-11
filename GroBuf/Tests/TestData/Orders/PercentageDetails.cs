using System;
using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Orders
{
    [DataContract]
    [ProtoContract]
    public class PercentageDetails
    {
        [DataMember]
        [ProtoMember(1)]
        public PercentageDetailsGroup PercentageDetailsGroup { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public DateTime[] StatusDescriptionCode { get; set; }
    }
}