using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MockServiceBus
{
    public enum MessageState { Active, Ignored, Transfered };

    [DebuggerDisplay("{State} {LastTraversedPath}")]
    public class Message
    {
        public Message()
        {
            BrokeredProperties.Add("MessageId", Guid.NewGuid().ToString("N"));
            State = MessageState.Active;
        }

        public List<string> TraversedPath { get; set; } = new List<string>();
        public MessageState State { get; set; }
        public Dictionary<string, object> BrokeredProperties { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();

        public string LastTraversedPath
        {
            get
            {
                return TraversedPath.Count == 0 ? "empty" : TraversedPath.Last();
            }
        }

        public Message Copy()
        {
            var clone = new Message
            {
                BrokeredProperties = BrokeredProperties.ToDictionary(entry => entry.Key,
                                               entry => entry.Value),
                CustomProperties = CustomProperties.ToDictionary(entry => entry.Key,
                                               entry => entry.Value),
                State = State,
                TraversedPath = TraversedPath.ToList()
            };

            return clone;
        }
    }
}
