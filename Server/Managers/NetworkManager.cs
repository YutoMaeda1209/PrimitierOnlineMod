using LiteNetLib;
using MelonLoader;

namespace YuchiGames.POM.Server.Managers
{
    public class NetworkManager
    {
        private EventBasedNetListener _listener;
        private NetManager _client;
        private int _port;

        public NetworkManager(int port = 54162)
        {
            _listener = new EventBasedNetListener();
            _listener.ConnectionRequestEvent += ConnectionRequestEventHandler;
            _listener.PeerConnectedEvent += PeerConnectedEventHandler;
            _listener.PeerDisconnectedEvent += PeerDisconnectedEventHandler;
            _client = new NetManager(_listener)
            {
                AutoRecycle = true
            };
            _port = port;
        }

        public void Start()
        {
            _client.Start(_port);
            Melon<Program>.Logger.Msg($"Server started on port {_port}");
        }

        public void Stop()
        {
            _client.Stop();
            Melon<Program>.Logger.Msg("Server stopped");
        }

        public void Update()
        {
            _client.PollEvents();
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
    }
}
