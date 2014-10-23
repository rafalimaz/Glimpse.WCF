﻿using System;
using System.Collections.Concurrent;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using Glimpse.Core.Extensibility;
using Glimpse.Core.Framework;
using Glimpse.Core.Message;

namespace Glimpse.WCF
{
    internal class GlimpseWcfClientMessageInspector : IClientMessageInspector
    {
        private readonly ConcurrentDictionary<Guid, TimeSpan> _knownOffsets = new ConcurrentDictionary<Guid, TimeSpan>(); 

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            IExecutionTimer timer = GlimpseConfiguration.GetConfiguredTimerStrategy()();
            TimeSpan offset = timer.Start();
            Guid correlationState = Guid.NewGuid();
            if (!_knownOffsets.TryAdd(correlationState, offset))
            {
                throw new InvalidOperationException();
            }

            return correlationState;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            // Time this call
            IExecutionTimer timer = GlimpseConfiguration.GetConfiguredTimerStrategy()();
            TimeSpan offset;
            if (!_knownOffsets.TryRemove((Guid)correlationState, out offset))
            {
                throw new InvalidOperationException();
            }

            var message = new WcfTimelineMessage
            {
                EventName = "Some Service"
            }.AsTimedMessage(timer.Stop(offset));

            // Publish this request's timeline message
            IMessageBroker broker = GlimpseConfiguration.GetConfiguredMessageBroker();
            broker.Publish(message);
            GlimpseWcfContext.Current.AccumulateMessage(message);

            // Publish any timeline messages the service is reporting
            var glimpseWcfHeader = reply.Headers.GetHeader<GlimpseWcfMessageHeader>(GlimpseWcfMessageHeader.Name, GlimpseWcfMessageHeader.Namespace);
            foreach (var headerMessage in glimpseWcfHeader.CalledServiceTimes)
            {
                broker.Publish(headerMessage);
                GlimpseWcfContext.Current.AccumulateMessage(headerMessage);
            }
        }
    }
}
