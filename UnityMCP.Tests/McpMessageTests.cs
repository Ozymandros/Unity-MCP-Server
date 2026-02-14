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
            
            Assert.IsInstanceOf<McpRequest>(message);
            var request = (McpRequest)message;
            Assert.AreEqual("ping", request.Method);
            Assert.AreEqual(1, ((System.Text.Json.JsonElement)request.Id).GetInt32());
        }

        [Test]
        public void CreateErrorResponse_ReturnsValidJsonRpcError()
        {
            var response = McpMessage.CreateErrorResponse(1, -32601, "Method not found");
            string json = McpMessage.SerializeResponse(response);
            
            Assert.IsTrue(json.Contains("\"code\":-32601"));
            Assert.IsTrue(json.Contains("\"message\":\"Method not found\""));
        }
    }
}
