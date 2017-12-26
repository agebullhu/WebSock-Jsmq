using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.WebSockets;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            using (WSRouter router = new WSRouter())
            using (WSPublisher publisher = new WSPublisher())
            {
                router.Bind("ws://localhost:80");
                publisher.Bind("ws://localhost:81");

                router.ReceiveReady += (sender, eventArgs) =>
                {
                    var identity = eventArgs.WSSocket.ReceiveFrameBytes();
                    string message = eventArgs.WSSocket.ReceiveFrameString();

                    eventArgs.WSSocket.SendMoreFrame(identity).SendFrame("OK");

                    publisher.SendMoreFrame("chat").SendFrame(message);
                };

                NetMQPoller poller = new NetMQPoller();
                poller.Add(router);

                poller.Run();
            }
        }
    }
}
