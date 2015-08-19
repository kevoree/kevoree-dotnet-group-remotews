using System;
using Org.Kevoree.Annotation;
using System.Net.WebSockets;
using Org.Kevoree.Core.Api;
using System.Threading;
using System.Collections.Concurrent;
using Org.Kevoree.Log;
using Org.Kevoree.Core.Api.Protocol;
using System.Collections.Generic;
using org.kevoree.modeling.api.json;
using org.kevoree.factory;
using org.kevoree.modeling.api;
using org.kevoree;
using System.Text;


namespace Org.Kevoree.Group
{
	public class RemoteWSConsummerThread
	{
		private BlockingCollection<string> queue;

		private RemoteWS group;

		private Log.Log log = LogFactory.getLog (typeof(RemoteWSProducerThread).ToString (), Level.INFO);

		private JSONModelLoader loader = new JSONModelLoader (new DefaultKevoreeFactory ());

		private JSONModelSerializer serializer = new JSONModelSerializer ();

		public RemoteWSConsummerThread (RemoteWS group, BlockingCollection<string> queue)
		{
			this.queue = queue;
			this.group = group;
		}

		string getInstanceName ()
		{
			return this.group.getContext ().getInstanceName ();
		}

		void casePush (Protocol.PushMessage pushMessage)
		{
			var model = pushMessage.getModel ();
			if (model != null && model.Length != 0) {
				var models = this.loader.loadModelFromString (model);
				if (models != null && model.Length > 0) {
					this.group.getModelService ().update ((ContainerRoot)models.get (0), null);
				} else {
					log.Warn (string.Format ("\"{0}\" received model is empty, push aborted", this.group.getContext ().getInstanceName ()));
				}
			} else {
				log.Warn (string.Format ("\"{0}\" push message does not contain a model, push aborted", getInstanceName ()));
			}

		}

		async void send (string str)
		{
			using (var ws = new ClientWebSocket ()) {
				await ws.ConnectAsync (new Uri ("ws://ws.kevoree.org/mytest"), CancellationToken.None);
				var outputBuffer = new ArraySegment<byte> (Encoding.UTF8.GetBytes (str));
				await ws.SendAsync (outputBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
			}
		}

		void casePull (Protocol.PullMessage pullMessage)
		{
			if (this.group.AnswerPull ()) {
				log.Info (string.Format ("\"{0}\" received a pull request", getInstanceName ()));
				try {
					this.send (serializer.serialize (this.group.getModelService ().getCurrentModel ().getModel ()));
				} catch {
					log.Warn (string.Format ("\"{0}\" unable to serialize current model, pull aborted", getInstanceName ()));
				}
			} else {
				log.Warn (string.Format ("\"{}\" received a pull request, but 'answerPull' mode is false", getInstanceName ()));
			}
		}

		void caseDefault (string message)
		{
			var summary = GetMessageSummary (message);
			log.Debug (string.Format ("\"{0}\" unknown incoming message ({0})", getInstanceName (), summary));
		}

		static string GetMessageSummary (string message)
		{
			string summary;
			if (message.Length > 10) {
				summary = message.Substring (0, 10) + "...";
			} else {
				summary = message;
			}
			return summary;
		}

		public void Consume ()
		{
			foreach (var message in queue.GetConsumingEnumerable()) {
				try {
					log.Info ("Message dequeued");
					var parsedMessage = Protocol.parse (message);
					var messageType = parsedMessage.getType ();
					if (messageType == Protocol.PUSH_TYPE) {
						casePush ((Protocol.PushMessage)parsedMessage);
					} else if (messageType == Protocol.PULL_TYPE) {
						casePull ((Protocol.PullMessage)parsedMessage);
					} else {
						caseDefault (message);
					}
				} catch {
					var summary = GetMessageSummary (message);
					log.Warn (string.Format ("\"{0}\" unable to process incoming message ({1})", getInstanceName (), summary));
				}
			}
		}
	}
}
