using System.Text.Json;
using UnityEngine;

namespace UnityMCP
{
    /// <summary>
    /// Simple ping tool for testing MCP connectivity.
    /// </summary>
    public class PingTool : BaseMcpTool
    {
        public override string GetName()
        {
            return "ping";
        }

        public override string GetDescription()
        {
            return "Simple ping test that responds with 'pong' and optional echo message";
        }

        public override object GetInputSchema()
        {
            return new
            {
                type = "object",
                properties = new
                {
                    message = new
                    {
                        type = "string",
                        description = "Optional message to echo back"
                    }
                },
                required = new string[] { }
            };
        }

        public override object Execute(JsonElement parameters)
        {
            // Log ping received
            Debug.Log("[PingTool] Ping received");

            // Get optional message parameter
            string echoMessage = GetStringParam(parameters, "message");

            // Return pong response
            if (!string.IsNullOrEmpty(echoMessage))
            {
                return new
                {
                    message = "pong",
                    echo = echoMessage,
                    timestamp = System.DateTime.UtcNow.ToString("o")
                };
            }
            else
            {
                return new
                {
                    message = "pong",
                    timestamp = System.DateTime.UtcNow.ToString("o")
                };
            }
        }
    }
}
