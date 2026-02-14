using NUnit.Framework;
using System;
using System.Threading.Tasks;
using UnityMCP;

namespace UnityMCP.Tests
{
    [TestFixture]
    public class McpDispatcherTests
    {
        [Test]
        public void DispatchAsync_NullRequest_ReturnsError()
        {
            Task<McpResponse> task = McpDispatcher.DispatchAsync(null);

            // Pump the queue manually since we are not in Unity
            McpDispatcher.ProcessMainThreadQueue();

            // Wait for task completion (it should be immediate if queue pumped)
            task.Wait(1000);

            Assert.That(task.IsCompleted, Is.True, "Task did not complete");
            var response = task.Result;

            Assert.That(response.Error, Is.Not.Null);
            Assert.That(response.Error.Code, Is.EqualTo(McpMessage.ErrorCodes.InvalidRequest));
        }

        [Test]
        public void GetQueueSize_InitiallyZero()
        {
            Assert.That(McpDispatcher.GetQueueSize(), Is.EqualTo(0));
        }
    }
}
