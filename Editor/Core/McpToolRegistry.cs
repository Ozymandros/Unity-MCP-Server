using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using UnityEditor;
using UnityEngine;

namespace UnityMCP
{
    /// <summary>
    /// Registry for MCP tools. Supports dynamic registration via reflection.
    /// </summary>
    public static class McpToolRegistry
    {
        private static readonly Dictionary<string, IMcpTool> _tools = new Dictionary<string, IMcpTool>();
        private static bool _initialized = false;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (_initialized) return;

            // Auto-register all tools implementing IMcpTool
            RegisterAllTools();
            _initialized = true;
            Debug.Log($"[McpToolRegistry] Registered {_tools.Count} tools");
        }

        /// <summary>
        /// Register all tools found via reflection.
        /// </summary>
        private static void RegisterAllTools()
        {
            // Find all types implementing IMcpTool in all loaded assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Type toolInterface = typeof(IMcpTool);

            foreach (var assembly in assemblies)
            {
                try
                {
                    var toolTypes = assembly.GetTypes()
                        .Where(t => toolInterface.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (Type toolType in toolTypes)
                    {
                        try
                        {
                            IMcpTool tool = (IMcpTool)Activator.CreateInstance(toolType);
                            RegisterTool(tool);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[McpToolRegistry] Failed to register tool {toolType.Name}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Skip assemblies that can't be reflected
                    if (!(ex is ReflectionTypeLoadException))
                    {
                        Debug.LogWarning($"[McpToolRegistry] Could not scan assembly {assembly.GetName().Name}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Register a single tool.
        /// </summary>
        public static void RegisterTool(IMcpTool tool)
        {
            if (tool == null) return;

            string toolName = tool.GetName();
            if (string.IsNullOrEmpty(toolName))
            {
                Debug.LogError("[McpToolRegistry] Tool has empty name, skipping registration");
                return;
            }

            if (_tools.ContainsKey(toolName))
            {
                Debug.LogWarning($"[McpToolRegistry] Tool '{toolName}' already registered, overwriting");
            }

            _tools[toolName] = tool;
            Debug.Log($"[McpToolRegistry] Registered tool: {toolName}");
        }

        /// <summary>
        /// Unregister a tool by name.
        /// </summary>
        public static bool UnregisterTool(string toolName)
        {
            if (_tools.Remove(toolName))
            {
                Debug.Log($"[McpToolRegistry] Unregistered tool: {toolName}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Try to get a tool by name.
        /// </summary>
        public static bool TryGetTool(string toolName, out IMcpTool tool)
        {
            return _tools.TryGetValue(toolName, out tool);
        }

        /// <summary>
        /// Get all registered tool names.
        /// </summary>
        public static string[] GetAllToolNames()
        {
            return _tools.Keys.ToArray();
        }

        /// <summary>
        /// Get all registered tools.
        /// </summary>
        public static IMcpTool[] GetAllTools()
        {
            return _tools.Values.ToArray();
        }

        /// <summary>
        /// Get tool descriptions in MCP format.
        /// </summary>
        public static object[] GetAllToolDescriptions()
        {
            var descriptions = new List<object>();

            foreach (var tool in _tools.Values)
            {
                try
                {
                    var description = new
                    {
                        name = tool.GetName(),
                        description = tool.GetDescription(),
                        inputSchema = tool.GetInputSchema()
                    };
                    descriptions.Add(description);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[McpToolRegistry] Error getting description for tool {tool.GetName()}: {ex.Message}");
                }
            }

            return descriptions.ToArray();
        }

        /// <summary>
        /// Clear all registered tools (mainly for testing).
        /// </summary>
        public static void ClearAllTools()
        {
            _tools.Clear();
            Debug.Log("[McpToolRegistry] Cleared all tools");
        }
    }

    /// <summary>
    /// Interface that all MCP tools must implement.
    /// </summary>
    public interface IMcpTool
    {
        /// <summary>
        /// Get the tool's unique name.
        /// </summary>
        string GetName();

        /// <summary>
        /// Get the tool's description.
        /// </summary>
        string GetDescription();

        /// <summary>
        /// Get the tool's input schema (JSON Schema format).
        /// </summary>
        object GetInputSchema();

        /// <summary>
        /// Execute the tool with given parameters.
        /// </summary>
        object Execute(JsonElement parameters);
    }

    /// <summary>
    /// Base class for MCP tools with common functionality.
    /// </summary>
    public abstract class BaseMcpTool : IMcpTool
    {
        public abstract string GetName();
        public abstract string GetDescription();
        public abstract object Execute(JsonElement parameters);

        public virtual object GetInputSchema()
        {
            return new
            {
                type = "object",
                properties = new { },
                required = new string[] { }
            };
        }

        protected string GetStringParam(JsonElement parameters, string paramName, string defaultValue = null)
        {
            if (parameters.TryGetProperty(paramName, out JsonElement element))
            {
                return element.GetString();
            }
            return defaultValue;
        }

        protected int GetIntParam(JsonElement parameters, string paramName, int defaultValue = 0)
        {
            if (parameters.TryGetProperty(paramName, out JsonElement element))
            {
                return element.GetInt32();
            }
            return defaultValue;
        }

        protected bool GetBoolParam(JsonElement parameters, string paramName, bool defaultValue = false)
        {
            if (parameters.TryGetProperty(paramName, out JsonElement element))
            {
                return element.GetBoolean();
            }
            return defaultValue;
        }

        protected float GetFloatParam(JsonElement parameters, string paramName, float defaultValue = 0f)
        {
            if (parameters.TryGetProperty(paramName, out JsonElement element))
            {
                return element.GetSingle();
            }
            return defaultValue;
        }
    }
}
