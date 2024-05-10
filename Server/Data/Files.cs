﻿using MessagePack;

namespace YuchiGames.POM.Server.Data.Files
{
    [MessagePackObject]
    public class VRMData
    {
        [Key(0)]
        public string Name { get; set; } = "VRMData";
        [Key(1)]
        public byte[] Data { get; set; }

        [SerializationConstructor]
        public VRMData(byte[] data)
        {
            Data = data;
        }
    }
}