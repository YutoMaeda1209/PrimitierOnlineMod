using MessagePack;
using System.Net.Sockets;

namespace YuchiGames.POM.Shared
{
    public interface IServerMessage
    {
        public ProtocolType Protocol { get; }
    }
}
