using LiteNetLib;
using MelonLoader;
using MessagePack;
using System.Net.Sockets;
using YuchiGames.POM.Shared;

namespace YuchiGames.POM.Client.Managers
{
    public class ReceiveMessageEventArgs
    {
        public NetPeer Peer { get; }
        public IServerMessage Message { get; }

        public ReceiveMessageEventArgs(NetPeer peer, IServerMessage message)
        {
            Peer = peer;
            Message = message;
        }
    }

    public class NetworkManager
    {
        public event EventHandler<ReceiveMessageEventArgs> ReceiveMessageEvent = delegate { };

        private EventBasedNetListener _listener;
        private NetManager _client;
        private string _ipAddress;
        private int _port;

        public NetworkManager(string ipAddress = "127.0.0.1", int port = 54162)
        {
            _listener = new EventBasedNetListener();
            _listener.PeerConnectedEvent += PeerConnectedEventHandler;
            _listener.PeerDisconnectedEvent += PeerDisconnectedEventHandler;
            _listener.NetworkReceiveEvent += NetworkReceiveEventHandler;
            _client = new NetManager(_listener)
            {
                AutoRecycle = true
            };
            _ipAddress = ipAddress;
            _port = port;
        }

        public void Connect()
        {
            _client.Start();
            _client.Connect(_ipAddress, _port, "Password");
            Melon<Program>.Logger.Msg($"Connecting to server: {_ipAddress}:{_port}");
        }

        public void Disconnect()
        {
            _client.Stop();
            Melon<Program>.Logger.Msg("Disconnected from server");
        }

        public void Send(IClientMessage message)
        {
            byte[] data = MessagePackSerializer.Serialize(message);
            switch (message.Protocol)
            {
                case ProtocolType.Tcp:
                    _client.FirstPeer.Send(data, DeliveryMethod.ReliableOrdered);
                    break;
                case ProtocolType.Udp:
                    _client.FirstPeer.Send(data, DeliveryMethod.Sequenced);
                    break;
                default:
                    throw new Exception("Unknown protocol");
            }
        }

        public void Update()
        {
            _client.PollEvents();
        }

        private void PeerConnectedEventHandler(NetPeer peer)
        {
            Melon<Program>.Logger.Msg($"Connected to server: {peer.Address}:{peer.Port}");
        }

        private void PeerDisconnectedEventHandler(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Melon<Program>.Logger.Msg($"Disconnected from server because {disconnectInfo.Reason}: {peer.Address}:{peer.Port}");
        }

        private void NetworkReceiveEventHandler(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            byte[] buffer = new byte[reader.AvailableBytes];
            reader.GetBytes(buffer, buffer.Length);
            IServerMessage message = MessagePackSerializer.Deserialize<IServerMessage>(buffer);
            ReceiveMessageEventArgs eventArgs = new ReceiveMessageEventArgs(peer, message);
            ReceiveMessageEvent(this, eventArgs);
        }
    }
}
