using MessagePack;
using System.Net.Sockets;

namespace YuchiGames.POM.Shared
{
    [Union(0, typeof(AcceptJoinMessage))]
    public interface IServerMessage
    {
        public byte Channel { get; }
        public ProtocolType Protocol { get; }
    }

    [MessagePackObject]
    public class AcceptJoinMessage : IServerMessage
    {
        [IgnoreMember]
        public byte Channel { get; } = 0x00;
        [IgnoreMember]
        public ProtocolType Protocol { get; } = ProtocolType.Tcp;
    }
}
