using LiteNetLib;
using MelonLoader;
using MessagePack;
using System.Net.Sockets;
using YuchiGames.POM.Shared;

namespace YuchiGames.POM.Server.Managers
{
    public class ReceiveMessageEventArgs
    {
        public NetPeer Peer { get; }
        public IClientMessage Message { get; }

        public ReceiveMessageEventArgs(NetPeer peer, IClientMessage message)
        {
            Peer = peer;
            Message = message;
        }
    }

    public class NetworkManager
    {
        public event EventHandler<ReceiveMessageEventArgs> ReceiveMessageEvent = delegate { };

        private EventBasedNetListener _listener;
        private NetManager _server;
        private int _port;

        public NetworkManager(int port = 54162)
        {
            _listener = new EventBasedNetListener();
            _listener.ConnectionRequestEvent += ConnectionRequestEventHandler;
            _listener.PeerConnectedEvent += PeerConnectedEventHandler;
            _listener.PeerDisconnectedEvent += PeerDisconnectedEventHandler;
            _listener.NetworkReceiveEvent += NetworkReceiveEventHandler;
            _server = new NetManager(_listener)
            {
                AutoRecycle = true
            };
            _port = port;
        }

        public void Start()
        {
            _server.Start(_port);
            Melon<Program>.Logger.Msg($"Server started on port {_port}");
        }

        public void Stop()
        {
            _server.Stop();
            Melon<Program>.Logger.Msg("Server stopped");
        }

        public void Send(int id, IServerMessage message)
        {
            byte[] data = MessagePackSerializer.Serialize(message);
            switch (message.Protocol)
            {
                case ProtocolType.Tcp:
                    _server.GetPeerById(id).Send(data, DeliveryMethod.ReliableOrdered);
                    break;
                case ProtocolType.Udp:
                    _server.GetPeerById(id).Send(data, DeliveryMethod.Sequenced);
                    break;
                default:
                    throw new Exception("Unknown protocol");
            }
        }

        public void Send(IServerMessage message)
        {
            byte[] data = MessagePackSerializer.Serialize(message);
            switch (message.Protocol)
            {
                case ProtocolType.Tcp:
                    _server.SendToAll(data, DeliveryMethod.ReliableOrdered);
                    break;
                case ProtocolType.Udp:
                    _server.SendToAll(data, DeliveryMethod.Sequenced);
                    break;
                default:
                    throw new Exception("Unknown protocol");
            }
        }

        public void Update()
        {
            _server.PollEvents();
        }

        private void ConnectionRequestEventHandler(ConnectionRequest request)
        {
            request.Accept();
            Melon<Program>.Logger.Msg($"Accepted connection from client: {request.RemoteEndPoint}");
        }

        private void PeerConnectedEventHandler(NetPeer peer)
        {
            Melon<Program>.Logger.Msg($"Connected to client: {peer.Address}:{peer.Port}");
        }

        private void PeerDisconnectedEventHandler(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Melon<Program>.Logger.Msg($"Disconnected from client because {disconnectInfo.Reason}: {peer.Address}:{peer.Port}");
        }

        private void NetworkReceiveEventHandler(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            byte[] buffer = new byte[reader.AvailableBytes];
            reader.GetBytes(buffer, buffer.Length);
            IClientMessage message = MessagePackSerializer.Deserialize<IClientMessage>(buffer);
            ReceiveMessageEventArgs eventArgs = new ReceiveMessageEventArgs(peer, message);
            ReceiveMessageEvent(this, eventArgs);
        }
    }
}
