using System;
using System.Text.Json;
using System.Threading.Tasks;
using UnityMcp.Application.Routing;
using UnityMcp.Core.Models;

namespace UnityMcp.Application.Handlers.Core
{
    /// <summary>
    /// Handles tool execution via the 'tools/call' method.
    /// </summary>
    public class CallToolHandler : McpHandlerBase<CallToolParameters, object>
    {
        private readonly McpRouter _router;

        public CallToolHandler(McpRouter router)
        {
            _router = router;
        }

        public override string Method => McpMethods.CallTool;

        public override async Task<object> HandleAsync(CallToolParameters parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters.Name))
                throw new ArgumentException("Tool name is required.");

            // Build a fake MCP request to route to the correct handler
            var request = new McpRequest
            {
                Method = parameters.Name,
                Params = parameters.Params
            };
            var response = await _router.RouteRequestAsync(request);
            if (response.Error != null)
                throw new Exception($"Tool execution failed: {response.Error.Message}");
            return response.Result ?? new object();
        }
    }

    public class CallToolParameters
    {
        public string Name { get; set; } = string.Empty;
        public JsonElement? Params { get; set; }
    }
}
