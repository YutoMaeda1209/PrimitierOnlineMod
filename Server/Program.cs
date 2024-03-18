using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace POMServer
{
    public class Program
    {
        IPEndPoint?[] _ipEndPoints;

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
            } catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

            Console.WriteLine("Stopped MainThread");
        }

        void ListenTcp()
        {
            Console.WriteLine("Running ListenTcpThread");

            TcpListener listener = new TcpListener(IPAddress.Any, 54162);

            try
            {
                listener.Start();
                while (true)
                {
                    Console.WriteLine("Waiting tcp connection");
                    TcpClient client = listener.AcceptTcpClient();
                    Console.WriteLine($"Accepted tcp connection: {(IPEndPoint)client.Client.RemoteEndPoint!}");
                    ThreadPool.QueueUserWorkItem(ProcessTcp, client);
                    Console.WriteLine("Created ProcessTcpThread");
                }
            } catch (Exception e)
            {
                Console.Error.WriteLine(e);
            } finally
            {
                listener.Stop();
            }

            Console.WriteLine("Stopped ListenTcpThread");
        }

        void ProcessTcp(object? obj)
        {
            Console.WriteLine("Running ProcessTcpThread");

            try
            {
                TcpClient client = (TcpClient)obj!;
                IPEndPoint ipEndPoint = (IPEndPoint)client.Client.RemoteEndPoint!;
                NetworkStream stream = client.GetStream();
                byte[] bytes = new byte[1];

                stream.Read(bytes, 0, bytes.Length);
                Console.WriteLine($"Received data: {bytes}, {ipEndPoint}");

                if (bytes[0] == 0x1)
                {
                    if (_ipEndPoints.Contains(ipEndPoint))
                        return;
                    for (int i = 0; i < _ipEndPoints.Length; i++)
                    {
                        if (_ipEndPoints[i] != default)
                        {
                            _ipEndPoints[i] = ipEndPoint;
                            break;
                        }
                    }
                    Console.WriteLine($"Accepted server connection: {ipEndPoint}");
                } else if (bytes[0] == 0x2)
                {
                    if (!_ipEndPoints.Contains(ipEndPoint))
                        return;
                    for (int i = 0; i < _ipEndPoints.Length; i++)
                    {
                        if (_ipEndPoints[i] == ipEndPoint)
                        {
                            _ipEndPoints[i] = null;
                            break;
                        }
                    }
                    Console.WriteLine($"Accepted server disconnection: {ipEndPoint}");
                }
            } catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

            Console.WriteLine("Stopped ProcessTcpThread");
        }

        void ListenUdp()
        {
            Console.WriteLine("Running ListenUdpThread");

            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 54162);
            using UdpClient listener = new UdpClient(ipEndPoint);

            try
            {
                while (true)
                {
                    Console.WriteLine("Waiting udp connection");
                    IPEndPoint? remoteEndPoint = null;
                    byte[] receiveData = listener.Receive(ref remoteEndPoint);
                    Console.WriteLine($"Received data: {receiveData}, {remoteEndPoint}");
                    if (_ipEndPoints.Contains(ipEndPoint))
                    {
                        object[] obj = { remoteEndPoint, receiveData };
                        ThreadPool.QueueUserWorkItem(ProcessUdp, obj);
                        Console.WriteLine("Created ProcessUdpThread");
                    }
                }
            } catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

            Console.WriteLine("Stopped ListenUdpThread");
        }

        void ProcessUdp(object? obj)
        {
            Console.WriteLine("Running ProcessUdpThread");

            using UdpClient sender = new UdpClient();

            try
            {
                IPEndPoint remoteEndPoint = (IPEndPoint)((object[])obj!)[0];
                byte[] receiveData = (byte[])((object[])obj!)[1];

                for (int i = 0; i < _ipEndPoints.Length; i++)
                {
                    if (_ipEndPoints[i] == remoteEndPoint)
                        continue;
                    sender.Send(receiveData, receiveData.Length, _ipEndPoints[i]);
                    Console.WriteLine($"Sent data: {receiveData}, {_ipEndPoints[i]}");
                }
            } catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

            Console.WriteLine("Stopped ProcessUdpThread");
        }
    }
}