using System.Runtime.Serialization;

using ProtoBuf;

namespace SKBKontur.GroBuf.Tests.TestData.Invoic
{
    [DataContract]
    [ProtoContract]
    public class DateTimePeriod
    {
        [DataMember]
        [ProtoMember(1)]
        public DateTimePeriodGroup DateTimePeriodGroup { get; set; }
    }
}