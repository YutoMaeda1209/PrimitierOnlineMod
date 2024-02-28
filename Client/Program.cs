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
        Thread _receiveThread;

        public Program()
        {
            _udpClient = new UdpClient();
            _ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11000);
        }

        public override void OnLateInitializeMelon()
        {
            _udpClient.Connect(_ipEndPoint);
            _receiveThread = new Thread(new ThreadStart(receiveWait));
            _receiveThread.Start();
            Byte[] sendBytes = Encoding.ASCII.GetBytes("Send to server data from client.");
            _udpClient.SendAsync(sendBytes, sendBytes.Length);
        }

        void receiveWait() {
            while (true)
            {
                Byte[] receiveBytes = _udpClient.Receive(ref _ipEndPoint);
                LoggerInstance.Msg(Encoding.ASCII.GetString(receiveBytes));
            }
        }
    }
}
