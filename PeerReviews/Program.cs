using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

//Microsoft Visual Studio Professional 2012
//Version 11.0.60315.01 Update 2
//Microsoft .NET Framework
//Version 4.5.50709



namespace assess4server
{
    public class EchoServer
    {
        public volatile bool isWorking;
        public IReactor _reactor { get; private set; }
        public IAcceptorConnector _acceptorConnector { get; private set; }
        public static Queue _queue;
        public static Queue _queueConnections;
        public Thread _reactorThread;


        /// <summary>
        /// Main Echoserver
        /// </summary>
        /// <param name="servicingThreadsCount">size of acceptor queue and thread for service connection should depends on opened socket to prevent blocking shoud be greater or equal opened sockets!</param>
        /// <param name="queueSizeforMessage">queue size for message waiting to process by main thread </param>
        public EchoServer(int servicingThreadsCount, int queueSizeforMessage)
        {
            _queueConnections = new Queue(servicingThreadsCount);
            _queue = new Queue(queueSizeforMessage);

            _acceptorConnector = new EchoAcceptorConnector(this, servicingThreadsCount, _queueConnections, _queue);
            ISynchronousEventDemultiplexer synchronousEventDemultiplexer = new SynchronousEventDemultiplexer();
            _reactor = new Reactor(this, synchronousEventDemultiplexer);

        }


        public void Start()
        {

            //register connection
            _acceptorConnector.Connect(IPAddress.Parse("127.0.0.1"), 8585);
            _acceptorConnector.Connect(IPAddress.Parse("127.0.0.1"), 8586);
            _acceptorConnector.Connect(IPAddress.Parse("127.0.0.1"), 8587);

            isWorking = true;


            _reactorThread = new Thread(_reactor.HandleEvents);
            _reactorThread.Start();
            MainThread();
            //start handle client

        }

        //process message and send back information, close connection if message is ended by "end" keyword
        private void MainThread()
        {
            while (isWorking)
            {
                var message = _queue.Dequeue();
                if (message != null)
                {
                    ASCIIEncoding encoder = new ASCIIEncoding();
                    message._socket.Send(encoder.GetBytes(message.Message));

                    //message._socket.Close();
                    if (message.Message.ToLower().EndsWith("end", StringComparison.InvariantCultureIgnoreCase))
                        message._socket.Close();
                }
            }
        }
    }

    class Program
    {
        private static Thread serverT;

        static void Main(string[] args)
        {
            var server = new EchoServer(4, 10);

            //start EchoServer thread
            serverT = new Thread(server.Start) { IsBackground = true };
            serverT.Start();


            //wait to kill server
            Console.ReadLine();



        }
    }

    public class Reactor : IReactor
    {
        private readonly EchoServer echoServer;
        private readonly ISynchronousEventDemultiplexer _synchronousEventDemultiplexer;
        private readonly IDictionary<TcpListener, IEventHandler> _handlers;
        public bool IsWorking;

        private IAcceptorConnector AcceptorConnector
        {
            get { return echoServer._acceptorConnector; }
        }

        public Reactor(EchoServer echoServer, ISynchronousEventDemultiplexer synchronousEventDemultiplexer)
        {
            this.echoServer = echoServer;
            _synchronousEventDemultiplexer = synchronousEventDemultiplexer;
            _handlers = new Dictionary<TcpListener, IEventHandler>();
        }

        public void RegisterHandle(TcpListener listener)
        {
            _handlers.Add(listener, null);
            listener.Start();
        }

        public void RemoveHandle(IEventHandler eventHandler)
        {
            _handlers.Remove(eventHandler.GetHandle());
        }

        public void HandleEvents()
        {
            IsWorking = true;
            while (IsWorking)
            {
                IList<TcpListener> listeners = _synchronousEventDemultiplexer.Select(_handlers.Keys);

                foreach (TcpListener listener in listeners)
                {
                    var socket = AcceptorConnector.Accept(listener);
                    if (socket != null)
                    {
                        MessageEventHandler messageEventHandler = AcceptorConnector.HandleEvent(listener);
                        messageEventHandler._socket = socket;
                        _handlers[listener] = messageEventHandler;
                        AcceptorConnector.GetHandle((MessageEventHandler)_handlers[listener]);
                    }
                }
            }
        }
    }

    public interface IEventHandler
    {
        bool HandleEvent(string threadName);
        TcpListener GetHandle();
    }

    public interface IReactor
    {
        void RegisterHandle(TcpListener eventHandler);
        void RemoveHandle(IEventHandler eventHandler);
        void HandleEvents();
    }


    //wraperFacade for connection
    public class MessageEventHandler : IEventHandler
    {
        //socket with data to read
        public Socket _socket;

        public TcpListener _listener;

        public MessageEventHandler(TcpListener listener)
        {
            _listener = listener;
        }


        private string _message;
        public string Message
        {
            get
            {
                return _message;
            }
            set { _message += value; }
        }

        //read form socket
        public bool HandleEvent(string threadName)
        {
            try
            {
                _message = string.Empty;
                Message = threadName + ": ";
                int dataReceived = 0;
                byte[] buffer = new byte[1024];

                do
                {
                    dataReceived = _socket.Receive(buffer);

                    if (dataReceived > 0)
                    {
                        string currentchar = Encoding.ASCII.GetString(buffer, 0, dataReceived);

                        if (currentchar.EndsWith(Environment.NewLine))
                            return true;

                        Message = currentchar;
                    }

                } while (dataReceived > 0 || _socket.Connected);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public TcpListener GetHandle()
        {
            return _listener;
        }
    }

    public interface ISynchronousEventDemultiplexer
    {
        IList<TcpListener> Select(ICollection<TcpListener> listeners);
    }

    public class SynchronousEventDemultiplexer : ISynchronousEventDemultiplexer
    {
        public IList<TcpListener> Select(ICollection<TcpListener> listeners)
        {
            var tcpListeners =
                new List<TcpListener>(from listener in listeners
                                      where listener.Pending()
                                      select listener);
            return tcpListeners;
        }
    }

    public interface IAcceptorConnector
    {
        Socket Accept(TcpListener listener);
        void GetHandle(MessageEventHandler connection);
        MessageEventHandler HandleEvent(TcpListener listener);

        void Connect(IPAddress ip, int port);
    }

    public class EchoAcceptorConnector : IAcceptorConnector
    {
        private readonly EchoServer echoServer;
        private readonly int serThreads;

        private readonly EchoThreadsPool threadsPool;

        public EchoAcceptorConnector(EchoServer echoServer, int servicingThreadsCount, Queue connectionToServe, Queue messageToProceed)
        {
            this.echoServer = echoServer;
            serThreads = servicingThreadsCount;

            threadsPool = new EchoThreadsPool(serThreads, connectionToServe, messageToProceed);
            threadsPool.Start();
        }

        public IReactor Dispatcher
        {
            get { return echoServer._reactor; }
        }


        //always accept connection
        public Socket Accept(TcpListener listener)
        {
            return listener.AcceptSocket();
        }


        //push connection to read
        public void GetHandle(MessageEventHandler connection)
        {
            threadsPool.sharedQueue.Enqueue(connection);
        }

        //Handle connection use wrapperFacade
        public MessageEventHandler HandleEvent(TcpListener listener)
        {
            return new MessageEventHandler(listener);
        }


        public void Connect(IPAddress ip, int port)
        {
            //IEventHandler client1 = new MessageEventHandler(ip, port);
            var listener = new TcpListener(ip, port);
            Dispatcher.RegisterHandle(listener);
        }
    }



    /// <summary>
    /// Synchronized queue class for received message from clients or messages ready for process by main thread! ;)
    /// </summary>
    public class Queue
    {
        //semaphores to prevent thread race
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private SemaphoreSlim semaphoreDq;
        private SemaphoreSlim semaphoreEq;


        private readonly ConcurrentQueue<MessageEventHandler> queue = new ConcurrentQueue<MessageEventHandler>();

        public Queue(int size)
        {

            semaphoreDq = new SemaphoreSlim(0, size);
            semaphoreEq = new SemaphoreSlim(size, size);
        }

        public void Enqueue(MessageEventHandler eventHandler)
        {

            semaphoreEq.Wait();

            semaphore.Wait();

            queue.Enqueue(eventHandler);
            semaphoreDq.Release(1);
            semaphore.Release();

            //only to show u thread can change
            Thread.Sleep(500);

        }

        public MessageEventHandler Dequeue()
        {
            semaphoreDq.Wait();



            semaphore.Wait();
            MessageEventHandler meh;

            while (!queue.TryDequeue(out meh)) { }

            semaphoreEq.Release(1);
            semaphore.Release();

            //only to show u thread can change
            Thread.Sleep(500);
            return meh;
        }
    }

    public class EchoThreadsPool
    {
        private readonly int threadsCount;

        public readonly Queue sharedQueue;
        public readonly Queue sharedMessageQueue;

        public volatile bool isWorking;
        private Thread[] _mainThreads;

        public EchoThreadsPool(int threadsCount, Queue sharedConnectioQueue, Queue sharedMessageQueue)
        {
            this.threadsCount = threadsCount;
            this.sharedQueue = sharedConnectioQueue;
            this.sharedMessageQueue = sharedMessageQueue;
        }

        public void Start()
        {
            isWorking = true;
            _mainThreads = Enumerable.Range(0, threadsCount).Select(i => new Thread(readFromSocket)).ToArray();

            for (int i = 0; i < threadsCount; i++)
            {
                _mainThreads[i].Name = "ServiceThread: " + (i + 1);
                _mainThreads[i].IsBackground = true;
                _mainThreads[i].Start();
            }
        }

        private void readFromSocket()
        {
            while (isWorking)
            {
                //get waiting connection to service
                var handler = sharedQueue.Dequeue();
                //read from socket
                var res = handler.HandleEvent(Thread.CurrentThread.Name);

                if (res)
                {
                    // put data for processing by mainThread
                    sharedMessageQueue.Enqueue(new MessageEventHandler(handler._listener)
                    {
                        _socket = handler._socket,
                        Message = handler.Message
                    });

                    //if still connected give it to another thread to read
                    if (handler._socket.Connected)
                        sharedQueue.Enqueue(handler);
                }
            }
        }
    }
}
