using System;
using Org.Kevoree.Annotation;
using Org.Kevoree.Core.Api;
using System.Threading;
using System.Collections.Concurrent;
using Org.Kevoree.Core.Api.Protocol;
using System.Text;
using com.sun.xml.@internal.ws.client;
using Org.Kevoree.Log;
using WebSocketSharp;


namespace Org.Kevoree.Library
{
	public class RemoteWSProducerThread
	{
		private BlockingCollection<string> queue;

		private Log.Log log = LogFactory.getLog (typeof(RemoteWSProducerThread).ToString (), Level.INFO);

	    private object lockz = new Object();

		public RemoteWSProducerThread (BlockingCollection<string> queue)
		{
			this.queue = queue;
		}

		public async void Listen ()
		{
		    var sem = new Semaphore(1, 1);
            using (var ws = new WebSocket("ws://ws.kevoree.org/mytest"))
            {
                ws.OnMessage += (sender, e) =>
                {
                    Console.WriteLine("Message received !");
                    queue.Add(e.Data);
                    sem.Release(1);
                };
                //while (true)
				//{
                while (true)
                {
                    ws.ConnectAsync();
                    sem.WaitOne();
                }
                
				    /*WebSocketReceiveResult result;
					var message = new StringBuilder ();
					do {
						var segment = new ArraySegment<byte> (new byte[1024]);
						result = await ws.ReceiveAsync (segment, CancellationToken.None);
						message.Append (Encoding.UTF8.GetString (segment.Array, 0, result.Count));
					} while(!result.EndOfMessage);
					log.Error ("Message enqueued.");
					queue.Add (message.ToString());*/
				//}
			}
		}
	}

}

