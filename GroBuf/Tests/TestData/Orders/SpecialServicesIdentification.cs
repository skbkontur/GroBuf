using System;
using System.Runtime.Serialization;

using ProtoBuf;

namespace GroBuf.Tests.TestData.Orders
{
    [DataContract]
    [ProtoContract]
    public class SpecialServicesIdentification
    {
        [DataMember]
        [ProtoMember(1)]
        public bool[] SpecialServiceDescriptionCode { get; set; }

        [DataMember]
        [ProtoMember(2)]
        public sbyte CodeListIdentificationCode { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public bool? CodeListResponsibleAgencyCode { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public bool?[] SpecialServiceDescription { get; set; }
        
        [DataMember]
        [ProtoMember(5)]
        public Guid Id { get; set; }

        [DataMember]
        [ProtoMember(6)]
        public Guid[] ChildrenIds { get; set; }

        [DataMember]
        [ProtoMember(5)]
        public Guid? ParentId { get; set; }

        [DataMember]
        [ProtoMember(6)]
        public Guid?[] ParentChildrenIds { get; set; }
    }
}