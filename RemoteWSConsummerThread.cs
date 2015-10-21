using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using org.kevoree;
using org.kevoree.factory;
using org.kevoree.pmodeling.api.json;
using Org.Kevoree.Core.Api.Protocol;
using Org.Kevoree.Core.Marshalled;
using Org.Kevoree.Log;
using Org.Kevoree.Log.Api;

namespace Org.Kevoree.Library
{
    public class RemoteWSConsummerThread
    {
        private readonly RemoteWSGroup _group;
        private readonly JSONModelLoader _loader = new JSONModelLoader(new DefaultKevoreeFactory());
        private ILogger _logger;
        

        public RemoteWSConsummerThread(RemoteWSGroup group, ILogger logger)
        {
            this._group = group;
            this._logger = logger;
        }

        private string getInstanceName()
        {
            return _group.getContext().getInstanceName();
        }

        private void casePush(Protocol.PushMessage pushMessage)
        {
            var model = pushMessage.getModel();
            if (model != null && model.Length != 0)
            {
                var models = _loader.loadModelFromString(model);
                if (models != null && model.Length > 0)
                {
                    _group.getModelService().update(new ContainerRootMarshalled((ContainerRoot)models.get(0)), null);
                }
                else
                {
                    _logger.Warn(string.Format("\"{0}\" received model is empty, push aborted",
                        _group.getContext().getInstanceName()));
                }
            }
            else
            {
                _logger.Warn(string.Format("\"{0}\" push message does not contain a model, push aborted", getInstanceName()));
            }
        }

        private async void send(string str)
        {
            this._group.getWebsocket().Send(str);
        }

        private void casePull(Protocol.PullMessage pullMessage)
        {
            if (_group.AnswerPull())
            {
                _logger.Info(string.Format("\"{0}\" received a pull request", getInstanceName()));
                try
                {
                    send(_group.getModelService().getCurrentModel().getModel().serialize());
                }
                catch
                {
                    _logger.Warn(string.Format("\"{0}\" unable to serialize current model, pull aborted", getInstanceName()));
                }
            }
            else
            {
                _logger.Warn(string.Format("\"{}\" received a pull request, but 'answerPull' mode is false",
                    getInstanceName()));
            }
        }

        private void caseDefault(string message)
        {
            var summary = GetMessageSummary(message);
            _logger.Debug(string.Format("\"{0}\" unknown incoming message ({0})", getInstanceName(), summary));
        }

        private static string GetMessageSummary(string message)
        {
            string summary;
            if (message.Length > 10)
            {
                summary = message.Substring(0, 10) + "...";
            }
            else
            {
                summary = message;
            }
            return summary;
        }

        public void Consume()
        {
            try
            {
                foreach (var message in this._group.getQueue().GetConsumingEnumerable())
                {
                    try
                    {
                        _logger.Info("Message dequeued");
                        var parsedMessage = Protocol.parse(message);
                        var messageType = parsedMessage.getType();
                        if (messageType == Protocol.PUSH_TYPE)
                        {
                            casePush((Protocol.PushMessage) parsedMessage);
                        }
                        else if (messageType == Protocol.PULL_TYPE)
                        {
                            casePull((Protocol.PullMessage) parsedMessage);
                        }
                        else
                        {
                            caseDefault(message);
                        }
                    }
                    catch
                    {
                        var summary = GetMessageSummary(message);
                        _logger.Warn(string.Format("\"{0}\" unable to process incoming message ({1})", getInstanceName(), summary));
                    }
                }
            }
            catch (OperationCanceledException) { }
            _logger.Debug("Consummer stopped");
        }

        internal void RequestStop()
        {
            _logger.Debug("Consummer stop requested");
            this._group.getQueue().CompleteAdding();
        }
    }
}