using Il2Cpp;
using YuchiGames.POM.Shared.DataObjects;
using UnityEngine;
using YuchiGames.POM.Client.Assets;
using static Il2Cpp.CubeAppearance;
using static Il2Cpp.SaveAndLoad;
using YuchiGames.POM.Shared.Utils;

namespace YuchiGames.POM.Shared
{
    public static class DataConverter
    {
        public static Vector3 ToUnity(this SVector3 sVector3)
        {
            return new Vector3(sVector3.X, sVector3.Y, sVector3.Z);
        }

        public static Vector2 ToUnity(this SVector2 sVector2)
        {
            return new Vector2(sVector2.X, sVector2.Y);
        }

        public static Vector2Int ToUnity(this SVector2Int sVector2Int)
        {
            return new Vector2Int(sVector2Int.X, sVector2Int.Y);
        }

        public static Quaternion ToUnity(this SQuaternion sQuaternion)
        {
            return new Quaternion(sQuaternion.X, sQuaternion.Y, sQuaternion.Z, sQuaternion.W);
        }

        public static Transform ToUnity(this STransform sTransform)
        {
            Transform transform = new Transform();
            transform.position = sTransform.Position.ToUnity();
            transform.rotation = sTransform.Rotation.ToUnity();
            return transform;
        }

        public static SVector3 ToShared(this Vector3 vector3)
        {
            return new SVector3(vector3.x, vector3.y, vector3.z);
        }

        public static SVector2 ToShared(this Vector2 vector2)
        {
            return new SVector2(vector2.x, vector2.y);
        }

        public static SVector2Int ToShared(this Vector2Int vector2Int)
        {
            return new SVector2Int(vector2Int.x, vector2Int.y);
        }

        public static SQuaternion ToShared(this Quaternion quaternion)
        {
            return new SQuaternion(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
        }

        public static STransform ToShared(this Transform transform)
        {
            return new STransform(transform.position.ToShared(), transform.rotation.ToShared());
        }

        public static Il2CppSystem.Collections.Generic.List<T> ToIl2cpp<T>(this List<T> systemList)
        {
            Il2CppSystem.Collections.Generic.List<T> il2cppList = new Il2CppSystem.Collections.Generic.List<T>();
            foreach (T item in systemList)
            {
                il2cppList.Add(item);
            }
            return il2cppList;
        }

        public static Il2CppSystem.Collections.Generic.HashSet<T> ToIl2Cpp<T>(this HashSet<T> systemSet) =>
            new Il2CppSystem.Collections.Generic.HashSet<T>()
            .Apply(set =>
            {
                foreach (var item in systemSet)
                    set.Add(item);
            });

        public static HashSet<T> ToSystem<T>(this Il2CppSystem.Collections.Generic.HashSet<T> values) =>
            new HashSet<T>().Apply(set =>
            {
                foreach (var item in values)
                    set.Add(item);
            });

        public static List<T> ToSystem<T>(this Il2CppSystem.Collections.Generic.List<T> il2cppList)
        {
            List<T> systemList = new List<T>();
            foreach (T item in il2cppList)
            {
                systemList.Add(item);
            }
            return systemList;
        }

        public static Chunk ToChunk(Il2CppSystem.Collections.Generic.List<GroupData> groups) => new Chunk
        {
            Groups = groups.ToSystem().Select(group => new Group
            {
                Position = group.pos.ToShared(),
                Rotation = group.rot.ToShared(),
                Cubes = group.cubes.ToSystem().Select(ToCube).ToList()
            }).ToList(),
        };

        private static Cube ToCube(CubeData cube) => new()
        {
            Position = cube.pos.ToShared(),
            Rotation = cube.rot.ToShared(),
            Scale = cube.scale.ToShared(),
            Life = cube.lifeRatio,
            MaxLife = cube.lifeRatio,
            Anchor = (Anchor)cube.anchor,
            Substance = (DataObjects.Substance)cube.substance,
            Name = (DataObjects.CubeName)cube.name,
            Connections = cube.connections.ToSystem(),
            Temperature = cube.temperature,
            IsBurning = cube.isBurning,
            BurnedRatio = cube.burnedRatio,
            SectionState = (DataObjects.SectionState)cube.sectionState,
            UVOffset = new DataObjects.UVOffset()
            {
                Right = cube.uvOffset.right.ToShared(),
                Left = cube.uvOffset.left.ToShared(),
                Top = cube.uvOffset.top.ToShared(),
                Bottom = cube.uvOffset.bottom.ToShared(),
                Front = cube.uvOffset.front.ToShared(),
                Back = cube.uvOffset.back.ToShared()
            },
            Behaviors = cube.behaviors.ToSystem(),
            States = cube.states.ToSystem(),
        };

        public static Il2CppSystem.Collections.Generic.List<GroupData> ToIl2CppChunk(Chunk chunk) =>
            chunk.Groups.Select(group => new GroupData
            {
                pos = group.Position.ToUnity(),
                rot = group.Rotation.ToUnity(),
                cubes = group.Cubes.Select(ToIl2CppCube).ToList().ToIl2cpp()
            }).ToList().ToIl2cpp();

        public static CubeData ToIl2CppCube(Cube cube) => new()
        {
            pos = cube.Position.ToUnity(),
            rot = cube.Rotation.ToUnity(),
            scale = cube.Scale.ToUnity(),
            lifeRatio = cube.Life,
            anchor = (CubeConnector.Anchor)cube.Anchor,
            substance = (Il2Cpp.Substance)cube.Substance,
            name = (Il2Cpp.CubeName)cube.Name,
            connections = cube.Connections.ToIl2cpp(),
            temperature = cube.Temperature,
            isBurning = cube.IsBurning,
            burnedRatio = cube.BurnedRatio,
            sectionState = (CubeAppearance.SectionState)cube.SectionState,
            uvOffset = new()
            {
                right = cube.UVOffset.Right.ToUnity(),
                left = cube.UVOffset.Left.ToUnity(),
                top = cube.UVOffset.Top.ToUnity(),
                bottom = cube.UVOffset.Bottom.ToUnity(),
                front = cube.UVOffset.Front.ToUnity(),
                back = cube.UVOffset.Back.ToUnity()
            },
            behaviors = cube.Behaviors.ToIl2cpp(),
            states = cube.States.ToIl2cpp(),
        };
    }
}