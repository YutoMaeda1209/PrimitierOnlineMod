﻿namespace YuchiGames.POM.DataTypes
{
    public class ClientSettings
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }
        public int ListenPort { get; set; }

        public ClientSettings(string name, string version, string ip, int port, int listenPort)
        {
            Name = name;
            Version = version;
            IP = ip;
            Port = port;
            ListenPort = listenPort;
        }
    }
}