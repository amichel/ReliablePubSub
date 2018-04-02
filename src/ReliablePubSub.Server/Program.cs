using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using ReliablePubSub.Common;

namespace ReliablePubSub.Server
{
    class Program
    {
        private static void Main(string[] args)
        {
            //Console.WriteLine("Running");
            //using (var monitor = new DefaultConnectionMonitor())
            //using (var server = new RouterSocket())
            //using (var client = new DealerSocket())
            //using (var poller = new NetMQPoller())
            //{
            //    server.Bind("tcp://*:6669");
            //    var timer = new NetMQTimer(1000) { Enable = false };
            //    timer.Elapsed += (s, e) =>
            //    {
            //        Console.WriteLine($"Timer Elapsed {DateTime.UtcNow.ToShortTimeString()}");
            //    };
            //    poller.Add(client);
            //    poller.Add(timer);
            //    poller.RunAsync();

            //    //timer.EnableAndReset();

            //    if (!monitor.TryConnectAndMonitorSocket(client, "tcp://localhost:6669", poller,
            //        (m, s) =>
            //        {
            //            Console.WriteLine($"Monitor ConnectionState {s}");

            //            if (s)
            //                timer.Enable = false;
            //            else
            //                timer.EnableAndReset();
            //        }))
            //    {
            //        Console.WriteLine("Connect Timeout");
            //    }

            //    //do
            //    //{
            //    //    Thread.Sleep(1000);
            //    //} while (Console.ReadKey().Key != ConsoleKey.Escape);
            //    Thread.Sleep(5000);
            //    server.Close();

            //    do
            //    {
            //        Thread.Sleep(1000);
            //    } while (Console.ReadKey().Key != ConsoleKey.Escape);
            //}
            //Console.WriteLine("Stopped");


            //return;

            var knownTypes = new Dictionary<Type, TypeConfig>();
            knownTypes.Add(typeof(MyMessage), new TypeConfig
            {
                Serializer = new WireSerializer(),
                Comparer = new DefaultComparer<MyMessage>(),
                KeyExtractor = new DefaultKeyExtractor<MyMessage>(x => x.Key)
            });

            var topics = new Dictionary<string, Type>();
            topics.Add("topic1", typeof(MyMessage));
            int counter = 0;
            using (var tokenSource = new CancellationTokenSource())
            using (var publisher = new Publisher("tcp://*", 6669, 6668, topics.Keys))
            {
                var tasks = new List<Task>();

                for (int i = 0; i < 100; i++)
                {
                    var t = Task.Run(() =>
                    {

                        long id = 0;
                        var rnd = new Random(1);
                        while (!tokenSource.IsCancellationRequested)
                        {
                            var message = new MyMessage
                            {
                                Id = id++,
                                Key = rnd.Next(1, 1000).ToString(),
                                Body = $"T:{Thread.CurrentThread.ManagedThreadId} Body: {Guid.NewGuid().ToString()}",
                                TimeStamp = DateTime.UtcNow
                            };
                            publisher.Publish(knownTypes, "topic1", message);
                            Console.Title = $"Sent: {Interlocked.Increment(ref counter)}";
                            Thread.Sleep(DateTime.UtcNow.Second % 30 == 0 ? 60000 : 100);
                        }
                    }, tokenSource.Token);
                    tasks.Add(t);
                }
                while (Console.ReadKey().Key != ConsoleKey.Escape) { }
                tokenSource.Cancel();
                Task.WaitAll(tasks.ToArray(), 5000);
            }
        }
    }
}