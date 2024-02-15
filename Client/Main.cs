using MelonLoader;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace POMClient
{
    public class Program : MelonMod
    {
        UdpClient _udpClient;
        IPEndPoint _ipEndPoint;

        public Program()
        {
            _udpClient = new UdpClient();
            _ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11000);
        }

        public override void OnInitializeMelon()
        {
            _udpClient.Connect(_ipEndPoint);
            receiveWait();
            Byte[] sendBytes = Encoding.ASCII.GetBytes("Send to server data from client.");
            _udpClient.Send(sendBytes, sendBytes.Length);
        }

        Task receiveWait() {
            while (true)
            {
                _udpClient.Receive(ref _ipEndPoint);
                MelonDebug.Msg(_ipEndPoint.ToString());
            }
        }
    }
}
