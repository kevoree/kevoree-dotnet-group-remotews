using System;
using Org.Kevoree.Annotation;
using System.Net.WebSockets;
using Org.Kevoree.Core.Api;
using System.Threading;
using System.Collections.Concurrent;
using System.Linq;


namespace Org.Kevoree.Library
{
    [GroupType]
    [Serializable]
    public class RemoteWSGroup : MarshalByRefObject, DeployUnit
    {

        [KevoreeInject]
        private Context context;

        [KevoreeInject]
        private ModelService modelService;

        [Param(Optional = false, FragmentDependent = true, DefaultValue = "true")]
        private bool answerPull = true;

        [Param(Optional = false)]
        private string host;

        [Param(DefaultValue = "80")]
        private int port = 80;

        [Param(DefaultValue = "/")]
        private string path = "/test";

        private readonly BlockingCollection<string> queue = new BlockingCollection<string>();

        string CleanupPath(string param)
        {
            return path.StartsWith("/") ? param.Substring(1) : param;
        }

        [Start]
        public void Start()
        {
            if (path != null && path.Length > 0 && path.First() != '/')
            {
                this.path = '/' + this.path;
            }
            Console.WriteLine("Start RemoteWS");
            
            var producerThread = new Thread(new ThreadStart(new RemoteWSProducerThread(this).Listen));
            var consummerThread = new Thread(new ThreadStart(new RemoteWSConsummerThread(this).Consume));
            producerThread.Start();
            consummerThread.Start();
            //consummerThread.Join();
            //producerThread.Join();
            //Console.WriteLine("Stop RemoteWS");
        }

        [Stop]
        public void Stop()
        {

        }

        [Update]
        public void Update()
        {
            // TODO : in principle useless ! (websocket url can be statically derived from host/port/path).
        }

        public Context getContext()
        {
            return this.context;
        }

        public ModelService getModelService()
        {
            return this.modelService;
        }

        public bool AnswerPull()
        {
            return this.answerPull;
        }

        internal string getWSUrl()
        {
            if (host == null)
            {
                host = "ws.kevoree.org";
            }

            if (path != null && !path.StartsWith("/"))
            {
                path = "/" + path;
            }
            return "ws://" + host + ":" + port + path;
        }

        internal BlockingCollection<string> getQueue()
        {
            return this.queue;
        }
    }
}

