using System;
using System.Collections.Concurrent;
using System.Linq;
using Org.Kevoree.Log;
using WebSocketSharp;
using System.Threading;
using Org.Kevoree.Log.Api;

namespace Org.Kevoree.Library
{
    public class RemoteWSProducerThread
    {
        private readonly RemoteWSGroup _group;
        private bool _stop = false;
        private readonly ILogger _logger;

        public RemoteWSProducerThread(RemoteWSGroup group, ILogger logger)
        {
            this._group = group;
            this._logger = logger;
        }

        public void Listen()
        {
            var ws = this._group.getWebsocket();
            ws.OnMessage += (sender, e) =>
            {
                this._logger.Debug("Message received");
                this._group.getQueue().Add(e.Data);
            };

            ws.OnClose += (sender, args) =>
            {
                this._logger.Debug("Websocket closed");
            };

            while (!_stop)
            {
                Thread.Sleep(1000);
            }
            _logger.Debug("Producer stopped");
        }

        public void RequestStop()
        {
            _logger.Debug("Producer stop requested");
            this._stop = true;
        }
    }
}