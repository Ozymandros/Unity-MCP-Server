using System.Collections.Generic;
using System.Threading.Tasks;
using UnityMcp.Application.Models;
using UnityMcp.Application.Routing;
using UnityMcp.Core.Models;

namespace UnityMcp.Application.Handlers.Core;

/// <summary>
/// Handles tool listing.
/// </summary>
public class ListToolsHandler : McpHandlerBase<EmptyParameters, ListToolsResult>
{
    /// <inheritdoc />
    public override string Method => McpMethods.ListTools;

    /// <inheritdoc />
    public override Task<ListToolsResult> HandleAsync(EmptyParameters parameters)
    {
        var tools = new List<McpTool>
        {
            new McpTool
            {
                Name = McpMethods.Ping,
                Description = "Check server connectivity",
                InputSchema = new
                {
                    type = "object",
                    properties = new { }
                }
            },
            new McpTool
            {
                Name = McpMethods.CreateScene,
                Description = "Creates a new Unity scene file",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string", description = "Relative path to the new scene (e.g. Assets/Scenes/NewScene.unity)" }
                    },
                    required = new[] { "path" }
                }
            },
                new McpTool
            {
                Name = McpMethods.CreateScript,
                Description = "Creates a new C# MonoBehaviour script",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string", description = "Relative path to the new script (e.g. Assets/Scripts/Player.cs)" },
                        scriptName = new { type = "string", description = "Name of the class/script (e.g. Player)" }
                    },
                    required = new[] { "path", "scriptName" }
                }
            },
            new McpTool
            {
                Name = McpMethods.ListAssets,
                Description = "Lists files in the Unity project Assets folder",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string", description = "Relative path to start listing from (default: Assets)" },
                        pattern = new { type = "string", description = "Search pattern (default: *)" }
                    }
                }
            },
            new McpTool
            {
                Name = McpMethods.BuildProject,
                Description = "Builds the Unity project for a specific target (requires Unity installed)",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        target = new { type = "string", description = "Build target (Win64, OSX, Linux64)" },
                        outputPath = new { type = "string", description = "Absolute path for the build output" }
                    },
                    required = new[] { "target", "outputPath" }
                }
            },
            new McpTool
            {
                Name = McpMethods.CreateGameObject,
                Description = "Creates a new GameObject in a scene",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        scenePath = new { type = "string", description = "Relative path to the scene file" },
                        gameObjectName = new { type = "string", description = "Name of the new GameObject" }
                    },
                    required = new[] { "scenePath", "gameObjectName" }
                }
            },
            new McpTool
            {
                Name = McpMethods.CreateAsset,
                Description = "Creates a new generic asset file",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string", description = "Relative path to the new asset" },
                        content = new { type = "string", description = "Text content of the asset" }
                    },
                    required = new[] { "path" }
                }
            }
        };

        return Task.FromResult(new ListToolsResult { Tools = tools });
    }
}
