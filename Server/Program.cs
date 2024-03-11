using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace POMServer
{
    [Serializable]
    public class InvalidDepartmentException : Exception
    {
        public InvalidDepartmentException() : base() { }
        public InvalidDepartmentException(string message) : base(message) { }
        public InvalidDepartmentException(string message, Exception inner) : base(message, inner) { }
    }

    public class Program
    {
        IPEndPoint[] _ipEndPoints;

        public static void Main(string[] aprgs)
        {
            new Program();
        }

        public Program()
        {
            int maxClient = 10;
            _ipEndPoints = new IPEndPoint[maxClient];

            try
            {
                Thread udpThread = new Thread(new ThreadStart(UdpClient));
                udpThread.Start();

                TcpListener tcpListener = new TcpListener(IPAddress.Any, 9000);
                tcpListener.Start();

                while (true)
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();


                    Thread tcpThread = new Thread(new ParameterizedThreadStart(TcpClient));
                    tcpThread.Start(tcpClient);
                }
            } catch (Exception e)
            {
                Console.Error.WriteLine(e);
            } finally
            {

            }
        }

        void TcpClient(object? obj)
        {
            TcpClient client = (TcpClient)obj!;

            try
            {

            } catch (Exception e)
            {
                Console.Error.WriteLine(e);
            } finally
            {

            }
        }

        void UdpClient()
        {
            UdpClient udpClient = new UdpClient();
            Byte[] buffer;
            IPEndPoint receiveEP;

            try
            {
                while (true)
                {
                    buffer = new byte[1024];
                    receiveEP = new IPEndPoint(IPAddress.Any, 9000);
                    buffer = udpClient.Receive(ref receiveEP);

                    if (_ipEndPoints.Contains(receiveEP))
                    {
                        foreach (IPEndPoint ip in _ipEndPoints)
                        {
                            if (ip != receiveEP)
                                udpClient.Send(buffer, buffer.Length);
                        }
                    } else
                    {
                        Console.Error.WriteLine("There was a UDP communication from a client other than the one to which the connection was authorized.");
                    }
                }
            } catch (Exception e)
            {
                Console.Error.WriteLine(e);
            } finally
            {
                udpClient.Close();
            }
        }
    }
}