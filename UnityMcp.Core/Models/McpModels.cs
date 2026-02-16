using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace UnityMcp.Core.Models;

/// <summary>
/// Represents a JSON-RPC 2.0 request.
/// </summary>
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
    public object? Result { get; set; }

    [JsonPropertyName("error")]
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

/// <summary>
/// Commonly used constants for the MCP protocol.
/// </summary>
public static class McpConstants
{
    /// <summary>
    /// The current JSON-RPC version.
    /// </summary>
    public const string JsonRpcVersion = "2.0";

    /// <summary>
    /// The current MCP protocol version.
    /// </summary>
    public const string ProtocolVersion = "2.0";

    /// <summary>
    /// Error codes as defined by the JSON-RPC 2.0 specification.
    /// </summary>
    public static class ErrorCodes
    {
        /// <summary>
        /// Invalid JSON was received by the server. An error occurred on the server while parsing the JSON text.
        /// </summary>
        public const int ParseError = -32700;

        /// <summary>
        /// The JSON sent is not a valid Request object.
        /// </summary>
        public const int InvalidRequest = -32600;

        /// <summary>
        /// The method does not exist / is not available.
        /// </summary>
        public const int MethodNotFound = -32601;

        /// <summary>
        /// Invalid method parameter(s).
        /// </summary>
        public const int InvalidParams = -32602;

        /// <summary>
        /// Internal JSON-RPC error.
        /// </summary>
        public const int InternalError = -32603;

        /// <summary>
        /// Reserved for implementation-defined server-errors.
        /// </summary>
        public const int ServerErrorExecution = -32000;
    }
}

/// <summary>
/// Method names for the MCP protocol.
/// </summary>
public static class McpMethods
{
    /// <summary>Handshake initialization.</summary>
    public const string Initialize = "initialize";
    
    /// <summary>Lists available tools.</summary>
    public const string ListTools = "tools/list";
    
    /// <summary>Connectivity check.</summary>
    public const string Ping = "ping";
    
    /// <summary>Creates a new Unity scene.</summary>
    public const string CreateScene = "unity_create_scene";
    
    /// <summary>Creates a new C# script.</summary>
    public const string CreateScript = "unity_create_script";
    
    /// <summary>Lists assets in the project.</summary>
    public const string ListAssets = "unity_list_assets";
    
    /// <summary>Builds the Unity project.</summary>
    public const string BuildProject = "unity_build_project";

    /// <summary>Creates a new GameObject.</summary>
    public const string CreateGameObject = "unity_create_gameobject";

    /// <summary>Creates a new generic asset.</summary>
    public const string CreateAsset = "unity_create_asset";
}

public class InitializeResult
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = McpConstants.ProtocolVersion;

    [JsonPropertyName("server")]
    public McpServerInfo Server { get; set; } = new();

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
