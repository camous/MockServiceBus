using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic;
using System.Text;

namespace MockServiceBus
{
    [DebuggerDisplay("{Name}")]
    public class Subscription
    {
        public string Name { get; set; }
        public List<Rule> Rules { get; set; } = new List<Rule>();

        public Message ReceiveMessage(Message message)
        {
            message.TraversedPath.Add(Name);
            var output = new List<Message> { message };

            var filter = string.Join(" || ", Rules.Select(x => x.LinqFilter).ToArray());
            bool rulesuccess = false;
            try
            {
                rulesuccess = output.Where(filter).Count() > 0;
            }
            catch (ParseException e)
            {
                Trace.WriteLine(filter.Substring(e.Position));
                throw e;
            }

            Rules.ForEach(x => x.ApplyAction(message));

            if (rulesuccess)
            {
                message.State = MessageState.Transfered;
            }
            else
            {
                message.State = MessageState.Ignored;
            }

            return message;
        }
    }
}
