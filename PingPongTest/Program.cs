// 20131304: .Net Framework 4.0, C# compiler vr 4.0.30319.17929
using System;
using System.Threading;

namespace ThePingPongApp
{
    /// <summary>
    /// Used wait and pulse in order to implement the
    /// required alternating printing of 'ping' and 'pong'
    /// from two concurrent threads.
    /// </summary>
    class Program
    {
        static readonly object _locker = new object();
        static bool _donePing = false;
        static bool _donePong = false;

        static void Main(string[] args)
        {
            Console.WriteLine("Ready… Set… Go!\n");

            // Spawn two threads ping and pong
            Thread ping = new Thread(Ping);
            Thread pong = new Thread(Pong);
            ping.Start();
            pong.Start();

            // Wait for the threads to complete
            ping.Join();
            pong.Join();
            Console.WriteLine("Done!!");
            Console.WriteLine("\nPress enter to exit..");
            Console.ReadLine();
        }

        static void Ping()
        {
            for (int i = 0; i <= 2; i++)
            {
                lock (_locker)  // For maintaining synchronization..
                {
                    while (!_donePong) Monitor.Wait(_locker);   // Wait till pong is done
                    Console.WriteLine("Ping!");
                    _donePing = true;               // ping is done
                    Monitor.PulseAll(_locker);      // release/notify/pulse pong
                    _donePong = false;
                }
            }
        }

        static void Pong()
        {
            for (int i = 0; i <= 2; i++)
            {
                lock (_locker)  // ..when there are more than one threads
                {
                    _donePong = true;               // pong is done (to prevent deadlock)
                    Monitor.PulseAll(_locker);      // release/notify/pulse ping
                    while (!_donePing) Monitor.Wait(_locker);   // Wait for ping
                    Console.WriteLine("Pong!");
                    _donePing = false;
                }
            }
        }
    }
}