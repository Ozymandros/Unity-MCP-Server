using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityMCP
{
    /// <summary>
    /// Dispatches MCP requests to registered tools and ensures execution on Unity main thread.
    /// </summary>
    public static class McpDispatcher
    {
        private static readonly Queue<Action> _mainThreadQueue = new Queue<Action>();
        private static readonly object _queueLock = new object();
        private static bool _initialized = false;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (_initialized) return;

            // Subscribe to EditorApplication.update to process main thread queue
            EditorApplication.update += ProcessMainThreadQueue;
            _initialized = true;
            Debug.Log("[McpDispatcher] Initialized");
        }

        /// <summary>
        /// Dispatch an MCP request to the appropriate tool.
        /// </summary>
        public static async Task<McpResponse> DispatchAsync(McpRequest request)
        {
            try
            {
                // Validate request
                if (request == null)
                {
                    return McpMessage.CreateErrorResponse(null, 
                        McpMessage.ErrorCodes.InvalidRequest, 
                        "Request is null");
                }

                if (string.IsNullOrEmpty(request.Method))
                {
                    return McpMessage.CreateErrorResponse(request.Id, 
                        McpMessage.ErrorCodes.InvalidRequest, 
                        "Invalid Request: missing method");
                }

                // Handle tools/call method (MCP standard)
                if (request.Method == "tools/call")
                {
                    return await DispatchToolCallAsync(request);
                }

                // Look up tool in registry by method name
                if (!McpToolRegistry.TryGetTool(request.Method, out var tool))
                {
                    return McpMessage.CreateErrorResponse(request.Id, 
                        McpMessage.ErrorCodes.MethodNotFound, 
                        $"Method not found: {request.Method}");
                }

                // Execute tool on main thread and wait for result
                TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

                EnqueueMainThreadAction(() =>
                {
                    try
                    {
                        object result = tool.Execute(request.Params);
                        tcs.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });

                object toolResult = await tcs.Task;
                return McpMessage.CreateSuccessResponse(request.Id, toolResult);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpDispatcher] Error dispatching request: {ex.Message}\n{ex.StackTrace}");
                return McpMessage.CreateErrorResponse(request.Id, 
                    McpMessage.ErrorCodes.InternalError, 
                    "Internal error", 
                    ex.Message);
            }
        }

        /// <summary>
        /// Handle the standard MCP tools/call method
        /// </summary>
        private static async Task<McpResponse> DispatchToolCallAsync(McpRequest request)
        {
            try
            {
                // Extract tool name from params
                if (!request.Params.TryGetProperty("name", out var nameElement))
                {
                    return McpMessage.CreateErrorResponse(request.Id,
                        McpMessage.ErrorCodes.InvalidParams,
                        "Missing 'name' parameter in tools/call");
                }

                string toolName = nameElement.GetString();

                // Get tool arguments
                var toolParams = request.Params.TryGetProperty("arguments", out var argsElement) 
                    ? argsElement 
                    : default;

                // Look up tool
                if (!McpToolRegistry.TryGetTool(toolName, out var tool))
                {
                    return McpMessage.CreateErrorResponse(request.Id,
                        McpMessage.ErrorCodes.MethodNotFound,
                        $"Tool not found: {toolName}");
                }

                // Execute tool on main thread
                TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

                EnqueueMainThreadAction(() =>
                {
                    try
                    {
                        object result = tool.Execute(toolParams);
                        tcs.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });

                object toolResult = await tcs.Task;

                // Wrap result in MCP standard format
                var mcpResult = new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = McpMessage.SerializeObject(toolResult)
                        }
                    }
                };

                return McpMessage.CreateSuccessResponse(request.Id, mcpResult);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpDispatcher] Error in tools/call: {ex.Message}");
                return McpMessage.CreateErrorResponse(request.Id,
                    McpMessage.ErrorCodes.InternalError,
                    "Error executing tool",
                    ex.Message);
            }
        }

        /// <summary>
        /// Enqueue an action to be executed on the Unity main thread.
        /// </summary>
        private static void EnqueueMainThreadAction(Action action)
        {
            if (action == null) return;

            lock (_queueLock)
            {
                _mainThreadQueue.Enqueue(action);
            }
        }

        /// <summary>
        /// Process queued actions on the Unity main thread.
        /// Called from EditorApplication.update.
        /// </summary>
        private static void ProcessMainThreadQueue()
        {
            // Process all queued actions
            lock (_queueLock)
            {
                while (_mainThreadQueue.Count > 0)
                {
                    Action action = _mainThreadQueue.Dequeue();
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[McpDispatcher] Error executing main thread action: {ex.Message}\n{ex.StackTrace}");
                    }
                }
            }
        }

        /// <summary>
        /// Get the current queue size (for debugging)
        /// </summary>
        public static int GetQueueSize()
        {
            lock (_queueLock)
            {
                return _mainThreadQueue.Count;
            }
        }
    }
}
