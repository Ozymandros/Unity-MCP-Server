using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnityMCP
{
    /// <summary>
    /// Represents an MCP request following JSON-RPC 2.0 structure.
    /// </summary>
    public class McpRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public object Id { get; set; } // Can be string, number, or null

        [JsonPropertyName("method")]
        public string Method { get; set; }

        [JsonPropertyName("params")]
        public JsonElement Params { get; set; }
    }

    /// <summary>
    /// Represents an MCP response following JSON-RPC 2.0 structure.
    /// </summary>
    public class McpResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public object Id { get; set; }

        [JsonPropertyName("result")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object Result { get; set; }

        [JsonPropertyName("error")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public McpError Error { get; set; }
    }

    /// <summary>
    /// Represents an MCP error following JSON-RPC 2.0 structure.
    /// </summary>
    public class McpError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object Data { get; set; }
    }

    /// <summary>
    /// Utility class for serializing and deserializing MCP messages.
    /// </summary>
    public static class McpMessage
    {
        public const string VERSION = "1.1.0";
        public const string MODIFICATION_DATE = "2026-02-14";

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Parse JSON string into McpRequest.
        /// </summary>
        public static McpRequest ParseRequest(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<McpRequest>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse MCP request: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Serialize McpResponse to JSON string.
        /// </summary>
        public static string SerializeResponse(McpResponse response)
        {
            try
            {
                return JsonSerializer.Serialize(response, _jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to serialize MCP response: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Serialize an object to JSON string.
        /// </summary>
        public static string SerializeObject(object obj)
        {
            try
            {
                return JsonSerializer.Serialize(obj, _jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to serialize object: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Create a success response.
        /// </summary>
        public static McpResponse CreateSuccessResponse(object id, object result)
        {
            return new McpResponse
            {
                Id = id,
                Result = result
            };
        }

        /// <summary>
        /// Create an error response.
        /// </summary>
        public static McpResponse CreateErrorResponse(object id, int code, string message, object data = null)
        {
            return new McpResponse
            {
                Id = id,
                Error = new McpError
                {
                    Code = code,
                    Message = message,
                    Data = data
                }
            };
        }

        /// <summary>
        /// Standard JSON-RPC error codes
        /// </summary>
        public static class ErrorCodes
        {
            public const int ParseError = -32700;
            public const int InvalidRequest = -32600;
            public const int MethodNotFound = -32601;
            public const int InvalidParams = -32602;
            public const int InternalError = -32603;
        }
    }
}
