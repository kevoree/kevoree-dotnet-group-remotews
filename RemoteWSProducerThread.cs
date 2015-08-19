using System;
using Org.Kevoree.Annotation;
using System.Net.WebSockets;
using Org.Kevoree.Core.Api;
using System.Threading;
using System.Collections.Concurrent;
using Org.Kevoree.Core.Api.Protocol;
using System.Text;
using Org.Kevoree.Log;


namespace Org.Kevoree.Group
{
	public class RemoteWSProducerThread
	{
		private BlockingCollection<string> queue;

		private Log.Log log = LogFactory.getLog (typeof(RemoteWSProducerThread).ToString (), Level.INFO);

		public RemoteWSProducerThread (BlockingCollection<string> queue)
		{
			this.queue = queue;
		}

		public async void Listen ()
		{
			using (var ws = new ClientWebSocket ()) {
				await ws.ConnectAsync (new Uri ("ws://ws.kevoree.org/mytest"), CancellationToken.None);
				while (true) {
					WebSocketReceiveResult result;
					var message = new StringBuilder ();
					do {
						var segment = new ArraySegment<byte> (new byte[1024]);
						result = await ws.ReceiveAsync (segment, CancellationToken.None);
						message.Append (Encoding.UTF8.GetString (segment.Array, 0, result.Count));
					} while(!result.EndOfMessage);
					log.Error ("Message enqueued.");
					queue.Add (message.ToString());
				}
			}
		}
	}

}

