using System.Net;
using System.Net.Sockets;
using System.Text;

namespace POMServer
{
    public class Program
    {
        UdpClient _udpClient;
        IPEndPoint _ipEndPoint;

        public static void Main(string[] args)
        {
            new Program();
        }

        public Program()
        {
            Console.WriteLine("Hello, world");
            _udpClient = new UdpClient(11000);
            _ipEndPoint = new IPEndPoint(IPAddress.Any, 11000);
            receiveWait();
        }

        Task receiveWait()
        {
            while (true)
            {
                _udpClient.Receive(ref _ipEndPoint);
                Console.Write(_ipEndPoint.ToString());
                Byte[] sendBytes = Encoding.ASCII.GetBytes("Send to client data from server.");
                _udpClient.Send(sendBytes, sendBytes.Length, _ipEndPoint);
            }
        }
    }
}