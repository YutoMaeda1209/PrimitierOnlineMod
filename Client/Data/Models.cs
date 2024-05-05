﻿using MessagePack;

namespace YuchiGames.POM.Client.Data.Models
{
    [MessagePackObject]
    public class Plyaer
    {
        [Key(0)]
        public PosRot LeftHand { get; set; }
        [Key(1)]
        public PosRot RightHand { get; set; }

        [SerializationConstructor]
        public Plyaer(PosRot leftHand, PosRot rightHand)
        {
            LeftHand = leftHand;
            RightHand = rightHand;
        }
    }

    [MessagePackObject]
    public class Object
    {
        [Key(0)]
        public string UUID { get; set; }
        [Key(1)]
        public PosRot PosRot { get; set; }

        [SerializationConstructor]
        public Object(string uuid, PosRot posRot)
        {
            UUID = uuid;
            PosRot = posRot;
        }
    }

    [MessagePackObject]
    public class PosRot
    {
        [Key(0)]
        public float[] Pos { get; set; }
        [Key(1)]
        public float[] Rot { get; set; }

        [SerializationConstructor]
        public PosRot(float[] pos, float[] rot)
        {
            Pos = pos;
            Rot = rot;
        }
    }
}