﻿using LiteNetLib;
using MessagePack;
using Serilog;
using YuchiGames.POM.Shared.DataObjects;
using System.Net;
using System.Net.Sockets;
using System.Text;
using YuchiGames.POM.Shared;
using System.Diagnostics.CodeAnalysis;

namespace YuchiGames.POM.Server.Managers
{
    public class Network
    {
        public struct ConnectedUser
        {
            public ConnectedUser() { }
            public string GUID = "";
            public int UID = NextID();


            static int s_idCounter;
            static int NextID() => s_idCounter++;
        }

        EventBasedNetListener _listener = new();

        Dictionary<NetPeer, ConnectedUser> _authorizedUsers = new();
        bool IsAuthorized(NetPeer peer) => _authorizedUsers.ContainsKey(peer);

        List<NetPeer> _waitingForAuthUsers = new();

        Task? _pollEventsTask = null;

        public int MaxPlayersCount => ServerSettings.MaxPlayers; // Can be chaged to variable later 

        public ServerSettings ServerSettings;

        NetManager _server;

        public WorldData WorldData;

        public Network(ServerSettings serverSettings)
        {
            this.ServerSettings = serverSettings;

            _server = new NetManager(_listener)
            {
                AutoRecycle = true,
                ChannelsCount = 2
            };

            WorldData = new(); // TODO: allow to load saves

            _listener.ConnectionRequestEvent += ConnectionRequestEventHandler;
            _listener.PeerConnectedEvent += PeerConnectedEventHandler;
            _listener.PeerDisconnectedEvent += PeerDisconnectedEventHandler;
            _listener.NetworkReceiveEvent += NetworkReceiveEventHandler;
            _listener.NetworkErrorEvent += NetworkErrorEventHandler;
        }

        bool ValidateConnection(AuthData authData, [NotNullWhen(false)] out string? errorMessage)
        {
            if (_authorizedUsers.Count + _waitingForAuthUsers.Count >= MaxPlayersCount)
            {
                errorMessage = "Too many connections";
                return false;
            }
            if (authData.Version != Program.Version)
            {
                errorMessage = $"Versions unmatched. Server: {Program.Version}. Client: {authData.Version}";
                return false;
            }
            errorMessage = null;
            return true;
        }

        private void ConnectionRequestEventHandler(ConnectionRequest request)
        {
            byte[] buffer = new byte[request.Data.AvailableBytes];
            request.Data.GetBytes(buffer, buffer.Length);
            AuthData authData = MessagePackSerializer.Deserialize<AuthData>(buffer);

            if (!ValidateConnection(authData, out var error))
            {
                Log.Debug($"Rejected connection from {request.RemoteEndPoint}, reason: {error}.");
                request.Reject();
                return;
            }

            Log.Debug($"Accepted connection from {request.RemoteEndPoint}.");
            request.Accept();
        }

        private void PeerConnectedEventHandler(NetPeer peer)
        {
            _waitingForAuthUsers.Add(peer);
            Log.Debug($"Connected peer with ID {peer.Id}.");
        }

        private void PeerDisconnectedEventHandler(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            _authorizedUsers.Remove(peer);
            _waitingForAuthUsers.Remove(peer);

            Log.Debug($"Disconnected peer with ID {peer.Id} {disconnectInfo.Reason}.");
            SendToAll(new LeaveMessage(peer.Id));
        }

        private void NetworkReceiveEventHandler(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            byte[] buffer = new byte[reader.AvailableBytes];
            reader.GetBytes(buffer, buffer.Length);
            NetworkReceiveEventProcess(peer, buffer, channel, deliveryMethod);
        }

        private void NetworkErrorEventHandler(IPEndPoint endPoint, SocketError socketError)
        {
            Log.Error($"A network error has occurred {socketError}.");
        }

        public void Start(int port)
        {
            _server.Start(port);
            Log.Information($"Server started at port {port}.");
            while (_server.IsRunning) // FIXME: Cancelation token or something
            {
                _server.PollEvents();
                Task.Delay(15).Wait(); // FIXME: SCARY! .Wait is a bad thing 
            }
        }

        public async Task Wait() =>
            await (_pollEventsTask ?? throw new("Start Server!")); // FIXME: SCARY! Stop using wait!

        public void Stop()
        {
            _server.Stop();
            Log.Information("Server stopped.");
        }

        private void NetworkReceiveEventProcess(NetPeer peer, byte[] buffer, byte channel, DeliveryMethod deliveryMethod)
        {
            switch (channel)
            {
                case 0x00:
                    ReceiveGameDataMessage(peer, MessagePackSerializer.Deserialize<IGameDataMessage>(buffer));
                    break;
                case 0x01:
                    ReceiveServerDataMessage(peer, MessagePackSerializer.Deserialize<IServerDataMessage>(buffer));
                    break;
            }
        }

        // TODO: Maybe??? just send for everyone that chunk is loaded, even if it still loading by another player
        Dictionary<SVector2Int, (NetPeer loader, HashSet<NetPeer> waiters)> _awaitsForChunkLoad = new();
        Dictionary<SVector2Int, HashSet<NetPeer>> _loadedChunks = new();

        /// <summary>
        /// Process Server Data Messages
        /// </summary>
        private void ReceiveServerDataMessage(NetPeer peer, IServerDataMessage serverDataMessage)
        {
            switch (serverDataMessage)
            {
                // Authorizing
                case RequestServerInfoMessage message:
                    ConnectedUser newUser = new() { GUID = message.UserGUID };
                    _authorizedUsers[peer] = newUser;
                    _waitingForAuthUsers.Remove(peer);

                    var localWorldData = LocalWorldData.From(WorldData, message.UserGUID);

                    Send(new ServerInfoMessage()
                    {
                        UID = newUser.UID,
                        IsDayNightCycle = ServerSettings.DayNightCycle,
                        WorldData = localWorldData,
                        MaxPlayers = MaxPlayersCount
                    }, peer);

                    SendToAllExcluded(new JoinMessage(peer.Id), peer);

                    Log.Information($"Peer with ID {peer.Id} was authorized.");
                    break;

                case RequestNewChunkDataMessage message:
                    // FIXME: EDGE CASE! Server request to load a chunk, but player leaves.

                    // If chunk already loaded by another player
                    if (_loadedChunks.TryGetValue(message.ChunkPos, out var loaders)) 
                    {
                        Send(new ChunkUnloadMessage() { Pos = message.ChunkPos }, peer);
                        loaders.Add(peer);
                        break;
                    }

                    // If someone at this moment loading that chunk
                    if (_awaitsForChunkLoad.TryGetValue(message.ChunkPos, out var loaderWaiter)) 
                    {
                        loaderWaiter.waiters.Add(peer);
                        break;
                    }

                    // If chunk is saved
                    if (WorldData.ChunksData.TryGetValue(message.ChunkPos, out var chunk)) 
                    {
                        Send(new SavedChunkDataMessage()
                        {
                            Chunk = chunk,
                            Pos = message.ChunkPos,
                        }, peer);

                        break;
                    }

                    // If chunk need to be generated
                    _awaitsForChunkLoad[message.ChunkPos] = (peer, new()); 
                    Send(new RequestNewChunkDataMessage() { ChunkPos = message.ChunkPos }, peer);
                    break;

                case RequestedChunkDataMessage message:
                    // When player finnaly generates a chunk after request
                    _loadedChunks[message.Pos] = new();
                    _awaitsForChunkLoad[message.Pos].waiters.ForEach(waiter =>
                    {
                        Send(new ChunkUnloadMessage() { Pos = message.Pos }, waiter);
                        _loadedChunks[message.Pos].Add(waiter);
                    });
                    break;

                case ChunkUnloadMessage message:

                    // If player unload any loaded chunk:
                    if (_loadedChunks.TryGetValue(message.Pos, out var loadedBy))
                    {
                        loadedBy.Remove(peer);
                        if (loadedBy.Count <= 0)
                            _loadedChunks.Remove(message.Pos);
                    }

                    // If player unload still loading chunk:
                    if (_awaitsForChunkLoad.TryGetValue(message.Pos, out var loader))
                        loader.waiters.Remove(peer);
                    
                    break;

                // Merge with MarkChunkLoadState? Send only by request?
                case SavedChunkDataMessage message:
                    if (!_loadedChunks.ContainsKey(message.Pos))
                        WorldData.ChunksData[message.Pos] = message.Chunk;
                    break;

                
            }
        }
        /// <summary>
        /// Process Game Data Messages
        /// </summary>
        private void ReceiveGameDataMessage(NetPeer peer, IGameDataMessage gameDataMessage)
        {
            switch (gameDataMessage)
            {
                case PlayerPositionMessage playerPosition:
                    SendToAllExcluded(new PlayerPositionUpdateMessage(playerPosition.PlayerPos, _authorizedUsers[peer].UID), peer);
                    break;

                case GroupUpdateMessage message:
                    SendToAllExcluded(message, peer);
                    break;

                case GroupDestroyedMessage message:
                    SendToAllExcluded(message, peer);
                    break;

                case GroupSetHostMessage message:
                    Log.Information($"Transfering ownership of {message.GroupID} to {message.NewHostID}");
                    SendToAll(message);
                    break;
            };
        }
        /// <summary>
        /// Sends message to everyon, except peer with id <paramref name="excludedPeerId"/>
        /// </summary>
        public void SendToAllExcluded(IDataMessage message, int excludedPeerId) =>
            SendToAllExcluded(message, _server.GetPeerById(excludedPeerId));
        /// <summary>
        /// Sends message to everyone, except <paramref name="excludedPeer"/>
        /// </summary>
        public void SendToAllExcluded(IDataMessage message, NetPeer excludedPeer) =>
            SendToAllExcluded(message, peer => peer != excludedPeer);
        /// <summary>
        /// Sends message to everyone, who satisfy <paramref name="filter"/>
        /// </summary>
        public void SendToAllExcluded(IDataMessage message, Func<NetPeer, bool> filter) =>
            _authorizedUsers.Keys.Where(filter).ForEach(peer => Send(message, peer));

        public void SendToAll(IDataMessage message) =>
            _authorizedUsers.Keys.ForEach(peer => Send(message, peer));

        // used _connectedUsers.Keys so data will be sended only to authorized users

        public void Send(IDataMessage message, int peerId) =>
            Send(message, _server.GetPeerById(peerId));
        public void Send(IDataMessage message, NetPeer peer)
        {
            byte[] data = IDataMessage.Serialize(message);

            peer.Send(data, message.Channel, message.Protocol switch
            {
                ProtocolType.Tcp => DeliveryMethod.ReliableOrdered,
                ProtocolType.Udp => DeliveryMethod.Sequenced,
                _ => throw new NotSupportedException()
            });
        }
        
    }
}