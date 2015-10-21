using System;
using Org.Kevoree.Annotation;
using Org.Kevoree.Core.Api;
using System.Threading;
using System.Collections.Concurrent;
using System.Linq;
using Org.Kevoree.Log.Api;
using WebSocketSharp;


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

        [KevoreeInject]
        private ILogger logger;

        private readonly BlockingCollection<string> queue = new BlockingCollection<string>();
        private RemoteWSProducerThread producerThread;
        private RemoteWSConsummerThread consummerThread;
        private WebSocket websocket;

        [Start]
        public void Start()
        {
            logger.Warn("Start");

            this.initWebSocket();
            this.producerThread = new RemoteWSProducerThread(this, this.logger);
            this.consummerThread = new RemoteWSConsummerThread(this, this.logger);

            var pThread = new Thread(new ThreadStart(producerThread.Listen));
            var cThread = new Thread(new ThreadStart(consummerThread.Consume));
            pThread.Start();
            cThread.Start();

        }

        private void initWebSocket()
        {
            if (this.websocket != null)
            {
                this.websocket.Close();
            }

            if (path != null && !path.StartsWith("/"))
            {
                path = '/' + path;
            }
            this.websocket = new WebSocket("ws://" + host + ":" + port + path);
            this.websocket.Connect();
        }

        [Stop]
        public void Stop()
        {
            producerThread.RequestStop();
            consummerThread.RequestStop();
            logger.Warn("Start");
        }

        [Update]
        public void Update()
        {
            this.initWebSocket();
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

        internal WebSocket getWebsocket()
        {

            return this.websocket;
        }

        internal BlockingCollection<string> getQueue()
        {
            return this.queue;
        }
    }
}

