using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MockServiceBus
{
    [DebuggerDisplay("{Name}")]
    public class Topic
    {
        public string Name { get; set; }
        public List<Subscription> Subscriptions { get; set; } = new List<Subscription>();

        public IEnumerable<Message> SendMessage(Message message)
        {
            message.TraversedPath.Add(Name);

            var outputMessages = new List<Message>();

            foreach (var subscriber in Subscriptions)
            {
                outputMessages.Add(subscriber.ReceiveMessage(message.Copy()));
            }
            return outputMessages;
        }
    }
}
