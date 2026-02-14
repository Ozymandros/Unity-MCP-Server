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
            
            Assert.IsTrue(task.IsCompleted, "Task did not complete");
            var response = task.Result;
            
            Assert.IsNotNull(response.Error);
            Assert.AreEqual(McpMessage.ErrorCodes.InvalidRequest, response.Error.Code);
        }

        [Test]
        public void GetQueueSize_InitiallyZero()
        {
            Assert.AreEqual(0, McpDispatcher.GetQueueSize());
        }
    }
}
