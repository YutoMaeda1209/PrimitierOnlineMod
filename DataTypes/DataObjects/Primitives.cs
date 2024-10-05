using MessagePack;

namespace YuchiGames.POM.Shared.DataObjects
{
    [MessagePackObject]
    public struct SVector3
    {
        [Key(0)]
        public float X { get; set; }
        [Key(1)]
        public float Y { get; set; }
        [Key(2)]
        public float Z { get; set; }

        public static SVector3 operator -(SVector3 a, SVector3 b) => new SVector3()
        {
            X = a.X - b.X,
            Y = a.Y - b.Y,
            Z = a.Z - b.Z
        };

        public static float Distance(SVector3 a, SVector3 b)
        {
            var s = a - b;
            return MathF.Sqrt(s.X * s.X + s.Y * s.Y + s.Z * s.Z); 
        }

        public override string ToString() =>
            $"({X}, {Y}, {Z})";

        [SerializationConstructor]
        public SVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    [MessagePackObject]
    public struct SVector2
    {
        [Key(0)]
        public float X { get; set; }
        [Key(1)]
        public float Y { get; set; }

        [SerializationConstructor]
        public SVector2(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    [MessagePackObject]
    public struct SVector2Int
    {
        [Key(0)]
        public int X { get; set; }
        [Key(1)]
        public int Y { get; set; }

        [SerializationConstructor]
        public SVector2Int(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"({X}, {Y})";
    }

    [MessagePackObject]
    public struct SQuaternion
    {
        [Key(0)]
        public float X { get; set; }
        [Key(1)]
        public float Y { get; set; }
        [Key(2)]
        public float Z { get; set; }
        [Key(3)]
        public float W { get; set; }

        [SerializationConstructor]
        public SQuaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
    }



    [MessagePackObject]
    public struct STransform
    {
        [Key(0)]
        public SVector3 Position { get; set; }
        [Key(1)]
        public SQuaternion Rotation { get; set; }

        [SerializationConstructor]
        public STransform(SVector3 position, SQuaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}
