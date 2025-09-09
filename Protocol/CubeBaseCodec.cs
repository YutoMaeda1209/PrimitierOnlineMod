using System;
using UnityEngine;
using Il2Cpp;

namespace YuchiGames.POM.Protocol
{
    public static class CubeBaseCodec
    {
        public static byte[] Compose(Vector3 pos, Quaternion rot, Vector3 scale,
                                     CubeConnector.Anchor anchor, Substance substance,
                                     byte flags = 0)
        {
            int a = (int)anchor;
            if ((uint)a > 2u) a = 0; // 0..2 に正規化
            byte header = (byte)((flags & 0b1111_1100) | (a & 0b11));

            byte[] p = TransformCodec.Vector3ToBytes(pos);
            byte[] r = TransformCodec.QuaternionToBytes(rot);
            byte[] s = TransformCodec.Vector3ToBytes(scale);
            byte[] subBytes = BitConverter.GetBytes((int)substance);

            byte[] payload = new byte[Wire.CubeBaseSize]; // 45
            Buffer.BlockCopy(p, 0, payload,  0, 12);
            Buffer.BlockCopy(r, 0, payload, 12, 16);
            Buffer.BlockCopy(s, 0, payload, 28, 12);
            payload[40] = header;
            Buffer.BlockCopy(subBytes, 0, payload, 41, 4);
            return payload;
        }

        public static void Parse(ReadOnlySpan<byte> payload,
                                 out Vector3 position, out Quaternion rotation, out Vector3 scale,
                                 out CubeConnector.Anchor anchor, out Substance substance, out byte flags)
        {
            if (payload.Length != Wire.CubeBaseSize)
                throw new ArgumentException($"CubeBase payload must be {Wire.CubeBaseSize} bytes.");

            position   = TransformCodec.BytesToVector3(payload.Slice(0, 12));
            rotation   = TransformCodec.BytesToQuaternion(payload.Slice(12,16));
            scale = TransformCodec.BytesToVector3(payload.Slice(28,12));

            byte header = payload[40];
            int a = header & 0b11;                 // Anchor 2bit
            flags = (byte)(header & 0b1111_1100);  // バイト列で処理するために予約してるだけだよ、後で追加出たら使ってちょ
            anchor = (CubeConnector.Anchor)a;

            var subArr = payload.Slice(41, 4).ToArray();
            substance = (Substance)BitConverter.ToInt32(subArr, 0);
        }
    }
}
