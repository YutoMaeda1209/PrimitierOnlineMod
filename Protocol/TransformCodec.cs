using System;
using UnityEngine;

namespace YuchiGames.POM.Protocol
{
    public static class TransformCodec
    {
        static TransformCodec()
        {
            if (!BitConverter.IsLittleEndian)
                MelonLoader.MelonLogger.Warning("Wire assumes little-endian floats/ints.");
        }

        public static byte[] Vector3ToBytes(in Vector3 v)
        {
            byte[] bytes = new byte[Wire.SizeOfVector3];
            Buffer.BlockCopy(BitConverter.GetBytes(v.x), 0, bytes, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(v.y), 0, bytes, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(v.z), 0, bytes, 8, 4);
            return bytes;
        }

        public static Vector3 BytesToVector3(ReadOnlySpan<byte> src)
        {
            if (src.Length != Wire.SizeOfVector3) throw new ArgumentException("Vector3 must be 12 bytes.");
            byte[] a = src.ToArray();
            float x = BitConverter.ToSingle(a, 0);
            float y = BitConverter.ToSingle(a, 4);
            float z = BitConverter.ToSingle(a, 8);
            return new Vector3(x, y, z);
        }

        public static byte[] QuaternionToBytes(in Quaternion q)
        {
            byte[] bytes = new byte[Wire.SizeOfQuaternion];
            Buffer.BlockCopy(BitConverter.GetBytes(q.x), 0, bytes,  0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(q.y), 0, bytes,  4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(q.z), 0, bytes,  8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(q.w), 0, bytes, 12, 4);
            return bytes;
        }

        public static Quaternion BytesToQuaternion(ReadOnlySpan<byte> src)
        {
            if (src.Length != Wire.SizeOfQuaternion) throw new ArgumentException("Quaternion must be 16 bytes.");
            byte[] a = src.ToArray();
            float x = BitConverter.ToSingle(a,  0);
            float y = BitConverter.ToSingle(a,  4);
            float z = BitConverter.ToSingle(a,  8);
            float w = BitConverter.ToSingle(a, 12);
            return new Quaternion(x, y, z, w);
        }

        public static byte[] TransformToBytes(Transform t)
        {
            byte[] p = Vector3ToBytes(t.position);   // 12
            byte[] r = QuaternionToBytes(t.rotation); // 16
            byte[] s = Vector3ToBytes(t.localScale); // 12

            byte[] bytes = new byte[Wire.SizeOfTransform]; // 40
            Buffer.BlockCopy(p, 0, bytes,  0, 12);
            Buffer.BlockCopy(r, 0, bytes, 12, 16);
            Buffer.BlockCopy(s, 0, bytes, 28, 12);
            return bytes;
        }

        public static void BytesToTransform(ReadOnlySpan<byte> src, Transform t)
        {
            if (src.Length != Wire.SizeOfTransform) throw new ArgumentException("Transform must be 40 bytes.");
            t.position   = BytesToVector3(src.Slice( 0, 12));
            t.rotation   = BytesToQuaternion(src.Slice(12, 16));
            t.localScale = BytesToVector3(src.Slice(28, 12));
        }
    }
}
