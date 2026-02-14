using NUnit.Framework;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace UnityMCP.Tests
{
    [TestFixture]
    public class WebSocketProtocolTests
    {
        [Test]
        public void Server_IsRunning_ReturnsFalseBeforeStart()
        {
            Assert.That(McpServer.IsRunning(), Is.False);
        }

        // Add more networking tests here if separable from Unity
    }
}
