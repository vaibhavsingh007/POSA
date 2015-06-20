// 20132104: .Net Framework 4.0, C# compiler vr 4.0.30319.17929
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReactorPatternTest
{
    /// <summary>
    /// This program tests the 'Reactor' server by spawning 3 clients
    /// concurrently that send a message to the server to be echoed back
    /// almost instantly without blocking any client.
    /// Please start the server before executing this program.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Create three test clients and start them concurrently
            Clients client1 = new Clients(IPAddress.Parse("127.0.0.1"), 123);
            Clients client2 = new Clients(IPAddress.Parse("127.0.0.1"), 123);
            Clients client3 = new Clients(IPAddress.Parse("127.0.0.1"), 123);
            Thread t1 = new Thread(client1.TestClient);
            Thread t2 = new Thread(client2.TestClient);
            Thread t3 = new Thread(client3.TestClient);
            t1.Start();
            t2.Start();
            t3.Start();
            t1.Join();
            t2.Join();
            t3.Join();

            Console.ReadLine();
            Console.WriteLine("Press Enter to exit..");
        }

        
    }

    public class Clients
    {
        private readonly IPAddress _ip;
        private readonly int _port;

        public Clients(IPAddress ip, int port)
        {
            _ip = ip;
            _port = port;
        }

        public void TestClient()
        {
            // Create a client to connect to the server and then recieve the message
            // in a stream as:
            TcpClient client = new TcpClient();
            NetworkStream stream = null;
            Console.WriteLine("Started client on thread: " + Thread.CurrentThread.ManagedThreadId);
            try
            {
                client.Connect(_ip, _port);

                // This is the way to read from the server stream
                stream = client.GetStream();
                byte[] sendBuffer = Encoding.UTF8.GetBytes("Hi server! This is client on port:" + _port + "\n");
                stream.Write(sendBuffer, 0, sendBuffer.Length);

                byte[] recieveBuffer = new byte[1024];
                int recieved = stream.Read(recieveBuffer, 0, 1024);
                Console.WriteLine(Encoding.UTF8.GetString(recieveBuffer).Trim('\0'));
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }

                if (client.Connected)
                {
                    client.Close();
                }
            }
        }
    }
}
