using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BlockingCollectionTrial
{
    class BlockingCollectionDemo
    {
        static void Main()
        {
            //AddTakeDemo.BC_AddTakeCompleteAdding();
            //TryTakeDemo.BC_TryTake();
            //FromToAnyDemo.BC_FromToAny();
            ConsumingEnumerableDemo.BC_GetConsumingEnumerable();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }


    class AddTakeDemo
    {
        public static void BC_AddTakeCompleteAdding()
        {
            // A bounded collection. It can hold no more
            // than 100 items at once.
            BlockingCollection<int> numbers = new BlockingCollection<int>(100);


            // blocking consumer with no cancellation.
            Task.Run(() =>
            {
                while (!numbers.IsCompleted)
                {

                    int number = 0;
                    // Blocks if number.Count == 0
                    // IOE means that Take() was called on a completed collection.
                    // Some other thread can call CompleteAdding after we pass the
                    // IsCompleted check but before we call Take. 
                    // In this example, we can simply catch the exception since the 
                    // loop will break on the next iteration.
                    try
                    {
                        number = numbers.Take();
                    }
                    catch (InvalidOperationException)
                    { }

                    Console.WriteLine(number);
                }
                Console.WriteLine("\r\nNo more items to take.");
            });

            // A simple blocking producer with no cancellation.
            Task.Run(() =>
            {
                int i = 0;
                while (i < 100)
                {
                    DateTime data = DateTime.Now;
                    // Blocks if numbers.Count == dataItems.BoundedCapacity
                    Console.WriteLine("Add {0}", i);
                    numbers.Add(i);
                    ++i;
                }
                // Let consumer know we are done.
                numbers.CompleteAdding();
            });

        }
    }

    class TryTakeDemo
    {
        // Demonstrates:
        //      BlockingCollection<T>.Add()
        //      BlockingCollection<T>.CompleteAdding()
        //      BlockingCollection<T>.TryTake()
        //      BlockingCollection<T>.IsCompleted
        public static void BC_TryTake()
        {
            // Construct and fill our BlockingCollection
            using (BlockingCollection<int> bc = new BlockingCollection<int>())
            {
                int NUMITEMS = 10000;
                for (int i = 0; i < NUMITEMS; i++) bc.Add(i);
                bc.CompleteAdding();
                int outerSum = 0;

                // Delegate for consuming the BlockingCollection and adding up all items
                Action action = () =>
                {
                    int localItem;
                    int localSum = 0;

                    while (bc.TryTake(out localItem))
                    {
                        localSum += localItem;
                    }
                    Interlocked.Add(ref outerSum, localSum);
                };

                // Launch three parallel actions to consume the BlockingCollection
                Parallel.Invoke(action, action, action);

                Console.WriteLine("Sum[0..{0}) = {1}, should be {2}", NUMITEMS, outerSum, ((NUMITEMS * (NUMITEMS - 1)) / 2));
                Console.WriteLine("bc.IsCompleted = {0} (should be true)", bc.IsCompleted);
            }
        }
    }

    class FromToAnyDemo
    {
        public static void BC_FromToAny()
        {
            BlockingCollection<int>[] bcs = new BlockingCollection<int>[2];
            bcs[0] = new BlockingCollection<int>(5); // collection bounded to 5 items
            bcs[1] = new BlockingCollection<int>(5); // collection bounded to 5 items

            // Should be able to add 10 items w/o blocking
            int numFailures = 0;
            for (int i = 0; i < 10; i++)
            {
                if (BlockingCollection<int>.TryAddToAny(bcs, i) == -1) numFailures++;
            }
            Console.WriteLine("TryAddToAny: {0} failures (should be 0)", numFailures);

            // Should be able to retrieve 10 items
            int numItems = 0;
            int item;
            while (BlockingCollection<int>.TryTakeFromAny(bcs, out item) != -1) numItems++;
            Console.WriteLine("TryTakeFromAny: retrieved {0} items (should be 10)", numItems);
        }
    }

    class ConsumingEnumerableDemo
    {
        public static void BC_GetConsumingEnumerable()
        {
            using (BlockingCollection<int> bc = new BlockingCollection<int>())
            {
                Task.Factory.StartNew(() =>
                {
                    for (int i = 0; i < 100; i++)
                    {
                        bc.Add(i);
                        Console.WriteLine("Added {0}", i);
                        Thread.Sleep(100); // sleep 100 ms between adds
                    }

                    // Need to do this to keep foreach below from hanging
                    bc.CompleteAdding();
                });

                // Now consume the blocking collection with foreach.
                // Use bc.GetConsumingEnumerable() instead of just bc because the
                // former will block waiting for completion and the latter will
                // simply take a snapshot of the current state of the underlying collection.
                Thread.Sleep(1000); // sleep 100 ms between adds
                foreach (var item in bc.GetConsumingEnumerable())
                {
                    //Thread.Sleep(100); // sleep 100 ms between adds
                    Console.WriteLine(item);
                }
            }
        }
    }
}