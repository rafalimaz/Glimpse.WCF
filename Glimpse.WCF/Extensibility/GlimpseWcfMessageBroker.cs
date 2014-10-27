﻿using System;
using Glimpse.Core.Extensibility;
using Glimpse.Core.Message;
using Glimpse.WCF.Message;

namespace Glimpse.WCF.Extensibility
{
    public class GlimpseWcfMessageBroker : IMessageBroker
    {
        private readonly GlimpseWcfContext _context = GlimpseWcfContext.Current;

        public void Publish<T>(T message)
        {
            if (message is ITimelineMessage)
            {
                _context.AccumulateMessage(new SerializableTimelineMessage(message as ITimelineMessage));
                return;
            }
            if (message is ITraceMessage)
            {
                _context.AccumulateMessage(new SerializableTraceMessage(message as ITraceMessage));
                return;
            }
            if (message is ITimedMessage)
            {
                _context.AccumulateMessage(new SerializableTimedMessage(message as ITimedMessage));
                return;
            }
        }

        public Guid Subscribe<T>(Action<T> action)
        {
            // do nothing
            return Guid.NewGuid();
        }

        public void Unsubscribe<T>(Guid subscriptionId)
        {
            // do nothing
        }
    }
}
