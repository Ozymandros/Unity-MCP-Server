using NUnit.Framework;
using System.Text.Json;
using UnityMCP;

namespace UnityMCP.Tests
{
    [TestFixture]
    public class McpMessageTests
    {
        [Test]
        public void ParseRequest_ValidJson_ReturnsRequest()
        {
            string json = "{\"jsonrpc\": \"2.0\", \"method\": \"ping\", \"id\": 1}";
            var message = McpMessage.ParseRequest(json);

            Assert.That(message, Is.InstanceOf<McpRequest>());
            var request = (McpRequest)message;
            Assert.That(request.Method, Is.EqualTo("ping"));
            Assert.That(((System.Text.Json.JsonElement)request.Id).GetInt32(), Is.EqualTo(1));
        }

        [Test]
        public void CreateErrorResponse_ReturnsValidJsonRpcError()
        {
            var response = McpMessage.CreateErrorResponse(1, -32601, "Method not found");
            string json = McpMessage.SerializeResponse(response);

            Assert.That(json.Contains("\"code\":-32601"), Is.True);
            Assert.That(json.Contains("\"message\":\"Method not found\""), Is.True);
        }
    }
}
