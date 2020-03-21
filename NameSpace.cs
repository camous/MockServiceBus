using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MockServiceBus
{
    public class NameSpace
    {
        public List<Topic> Topics { get; set; } = new List<Topic>();

        public void AssertRouting(string topicname, Message message, Dictionary<string, MessageState> expectedResults)
        {
            var topic = this.Topics.Single(x => x.Name == topicname);

            var output = topic.SendMessage(message);

            foreach (var expectedResult in expectedResults)
            {
                var subscribermessage = output.SingleOrDefault(x => x.TraversedPath.Contains(expectedResult.Key));

                if (subscribermessage == null)
                {
                    Assert.Fail($"subscriber not found {expectedResult.Key}");
                }
                else
                {
                    Assert.AreEqual(expectedResult.Value, subscribermessage.State, expectedResult.Key);
                }
            }

            // checking extra subscribers unexpected
            var extras = output.Where(x => !expectedResults.Keys.Contains(x.TraversedPath.Last()));
            if (extras.Count() > 0)
            {
                Assert.Fail("unexpected subscribers " + string.Join(" , ", extras.Select(x => x.TraversedPath.Last())));
            }
        }
    }
}
