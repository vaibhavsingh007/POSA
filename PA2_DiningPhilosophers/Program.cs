// 20131404: .Net Framework 4.0, C# compiler vr 4.0.30319.17929
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PA2_DiningPhilosophers
{
    /// <summary>
    /// This implementation exemplifies 'solution #2' representation of 
    /// Petri Nets for PA2 on coursera wiki - 
    /// https://share.coursera.org/wiki/index.php/Posa:Philosophers_problem#Analysis
    /// Threadpool has been used to spawn threads. This can also be replaced with the
    /// naive individual-thread spawning. :)
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Spawn five philosophers to begin the dinner.
            const int totalPhilosophers = 5;
            const int eatCount = 5; // This may be reduced to 1 for simplicity.

            // Create 'one' (shared) instance of Chopstick(synchronized monitor object)
            ChopStick chopstick = new ChopStick(totalPhilosophers);

            // One event used for each philosopher who completes the dinner.
            ManualResetEvent[] doneEvents = new ManualResetEvent[totalPhilosophers];
            Console.WriteLine("Dinner is starting!\n");
            for (int i = 1; i <= totalPhilosophers; i++)
            {
                doneEvents[i-1] = new ManualResetEvent(false);
                Philosopher philosopher = new Philosopher(i, doneEvents[i-1], chopstick, eatCount);
                //Console.WriteLine("Queueing philosopher " + i);   // Uncomment to display queueing.
                ThreadPool.QueueUserWorkItem(philosopher.StartDining);
            }

            // Wait for the dinner to complete
            WaitHandle.WaitAll(doneEvents);
            Console.WriteLine("\nDinner is over!");
            Console.WriteLine("Press enter to exit..");
            Console.ReadLine();
        }
    }

    class Philosopher
    {
        private readonly int _id;
        private readonly int _eatCount;
        private ChopStick _chopSticks;
        private ManualResetEvent _doneEvent;

        public Philosopher(int philosopherId, ManualResetEvent doneEvent,
                            ChopStick chopSticks, int eatCount)
        {
            _id = philosopherId;
            _doneEvent = doneEvent;
            _chopSticks = chopSticks;
            _eatCount = eatCount;
        }

        // Wrapper method for use with threadpool.
        public void StartDining(Object threadContext)
        {
            int eats = 0;
            string whichChopstick;
            while (eats != _eatCount)
            {
                // Pick two chopsticks
                for (int i = 0; i <= 1; i++)
                {
                    whichChopstick = i == 0 ? "left chopstick." : "right chopstick.";
                    _chopSticks.PickChopstick(_id);
                    Console.WriteLine("Philosopher " + _id + " picks up " + whichChopstick);
                }
                eats++;
                Console.WriteLine("\nPhilosopher " + _id + " EATS.\n");

                // Put down both chopsticks after eating once
                for (int i = 0; i <= 1; i++)
                {
                    whichChopstick = i == 0 ? "left chopstick." : "right chopstick.";
                    _chopSticks.PutChopstick(_id);
                    Console.WriteLine("Philosopher " + _id + " puts down " + whichChopstick);
                }
            }
            _doneEvent.Set();   // Signal that this philosopher finished the dinner.
        }
    }

    /// <summary>
    /// Simulates the synchronized monitor object
    /// </summary>
    class ChopStick
    {
        // Though the monitor object pattern implements a queue to synchronize
        // messages, here we may get the job done using a synchronized counter
        // in the monitored object (ex: _chopsticksLeft).

        #region PrivateFields

        // Implementing the monitor object's internal methods and fields.
        private int _chopsticksLeft;
        private readonly int _maxChopsticks;
        private bool _safePick;
        private IList<int> _philosophersWithChopsticks;
        private readonly object _locker; 
        #endregion

        #region ctor

        public ChopStick(int maxChopsticks)
        {
            _maxChopsticks = maxChopsticks;
            _chopsticksLeft = maxChopsticks;
            _locker = new object();
            _safePick = true;
            _philosophersWithChopsticks = new List<int>();
        } 
        #endregion

        public void PickChopstick(int philosopherId)
        {
            lock (_locker)
            {
                // Release the lock and wait(suspend) if no chopsticks available or
                // wait if not _safePick : preventing deadlock.
                // Review ManageChopsticks() for more info
                ManageChopsticks(philosopherId);
                while ((_chopsticksLeft == 0) || !_safePick) Monitor.Wait(_locker);
                _philosophersWithChopsticks.Add(philosopherId);
                _chopsticksLeft--;
            }
        }

        public void PutChopstick(int philosopherId)
        {
            lock (_locker)
            {
                // Remove this philosopher from list
                _philosophersWithChopsticks.Remove(philosopherId);
                _chopsticksLeft++;
                Monitor.Pulse(_locker); // Release thread waiting on 0 Chopstick/!_safePick
            }
        }

        // Maintain private state of the object
        /* If all but 1 chopsticks were picked by disparate philosophers,
           restrict the next disparate to pick the last chopstick in order to 
           avoid deadlock.
         */
        private void ManageChopsticks(int philosopherId)
        {
            lock (_locker)  // Nested lock (reentrancy)
            {
                if (_chopsticksLeft == 1)
                {
                    // Check if all philosophers have picked left(one) chopstick each:
                    // Avoid deadlock.
                    _safePick = false;
                    _philosophersWithChopsticks.Add(philosopherId);

                    // If _philosophersWithChopsticks has less than the number of
                    // total philosophers at this moment, then it has reached via
                    // a pulse. Hence, safe pick.
                    if (_philosophersWithChopsticks.Count < _maxChopsticks)
                    {
                        _safePick = true;
                        _philosophersWithChopsticks.Remove(philosopherId);
                        return;
                    }

                    foreach (int philosopher in _philosophersWithChopsticks)
                    {
                        if (_philosophersWithChopsticks.Count(p => p == philosopher) > 1)
                        {
                            _safePick = true;
                            break;
                            //_chopsticksLeft--;
                        }
                    }
                    // Remove the last philosopher from list if pick not allowed
                    _philosophersWithChopsticks.Remove(philosopherId);
                }
            }
        }


    }
}
