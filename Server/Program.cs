using System.Net;
using System.Net.Sockets;
using System.Text;

namespace POMServer
{
    public class Program
    {
        UdpClient _udpClient;
        IPEndPoint _ipEndPoint;
        Thread _receiveThread;
        IPEndPoint[] _iPEndPoints;

        public static void Main(string[] args)
        {
            new Program();
        }

        public Program()
        {
            _udpClient = new UdpClient(11000);
            _ipEndPoint = new IPEndPoint(IPAddress.Any, 11000);
            _receiveThread = new Thread(new ThreadStart(receiveWait));
            _receiveThread.Start();
        }

        void receiveWait()
        {
            while (true)
            {
                Byte[] receiveBytes = _udpClient.Receive(ref _ipEndPoint);
                Console.WriteLine(Encoding.ASCII.GetString(receiveBytes) + _ipEndPoint.Address.ToString());
                Byte[] sendBytes = Encoding.ASCII.GetBytes("Send to client data from server.");
                _udpClient.SendAsync(sendBytes, sendBytes.Length, _ipEndPoint);
            }
        }
    }
}