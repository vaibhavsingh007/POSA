using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NonBlockingSync
{
    class Program
    {
        static void Main(string[] args)
        {
            TheTestClass testClass = new TheTestClass();

            for (int i = 0; i <= 10; i++)
            {
                int temp = i;
                Console.WriteLine("Spawned thread # " + temp);
                new Thread(() => testClass.AddString(temp.ToString())).Start();
            
                new Thread(() => Console.WriteLine(testClass.ToString())).Start();
            }

            Console.ReadLine();
        }
    }

    /// <summary>
    /// Implements Non-Blocking Synchronization
    /// </summary>
    class TheTestClass
    {
        List<string> internalCollection;
        bool _proceed;

        public TheTestClass()
        {
            internalCollection = new List<string>();
            _proceed = true;    // Condition variable to maintain synchronization
        }

        /// <summary>
        /// Thread safe method
        /// </summary>
        /// <param name="stringToAdd"></param>
        public void AddString(string stringToAdd)
        {
            // In order to avoid locks, SpinWait is used.
            SpinWait.SpinUntil(() => {Thread.MemoryBarrier(); return _proceed;});
            _proceed = false;
            Console.WriteLine(String.Format("Thread {0} inserting {1}",
                Thread.CurrentThread.ManagedThreadId, stringToAdd));

            internalCollection.Add(stringToAdd);
            Thread.MemoryBarrier();
            Console.WriteLine(String.Format("Thread {0} inserted {1}", 
                Thread.CurrentThread.ManagedThreadId, stringToAdd));
            _proceed = true;
        }

        /// <summary>
        /// Allows concurrent access using memory fence.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            // Apply fence for concurrent read-only access to maintain
            // variable freshness.
            Thread.MemoryBarrier();
            return String.Join<string>(",", internalCollection);
        }
    }
       
}
