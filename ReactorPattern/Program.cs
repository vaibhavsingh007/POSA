// 20132104: .Net Framework 4.0, C# compiler vr 4.0.30319.17929
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ReactorPattern
{
    /// <summary>
    /// This is an echo server implemented using the
    /// responsive REACTOR pattern that listens to 3
    /// clients concurrently on three different ports.
    /// It uses - handler registration, synchronous event
    /// demultiplexing, initiation dispatcher to dispatch
    /// the requests and handler callbacks in order to
    /// service the client requests.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Create three event handlers
            IEventHandler client1 = new MessageEventHandler(IPAddress.Parse("127.0.0.1"), 123);
            IEventHandler client2 = new MessageEventHandler(IPAddress.Parse("127.0.0.1"), 124);
            IEventHandler client3 = new MessageEventHandler(IPAddress.Parse("127.0.0.1"), 125);

            ISynchronousEventDemultiplexer synchronousEventDemultiplexer = new SynchronousEventDemultiplexer();

            // Create an initiation dispatcher by passing an instance of the
            // synchronous event demultiplexer.
            Reactor dispatcher = new Reactor(synchronousEventDemultiplexer);

            // Register handles to the dispatcher.
            dispatcher.RegisterHandle(client1);
            dispatcher.RegisterHandle(client2);
            dispatcher.RegisterHandle(client3);

            Console.WriteLine("Server started!\n");
            dispatcher.HandleEvents();
        }
    }

    #region ReactorInterfaces

    // Define the Event Handler Interfac
    // These interfaces also exhibit the implementation of the wrapper facades
    // making use of the Reactor framework under the hood.
    public interface IEventHandler
    {
        void HandleEvent(byte[] data, Socket socket);
        TcpListener GetHandler();
    }

    // Define the Synchronous Event Demultiplexer interface
    public interface ISynchronousEventDemultiplexer
    {
        IEnumerable<TcpListener> Select(ICollection<TcpListener> listeners);
    }

    // Define the Reactor/InitiationDispatcher interface
    /// <summary>
    /// This interface will later exhibit the implementation of the acceptor pattern
    /// by delegating the task of accepting the client requests and connections to 
    /// the Acceptor class.
    /// </summary>
    public interface IReactor
    {
        void RegisterHandle(IEventHandler eventHandler);
        void RemoveHandle(IEventHandler eventHandler);
        void HandleEvents();
    } 
    #endregion

    #region InterfaceImplementation

    /* Now we have the concrete implementation of the IEventHandler
     * which creates an instance of our TcpListener handle. 
     * It also has methods for returning the Handle and handling 
     * the event for a message arriving.
     */
    public class MessageEventHandler : IEventHandler
    {
        private TcpListener _listener;

        public MessageEventHandler(IPAddress ip, int port)
        {
            // A new handle
            _listener = new TcpListener(ip, port);
            _listener.Start();
            Console.WriteLine("Listening on port: " + port);
        }

        public void HandleEvent(byte[] data, Socket socket)
        {
            string message = "Server response [Echo]: " + Encoding.UTF8.GetString(data);
            byte[] responseBuffer = Encoding.UTF8.GetBytes(message);
            socket.Send(responseBuffer, responseBuffer.Length, 0);
            // PS: A handler may also implement another method to be called ex: Echo(), instead!
        }

        public TcpListener GetHandler()
        {
            // Return the handle
            return _listener;
        }
    }


    /* Implemenation of the Synchronous Event Demultiplexer
     * which returns TcpListeners which are ready to be processed.
     */
    public class SynchronousEventDemultiplexer : ISynchronousEventDemultiplexer
    {

        public IEnumerable<TcpListener> Select(ICollection<TcpListener> listeners)
        {
            var tcpListeners = listeners.Where(l => l.Pending());
            return tcpListeners;
        }
    }

    /* Implementing InitiationDispatcher/Reactor.
     * This takes a Synchronous Event Demultiplexer for getting handles 
     * that are ready to be processes. It also creates a IDictionary to 
     * store all its handles and EventHandlers.
     */
    public class Reactor : IReactor
    {
        private readonly ISynchronousEventDemultiplexer _synchronousEventDemultiplexer;
        private readonly IDictionary<TcpListener, IEventHandler> _handlers;

        public Reactor(ISynchronousEventDemultiplexer synchronousEventDemultiplexer)
        {
            _synchronousEventDemultiplexer = synchronousEventDemultiplexer;
            _handlers = new Dictionary<TcpListener, IEventHandler>();
        }

        public void RegisterHandle(IEventHandler eventHandler)
        {
            _handlers.Add(eventHandler.GetHandler(), eventHandler);
        }

        public void RemoveHandle(IEventHandler eventHandler)
        {
            _handlers.Remove(eventHandler.GetHandler());
        }

        public void HandleEvents()
        {
            // An instance of Acceptor
            Acceptor acceptor = new Acceptor();
            for (; ; )
            {
                try
                {
                    IEnumerable<TcpListener> listeners = _synchronousEventDemultiplexer.Select(_handlers.Keys);

                    foreach (TcpListener listener in listeners)
                    {
                        // Dispatch service handler and handle to acceptor
                        acceptor.HandleInput(listener, _handlers[listener]);
                        Console.WriteLine("Response sent on: " + listener.LocalEndpoint.ToString());
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine("{0} Error code: {1}.", e.Message, e.ErrorCode);
                }
            }
        }
    } 
    #endregion

    /// <summary>
    /// Singleton Acceptor
    /// </summary>
    public class Acceptor
    {
        private static Int16 _singletonCounter = 0;

        /// <summary>
        /// Instantiates a singleton instance for the socket acceptor.
        /// </summary>
        /// <param name="listener"></param>
        /// <returns></returns>
        public Acceptor GetSingletonInstance()
        {
            if (_singletonCounter == 0)
            {
                _singletonCounter++;
                return new Acceptor();
            }
            else
            {
                return this;
            }
        }

        public void HandleInput(TcpListener listener,IEventHandler handler)
        {
            int dataReceived = 0;
            byte[] buffer = new byte[256];
            IList<byte> data = new List<byte>();

            Socket socket = listener.AcceptSocket();
            Console.WriteLine("Listening..");
            do
            {
                // Accomodates only as much is the capacity
                // of the buffer. Rest is pending in the stream
                // be consumed in the next iteration.
                dataReceived = socket.Receive(buffer);

                if (dataReceived > 0)
                {
                    for (int i = 0; i < dataReceived; i++)
                    {
                        data.Add(buffer[i]);
                    }
                }
                // Recieving and sending data in chunks.
            } while (dataReceived == buffer.Length);

            // Call the EventHandler's HandleEvent() method.
            // This is apparently the Echo Server Handler
            handler.HandleEvent(data.ToArray(), socket);
            socket.Close();
        }
    }
}

