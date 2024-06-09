﻿using System.Net.Sockets;
using MessagePack;
using YuchiGames.POM.Client.Data;

namespace YuchiGames.POM.Client.Network
{
    public class Senders
    {
        public ITcpMessage Tcp(ITcpMessage message)
        {
            try
            {
                byte[] buffer = new byte[1024];
                buffer = MessagePackSerializer.Serialize(message);

                using (TcpClient client = new TcpClient(Program.Settings.IP, Program.Settings.Port))
                using (NetworkStream stream = client.GetStream())
                {
                    stream.Write(buffer, 0, buffer.Length);
                    stream.Read(buffer, 0, buffer.Length);

                    message = MessagePackSerializer.Deserialize<ITcpMessage>(buffer);
                    if (message is FailureMessage)
                        throw new Exception("Failure message received.");

                    return message;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Udp(IUdpMessage message)
        {
            try
            {
                byte[] buffer = new byte[1024];
                buffer = MessagePackSerializer.Serialize(message);

                using (UdpClient client = new UdpClient(Program.Settings.IP, Program.Settings.Port))
                {
                    client.Send(buffer, buffer.Length);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}