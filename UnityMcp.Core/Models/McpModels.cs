using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace UnityMcp.Core.Models
{
    /// <summary>
    /// Commonly used constants for the MCP protocol.
    /// </summary>
    public static class McpConstants
    {
        public const string JsonRpcVersion = "2.0";
        public const string ProtocolVersion = "2024-11-05";
        public static class ErrorCodes
        {
            public const int ParseError = -32700;
            public const int InvalidRequest = -32600;
            public const int MethodNotFound = -32601;
            public const int InvalidParams = -32602;
            public const int InternalError = -32603;
            public const int ServerErrorExecution = -32000;
        }
    }

    /// <summary>
    /// Method names for the MCP protocol.
    /// </summary>
    public static class McpMethods
    {
        public const string CallTool = "tools/call";
        public const string Initialize = "initialize";
        public const string ListTools = "tools/list";
        public const string Ping = "ping";
        public const string CreateScene = "unity_create_scene";
        public const string CreateScript = "unity_create_script";
        public const string ListAssets = "unity_list_assets";
        public const string BuildProject = "unity_build_project";
        public const string CreateGameObject = "unity_create_gameobject";
        public const string CreateAsset = "unity_create_asset";
    }

    public class McpRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = McpConstants.JsonRpcVersion;
        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;
        [JsonPropertyName("params")]
        public System.Text.Json.JsonElement? Params { get; set; }
        [JsonPropertyName("id")]
        public System.Text.Json.JsonElement? Id { get; set; }
    }

    public class McpResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = McpConstants.JsonRpcVersion;
        [JsonPropertyName("result")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Result { get; set; }
        [JsonPropertyName("error")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public McpError? Error { get; set; }
        [JsonPropertyName("id")]
        public System.Text.Json.JsonElement? Id { get; set; }
    }

    public class McpError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        [JsonPropertyName("data")]
        public System.Text.Json.JsonElement? Data { get; set; }
    }

    public class McpTool
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        [JsonPropertyName("inputSchema")]
        public object InputSchema { get; set; } = new { type = "object" };
    }

    public class InitializeResult
    {
        [JsonPropertyName("protocolVersion")]
        public string ProtocolVersion { get; set; } = McpConstants.ProtocolVersion;
        [JsonPropertyName("serverInfo")]
        public McpServerInfo ServerInfo { get; set; } = new();
        [JsonPropertyName("capabilities")]
        public McpCapabilities Capabilities { get; set; } = new();
    }

    public class McpServerInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
    }

    public class McpCapabilities
    {
        [JsonPropertyName("tools")]
        public object? Tools { get; set; }
    }

    public class McpClientInfo
    {
        [JsonPropertyName("name")]
        [Required]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("version")]
        [Required]
        public string Version { get; set; } = string.Empty;
    }

    public class ListToolsResult
    {
        [JsonPropertyName("tools")]
        public System.Collections.Generic.List<McpTool> Tools { get; set; } = new();
    }
}
