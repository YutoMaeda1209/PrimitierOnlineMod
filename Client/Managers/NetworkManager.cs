using LiteNetLib;
using MelonLoader;

namespace YuchiGames.POM.Client.Managers
{
    public class NetworkManager
    {
        private EventBasedNetListener _listener;
        private NetManager _client;
        private string _ipAddress;
        private int _port;

        public NetworkManager(string ipAddress = "127.0.0.1", int port = 54162)
        {
            _listener = new EventBasedNetListener();
            _listener.PeerConnectedEvent += PeerConnectedEventHandler;
            _listener.PeerDisconnectedEvent += PeerDisconnectedEventHandler;
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
    }
}
