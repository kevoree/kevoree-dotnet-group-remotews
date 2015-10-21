using System;
using System.Collections.Concurrent;
using System.Linq;
using Org.Kevoree.Log;
using WebSocketSharp;
using System.Threading;

namespace Org.Kevoree.Library
{
    public class RemoteWSProducerThread
    {
        private Log.Log log = LogFactory.getLog(typeof(RemoteWSProducerThread).ToString(), Level.INFO);
        private readonly RemoteWSGroup group;

        public RemoteWSProducerThread(RemoteWSGroup group)
        {
            this.group = group;
        }

        public void Listen()
        {


            using (var ws = new WebSocket(this.group.getWSUrl()))
            {
                ws.OnMessage += (sender, e) =>
                {
                    Console.WriteLine("Message received !");
                    this.group.getQueue().Add(e.Data);
                };

                ws.OnClose += (sender, args) =>
                {
                    Console.WriteLine("RemoteWSGroup OnClose " + sender + " " + args);
                };


                ws.Connect();
                while (true)
                {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}