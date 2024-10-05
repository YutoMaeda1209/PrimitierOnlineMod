using MessagePack;

namespace YuchiGames.POM.Shared.DataObjects
{
    [MessagePackObject]
    public class VRMPosData
    {
        [Key(0)]
        public STransform Head { get; }
        [Key(1)]
        public STransform Hips { get; }
        [Key(2)]
        public STransform Spine { get; }
        [Key(3)]
        public STransform LUpperArm { get; }
        [Key(4)]
        public STransform RUpperArm { get; }
        [Key(5)]
        public STransform LLowerArm { get; }
        [Key(6)]
        public STransform RLowerArm { get; }
        [Key(7)]
        public STransform LHand { get; }
        [Key(8)]
        public STransform RHand { get; }
        [Key(9)]
        public STransform LUpperLeg { get; }
        [Key(10)]
        public STransform RUpperLeg { get; }
        [Key(11)]
        public STransform LLowerLeg { get; }
        [Key(12)]
        public STransform RLowerLeg { get; }
        [Key(13)]
        public STransform LFoot { get; }
        [Key(14)]
        public STransform RFoot { get; }

        [SerializationConstructor]
        public VRMPosData(STransform head, STransform hips, STransform spine, STransform lUpperArm, STransform rUpperArm, STransform lLowerArm, STransform rLowerArm, STransform lHand, STransform rHand, STransform lUpperLeg, STransform rUpperLeg, STransform lLowerLeg, STransform rLowerLeg, STransform lFoot, STransform rFoot)
        {
            Head = head;
            Hips = hips;
            Spine = spine;
            LUpperArm = lUpperArm;
            RUpperArm = rUpperArm;
            LLowerArm = lLowerArm;
            RLowerArm = rLowerArm;
            LHand = lHand;
            RHand = rHand;
            LUpperLeg = lUpperLeg;
            RUpperLeg = rUpperLeg;
            LLowerLeg = lLowerLeg;
            RLowerLeg = rLowerLeg;
            LFoot = lFoot;
            RFoot = rFoot;
        }
    }

    [MessagePackObject]
    public class PlayerPositionData
    {
        [Key(0)]
        public STransform Head { get; set;  }
        [Key(1)]
        public STransform LeftHand { get; set;  }
        [Key(2)]
        public STransform RightHand { get; set; }
        [Key(3)]
        public SVector3 BasePosition { get; set; }
    }
}