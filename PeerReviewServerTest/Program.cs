using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Coursera.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            string message = "Hello world!";

            using (var socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect("127.0.0.1", 8585);

                using (var stream = new NetworkStream(socket))
                using (var reader = new StreamReader(stream))
                using (var writer = new StreamWriter(stream))
                {
                    Console.WriteLine("Sending message: " + message);
                    writer.WriteLine(message);
                    writer.Flush();
                    Console.WriteLine("Received message: " + reader.ReadLine());
                }
            }
            Console.ReadLine();
        }
    }
}

