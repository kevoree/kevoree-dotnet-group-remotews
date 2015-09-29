using System;
using Org.Kevoree.Annotation;
using System.Net.WebSockets;
using Org.Kevoree.Core.Api;
using System.Threading;
using System.Collections.Concurrent;


namespace Org.Kevoree.Library
{
	[GroupType]
    [Serializable]
	public class WSGroup: MarshalByRefObject, DeployUnit
	{
		public WSGroup ()
		{
		}

		[KevoreeInject]
		private Context context;

		[KevoreeInject]
		private ModelService modelService;

		[Param (Optional = false, FragmentDependent = true, DefaultValue = "true")]
		private bool answerPull;

		[Param (Optional = false)]
		private string host;

		[Param (DefaultValue = "80")]
		private int port;

		[Param (DefaultValue = "/")]
		private string path;

		string CleanupPath (string param)
		{
			string ret;
			if (path.StartsWith ("/")) {
				ret = param.Substring (1);
			} else {
				ret = param;
			}
			return ret;
		}

		Uri UriAAA () {
			var pt = CleanupPath (path);
			return new Uri (string.Format ("ws://{0}:{1}/{2}", host, port, pt));
		}

		[Start]
		public void Start ()
		{
			// TODO
			BlockingCollection<string> queue = new BlockingCollection<string>();
			Thread producerThread = new Thread(new ThreadStart(new RemoteWSProducerThread(queue).Listen));
			Thread consummerThread = new Thread(new ThreadStart(new RemoteWSConsummerThread(this,queue).Consume));
			producerThread.Start ();
			consummerThread.Start ();
		}

		[Stop]
		public void Stop ()
		{
			
		}

		[Update]
		public void Update ()
		{
			// TODO : in principle useless ! (websocket url can be statically derived from host/port/path).
		}

		public Context getContext ()
		{
			return this.context;
		}

		public ModelService getModelService ()
		{
			return this.modelService;
		}

		public bool AnswerPull ()
		{
			return this.answerPull;
		}
	}
}

