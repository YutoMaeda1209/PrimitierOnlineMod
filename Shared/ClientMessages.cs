using MessagePack;
using System.Net.Sockets;

namespace YuchiGames.POM.Shared
{
    public interface IClientMessage
    {
        public ProtocolType Protocol { get; }
    }
}
