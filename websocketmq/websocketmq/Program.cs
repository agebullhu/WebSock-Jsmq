﻿using NetMQ;
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
                        var a = eventArgs.WSSocket.ReceiveFrameString();
                        var identity = eventArgs.WSSocket.ReceiveFrameBytes();
                        var message = eventArgs.WSSocket.ReceiveFrameString();

                        eventArgs.WSSocket.SendMoreFrame(identity).SendFrame("OK");

                        publisher?.SendMoreFrame("chat").SendFrame(message);
                    };

                    var poller = new NetMQPoller();
                    poller.Add(router);

                    poller.Run();
                }
            }
        }
    }
}
