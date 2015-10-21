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
using WebSocket = WebSocketSharp.WebSocket;

namespace Org.Kevoree.Library
{
    public class RemoteWSConsummerThread
    {
        private readonly RemoteWSGroup group;
        private readonly JSONModelLoader loader = new JSONModelLoader(new DefaultKevoreeFactory());
        private readonly Log.Log log = LogFactory.getLog(typeof(RemoteWSProducerThread).ToString(), Level.INFO);
        private readonly JSONModelSerializer serializer = new JSONModelSerializer();
        

        public RemoteWSConsummerThread(RemoteWSGroup group)
        {
            this.group = group;
        }

        private string getInstanceName()
        {
            return group.getContext().getInstanceName();
        }

        private void casePush(Protocol.PushMessage pushMessage)
        {
            var model = pushMessage.getModel();
            if (model != null && model.Length != 0)
            {
                var models = loader.loadModelFromString(model);
                if (models != null && model.Length > 0)
                {
                    group.getModelService().update(new ContainerRootMarshalled((ContainerRoot)models.get(0)), null);
                }
                else
                {
                    log.Warn(string.Format("\"{0}\" received model is empty, push aborted",
                        group.getContext().getInstanceName()));
                }
            }
            else
            {
                log.Warn(string.Format("\"{0}\" push message does not contain a model, push aborted", getInstanceName()));
            }
        }

        private async void send(string str)
        {
            using (var ws = new WebSocket(this.group.getWSUrl()))
            {
                ws.Connect();
                ws.Send(str);
                ws.Close();
            }
        }

        private void casePull(Protocol.PullMessage pullMessage)
        {
            if (group.AnswerPull())
            {
                log.Info(string.Format("\"{0}\" received a pull request", getInstanceName()));
                try
                {
                    send(group.getModelService().getCurrentModel().getModel().serialize());
                }
                catch
                {
                    log.Warn(string.Format("\"{0}\" unable to serialize current model, pull aborted", getInstanceName()));
                }
            }
            else
            {
                log.Warn(string.Format("\"{}\" received a pull request, but 'answerPull' mode is false",
                    getInstanceName()));
            }
        }

        private void caseDefault(string message)
        {
            var summary = GetMessageSummary(message);
            log.Debug(string.Format("\"{0}\" unknown incoming message ({0})", getInstanceName(), summary));
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
            foreach (var message in this.group.getQueue().GetConsumingEnumerable())
            {
                try
                {
                    log.Info("Message dequeued");
                    var parsedMessage = Protocol.parse(message);
                    var messageType = parsedMessage.getType();
                    if (messageType == Protocol.PUSH_TYPE)
                    {
                        casePush((Protocol.PushMessage)parsedMessage);
                    }
                    else if (messageType == Protocol.PULL_TYPE)
                    {
                        casePull((Protocol.PullMessage)parsedMessage);
                    }
                    else
                    {
                        caseDefault(message);
                    }
                }
                catch
                {
                    var summary = GetMessageSummary(message);
                    log.Warn(string.Format("\"{0}\" unable to process incoming message ({1})", getInstanceName(),
                        summary));
                }
            }
        }
    }
}