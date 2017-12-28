using System;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using NetMQ.WebSockets;

namespace websocketmq
{
    class Program
    {

        static void Main(string[] args)
        {
            using (var router = new WSRouter())
            {
                using (var publisher = new WSPublisher())
                {
                    router.Bind("ws://localhost:80");
                    publisher.Bind("ws://localhost:81");

                    router.ReceiveReady += (sender, eventArgs) =>
                    {
                        var identity = eventArgs.WSSocket.ReceiveFrameBytes();
                        var message = eventArgs.WSSocket.ReceiveFrameString();

                        using (var datasendercontext = new DataSenderContext())
                        {
                            datasendercontext.Start(eventArgs, publisher, new Identity() { Receiver = identity });
                        }
                    };

                    var poller = new NetMQPoller();
                    poller.Add(router);
                    poller.Run();
                }
            }
        }

        public class Identity
        {
            public byte[] Receiver { get; set; }
            public string Channel { get; set; }
        }

        public class DataSenderContext : IDisposable
        {
            private Thread _mainThread;
            private readonly ManualResetEvent _manualResetEvent = new ManualResetEvent(false);

            public DataSenderContext()
            {
                _mainThread = new Thread(SendData);
                _mainThread.IsBackground = true;
            }

            private void SendData(object parameter)
            {
                var parameters = (object[])parameter;

                var eventArgs = parameters[0] as WSSocketEventArgs;
                var publisher = parameters[1] as WSPublisher;
                var identity = parameters[2] as Identity;

                eventArgs.WSSocket.SendMoreFrame(identity.Receiver).SendFrame("begin");

                for (int i = 0; i < 100; i++)
                {
                    Thread.Sleep(100);
                    publisher?.SendMoreFrame("chat").SendFrame(i.ToString());
                }

                eventArgs.WSSocket.SendMoreFrame(identity.Receiver).SendFrame("end");

                _manualResetEvent.Set();
            }

            public void Dispose()
            {
                _manualResetEvent?.Dispose();
                _mainThread = null;
            }

            public void Start(WSSocketEventArgs eventArgs, WSPublisher publisher, Identity identity)
            {
                _mainThread.Start(new object[] { eventArgs, publisher, identity });
                _manualResetEvent.WaitOne();
                _mainThread.Join(TimeSpan.FromSeconds(3));
                _mainThread.Abort();
            }
        }
    }
}
