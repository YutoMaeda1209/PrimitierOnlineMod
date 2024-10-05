using System.Text;
using System.Text.Json;
using System.IO.Compression;
using YuchiGames.POM.Shared.DataObjects;
using MessagePack;
using Newtonsoft.Json;

namespace YuchiGames.POM.Server.Managers
{
    public class WorldManager
    {
        public static WorldData Load(string path)
        {
            using FileStream fs = new FileStream(path, FileMode.Open);
            using GZipStream gzs = new GZipStream(fs, CompressionMode.Decompress);

            byte[] streamData = new byte[gzs.Length];
            gzs.Read(streamData, 0, streamData.Length);

            string json = Encoding.UTF8.GetString(streamData);

            return JsonConvert.DeserializeObject<WorldData>(json) ?? throw new FormatException("Wrong data format");
        }

        public static void Save(WorldData world, string path)
        {
            string json = JsonConvert.SerializeObject(world);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            using FileStream fs = new FileStream(path, FileMode.Create);
            using GZipStream gz = new GZipStream(fs, CompressionMode.Compress);
            gz.Write(jsonBytes, 0, jsonBytes.Length);
        }
    }
}