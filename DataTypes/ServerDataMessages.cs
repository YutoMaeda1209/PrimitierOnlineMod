using MessagePack;
using YuchiGames.POM.Shared.DataObjects;
using System.Net.Sockets;

namespace YuchiGames.POM.Shared
{
    [Union(0, typeof(RequestServerInfoMessage))]
    [Union(1, typeof(ServerInfoMessage))]
    [Union(2, typeof(RequestNewChunkDataMessage))]
    [Union(3, typeof(SavedChunkDataMessage))]
    [Union(4, typeof(ChunkUnloadMessage))]
    [Union(5, typeof(RequestedChunkDataMessage))]
    public interface IServerDataMessage : IDataMessage
    {
        [IgnoreMember]
        byte IDataMessage.Channel => 0x01;
    }

    [MessagePackObject]
    public class RequestServerInfoMessage : IServerDataMessage
    {
        [IgnoreMember]
        public ProtocolType Protocol => ProtocolType.Tcp;


        [Key(0)]
        public string UserGUID { get; }

        [SerializationConstructor]
        public RequestServerInfoMessage(string userGUID)
        {
            UserGUID = userGUID;
        }
    }

    [MessagePackObject]
    public class ServerInfoMessage : IServerDataMessage
    {
        [IgnoreMember]
        public ProtocolType Protocol => ProtocolType.Tcp;


        [Key(0)]
        public int MaxPlayers { get; set; }
        [Key(1)]
        public LocalWorldData WorldData { get; set; } = null!;
        [Key(2)]
        public bool IsDayNightCycle { get; set; }
        [Key(3)]
        public int UID { get; set; }
        [Key(4)]
        public float WorldQuickUpdateDistance { get; set; }
        [Key(5)]
        public float WorldUpdateDistance { get; set; }
    }


    // WARNING:
    // client->server: client request new chunk to load
    // server->client: server request to generate that chunk
    [MessagePackObject]
    public class RequestNewChunkDataMessage : IServerDataMessage
    {
        [IgnoreMember]
        public ProtocolType Protocol => ProtocolType.Tcp;

        [Key(0)]
        public SVector2Int ChunkPos { get; set; }
    }
    [MessagePackObject]
    public class SavedChunkDataMessage : IServerDataMessage
    {
        [IgnoreMember]
        public ProtocolType Protocol => ProtocolType.Tcp;

        [Key(0)]
        public Chunk Chunk { get; set; } = null!;
        [Key(1)]
        public SVector2Int Pos { get; set; }
    }
    [MessagePackObject]
    public class ChunkUnloadMessage : IServerDataMessage
    {
        [IgnoreMember]
        public ProtocolType Protocol => ProtocolType.Udp;

        [Key(0)]
        public SVector2Int Pos { get; set; }
    }
    [MessagePackObject]
    public class RequestedChunkDataMessage : IServerDataMessage
    {
        [IgnoreMember]
        public ProtocolType Protocol => ProtocolType.Tcp;

        /*[Key(0)]
        public Chunk Chunk { get; set; } = null!;*/
        [Key(0)]
        public SVector2Int Pos { get; set; }
    }
}