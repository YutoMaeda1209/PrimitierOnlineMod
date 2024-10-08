﻿using Il2Cpp;
using YuchiGames.POM.Shared.DataObjects;
using UnityEngine;
using YuchiGames.POM.Client.Assets;

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
            return new STransform(new SVector3(transform.position.x, transform.position.y, transform.position.z),
                new SQuaternion(transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w));
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

        public static List<T> ToSystem<T>(this Il2CppSystem.Collections.Generic.List<T> il2cppList)
        {
            List<T> systemList = new List<T>();
            foreach (T item in il2cppList)
            {
                systemList.Add(item);
            }
            return systemList;
        }

        public static Chunk ToChunk(SaveAndLoad.ChunkData chunkData)
        {
            List<Group> groups = new List<Group>();
            foreach (SaveAndLoad.GroupData groupData in chunkData.groups)
            {
                List<Cube> cubes = new List<Cube>();
                foreach (SaveAndLoad.CubeData cubeData in groupData.cubes)
                {
                    UVOffset uvOffset = new UVOffset()
                    {
                        Right = cubeData.uvOffset.right.ToShared(),
                        Left = cubeData.uvOffset.left.ToShared(),
                        Top = cubeData.uvOffset.top.ToShared(),
                        Bottom = cubeData.uvOffset.bottom.ToShared(),
                        Front = cubeData.uvOffset.front.ToShared(),
                        Back = cubeData.uvOffset.back.ToShared()
                    };
                    Cube cube = new Cube(
                        cubeData.pos.ToShared(),
                        cubeData.rot.ToShared(),
                        cubeData.scale.ToShared(),
                        cubeData.lifeRatio,
                        (Anchor)cubeData.anchor,
                        (DataObjects.Substance)cubeData.substance,
                        (DataObjects.CubeName)cubeData.name,
                        ToSystem<int>(cubeData.connections),
                        cubeData.temperature,
                        cubeData.isBurning,
                        cubeData.burnedRatio,
                        (SectionState)cubeData.sectionState,
                        uvOffset,
                        cubeData.behaviors.ToSystem(),
                        cubeData.states.ToSystem()
                        );
                    cubes.Add(cube);
                }
                Group group = new Group(
                    groupData.pos.ToShared(),
                    groupData.rot.ToShared(),
                    cubes
                    );
                groups.Add(group);
            }
            Chunk chunk = new Chunk(chunkData.x, chunkData.z, groups);
            return chunk;
        }

        public static SaveAndLoad.ChunkData ToChunkData(Chunk chunk)
        {
            List<SaveAndLoad.GroupData> groupDataList = new List<SaveAndLoad.GroupData>();
            foreach (Group group in chunk.Groups)
            {
                List<SaveAndLoad.CubeData> cubeDataList = new List<SaveAndLoad.CubeData>();
                foreach (Cube cube in group.Cubes)
                {
                    CubeAppearance.UVOffset uvOffset = new CubeAppearance.UVOffset()
                    {
                        right = cube.UVOffset.Right.ToUnity(),
                        left = cube.UVOffset.Left.ToUnity(),
                        top = cube.UVOffset.Top.ToUnity(),
                        bottom = cube.UVOffset.Bottom.ToUnity(),
                        front = cube.UVOffset.Front.ToUnity(),
                        back = cube.UVOffset.Back.ToUnity()
                    };
                    SaveAndLoad.CubeData cubeData = new SaveAndLoad.CubeData()
                    {
                        pos = cube.Position.ToUnity(),
                        rot = cube.Rotation.ToUnity(),
                        scale = cube.Scale.ToUnity(),
                        lifeRatio = cube.LifeRatio,
                        anchor = (CubeConnector.Anchor)cube.Anchor,
                        substance = (Il2Cpp.Substance)cube.Substance,
                        name = (Il2Cpp.CubeName)cube.Name,
                        connections = cube.Connections.ToIl2cpp(),
                        temperature = cube.Temperature,
                        isBurning = cube.IsBurning,
                        burnedRatio = cube.BurnedRatio,
                        sectionState = (CubeAppearance.SectionState)cube.SectionState,
                        uvOffset = uvOffset,
                        behaviors = cube.Behaviors.ToIl2cpp(),
                        states = cube.States.ToIl2cpp()
                    };
                    cubeDataList.Add(cubeData);
                }
                SaveAndLoad.GroupData groupData = new SaveAndLoad.GroupData()
                {
                    pos = group.Position.ToUnity(),
                    rot = group.Rotation.ToUnity(),
                    cubes = cubeDataList.ToIl2cpp()
                };
                groupDataList.Add(groupData);
            }
            SaveAndLoad.ChunkData chunkData = new SaveAndLoad.ChunkData()
            {
                x = chunk.X,
                z = chunk.Z,
                groups = groupDataList.ToIl2cpp()
            };
            return chunkData;
        }

        public static Group ToGroup(SaveAndLoad.GroupData groupData)
        {
            List<Cube> cubes = new List<Cube>();
            foreach (SaveAndLoad.CubeData cubeData in groupData.cubes)
            {
                UVOffset uvOffset = new UVOffset()
                {
                    Right = cubeData.uvOffset.right.ToShared(),
                    Left = cubeData.uvOffset.left.ToShared(),
                    Top = cubeData.uvOffset.top.ToShared(),
                    Bottom = cubeData.uvOffset.bottom.ToShared(),
                    Front = cubeData.uvOffset.front.ToShared(),
                    Back = cubeData.uvOffset.back.ToShared()
                };
                Cube cube = new Cube(
                    cubeData.pos.ToShared(),
                    cubeData.rot.ToShared(),
                    cubeData.scale.ToShared(),
                    cubeData.lifeRatio,
                    (Anchor)cubeData.anchor,
                    (DataObjects.Substance)cubeData.substance,
                    (DataObjects.CubeName)cubeData.name,
                    cubeData.connections.ToSystem(),
                    cubeData.temperature,
                    cubeData.isBurning,
                    cubeData.burnedRatio,
                    (SectionState)cubeData.sectionState,
                    uvOffset,
                    cubeData.behaviors.ToSystem(),
                    cubeData.states.ToSystem()
                    );
                cubes.Add(cube);
            }
            Group group = new Group(
                groupData.pos.ToShared(),
                groupData.rot.ToShared(),
                cubes
                );
            return group;
        }
    }
}