using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Threading.Tasks;
using UnityMcp.Core.Models;
using Microsoft.Extensions.Logging;

namespace UnityMcp.Application.Routing;

/// <summary>
/// Interface for a generic MCP request handler (typed protocol erasure).
/// </summary>
public interface IMcpHandler
{
    /// <summary>
    /// The method name this handler responds to.
    /// </summary>
    string Method { get; }

    /// <summary>
    /// Handles the request logic using JsonElement for protocol erasure.
    /// </summary>
    /// <param name="parameters">Request parameters (raw JSON).</param>
    /// <returns>Result as JsonElement.</returns>
    Task<JsonElement?> HandleAsync(JsonElement? parameters);
}

/// <summary>
/// Base class for a generic MCP handler with strict application typing.
/// </summary>
/// <typeparam name="TParams">The type of parameters.</typeparam>
/// <typeparam name="TResult">The type of the result.</typeparam>
public abstract class McpHandlerBase<TParams, TResult> : IMcpHandler 
    where TParams : class, new()
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <inheritdoc />
    public abstract string Method { get; }

    /// <summary>
    /// Client-facing handler logic with strict types.
    /// </summary>
    public abstract Task<TResult> HandleAsync(TParams parameters);

    /// <inheritdoc />
    async Task<JsonElement?> IMcpHandler.HandleAsync(JsonElement? parameters)
    {
        TParams typedParams;
        if (parameters.HasValue)
        {
            typedParams = parameters.Value.Deserialize<TParams>(_jsonOptions) ?? new TParams();
        }
        else
        {
            typedParams = new TParams();
        }

        ValidateParameters(typedParams);
        
        var result = await HandleAsync(typedParams);
        return JsonSerializer.SerializeToElement(result, _jsonOptions);
    }

    private void ValidateParameters(TParams parameters)
    {
        var context = new ValidationContext(parameters);
        Validator.ValidateObject(parameters, context, validateAllProperties: true);
    }
}

/// <summary>
/// Routes incoming MCP requests to the appropriate handler.
/// </summary>
public class McpRouter
{
    private readonly Dictionary<string, IMcpHandler> _handlers = new();
    private readonly ILogger<McpRouter> _logger;

    public McpRouter(IEnumerable<IMcpHandler> handlers, ILogger<McpRouter> logger)
    {
        _logger = logger;
        foreach (var handler in handlers)
        {
            _handlers[handler.Method] = handler;
        }
    }

    /// <summary>
    /// Routes the request to a registered handler.
    /// </summary>
    public async Task<McpResponse> RouteRequestAsync(McpRequest request)
    {
        if (!_handlers.TryGetValue(request.Method, out var handler))
        {
            return new McpResponse
            {
                JsonRpc = McpConstants.JsonRpcVersion,
                Id = request.Id,
                Error = new McpError { Code = McpConstants.ErrorCodes.MethodNotFound, Message = $"Method '{request.Method}' not found." }
            };
        }

        try
        {
            // Pass JsonElement? directly to the handler
            var result = await handler.HandleAsync(request.Params);

            return new McpResponse
            {
                JsonRpc = McpConstants.JsonRpcVersion,
                Id = request.Id,
                Result = result
            };
        }
        catch (ValidationException valEx)
        {
            return new McpResponse
            {
                JsonRpc = McpConstants.JsonRpcVersion,
                Id = request.Id,
                Error = new McpError { Code = McpConstants.ErrorCodes.InvalidParams, Message = valEx.Message }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing method {Method}", request.Method);
            return new McpResponse
            {
                JsonRpc = McpConstants.JsonRpcVersion,
                Id = request.Id,
                Error = new McpError { Code = McpConstants.ErrorCodes.ServerErrorExecution, Message = ex.Message }
            };
        }
    }
}
