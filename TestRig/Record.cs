using ProtoBuf;

namespace TestRig
{
    [ProtoContract]
    public class Record
    {
        [ProtoMember(1)]
        public long ID;
        [ProtoMember(2)]
        public long Value;
        [ProtoMember(3)]
        public string SValue;
    }
}
