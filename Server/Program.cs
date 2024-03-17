using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace POMServer
{
    public class Program
    {
        IPEndPoint[] _ipEndPoints;

        public static void Main(string[] args)
        {
            new Program();
        }

        public Program()
        {
            Console.WriteLine("Running MainThread");
            _ipEndPoints = new IPEndPoint[10];

            try
            {
                Thread tcpThread = new Thread(new ThreadStart(ListenTcp));
                tcpThread.Start();
                Thread udpThread = new Thread(new ThreadStart(ListenUdp));
                udpThread.Start();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
            Console.WriteLine("Stopping MainThread");
        }

        void ListenTcp()
        {
            Console.WriteLine("Running ListenTcpThread");
            TcpListener listener = new TcpListener(IPAddress.Any, 9000);
            TcpClient client;

            try
            {
                listener.Start();
                Console.WriteLine($"Started listening: {listener.LocalEndpoint}");
                while (true)
                {
                    Console.WriteLine("Waiting connection");
                    client = listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(ProcessTcp, client);
                    Console.WriteLine($"Accepted Tcp Data: {client}");
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
            finally
            {
                listener.Stop();
            }
            Console.WriteLine("Stopping ListenTcpThread");
        }

        void ProcessTcp(object? obj)
        {
            Console.WriteLine("Running ProcessTcpThread");
            TcpClient client = (TcpClient)obj!;
            NetworkStream stream = client.GetStream();
            byte[] bytes = new byte[1];

            try
            {
                stream.Read(bytes, 0, bytes.Length);
                Console.WriteLine($"Received data: {bytes}");

                if (bytes[0] == 0x1)
                {
                    IPEndPoint ipEndPoint = (IPEndPoint)client.Client.RemoteEndPoint!;
                    if (_ipEndPoints.Contains(ipEndPoint))
                    for (int i = 0; i < _ipEndPoints.Length; i++)
                    {
                        if (_ipEndPoints[i] != default)
                        {
                            _ipEndPoints[i] = ipEndPoint;
                            Console.WriteLine($"Add IPEndPoint: {i}");
                        }
                    }
                } else if (bytes[0] == 0x2)
                {

                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

        void ListenUdp()
        {
            try
            {

            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

        void ProcessUdp(object? obj)
        {
            UdpClient client = (UdpClient)obj!;

            try
            {

            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }
    }
}