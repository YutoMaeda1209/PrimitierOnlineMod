using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace YuchiGames.POM.Shared
{
    public interface IDataMessage
    {
        public byte Channel { get; }
        public ProtocolType Protocol { get; }

        // TODO: Do it a better way
        static byte[] Serialize(IDataMessage message) => message switch
        {
            IGameDataMessage m => MessagePackSerializer.Serialize(m),
            IServerDataMessage m => MessagePackSerializer.Serialize(m),
            _ => throw new NotImplementedException(),
        };
    }
}
