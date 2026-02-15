using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using UnityMcp.Application.Routing;
using UnityMcp.Core.Interfaces;
using UnityMcp.Core.Models;

namespace UnityMcp.Application.Handlers.Tools;

/// <summary>
/// Parameters for creating a GameObject.
/// </summary>
public class CreateGameObjectParameters
{
    /// <summary>
    /// Relative path to the scene file.
    /// </summary>
    [Required]
    public string ScenePath { get; set; } = string.Empty;

    /// <summary>
    /// Name of the new GameObject.
    /// </summary>
    [Required]
    public string GameObjectName { get; set; } = string.Empty;
}

/// <summary>
/// Handler for creating a new GameObject.
/// </summary>
public class CreateGameObjectHandler : McpHandlerBase<CreateGameObjectParameters, string>
{
    private readonly IUnityService _unityService;

    public CreateGameObjectHandler(IUnityService unityService)
    {
        _unityService = unityService;
    }

    /// <inheritdoc />
    public override string Method => McpMethods.CreateGameObject;

    /// <inheritdoc />
    public override async Task<string> HandleAsync(CreateGameObjectParameters parameters)
    {
        try 
        {
            await _unityService.CreateGameObjectAsync(parameters.ScenePath, parameters.GameObjectName);
            return $"GameObject {parameters.GameObjectName} created in {parameters.ScenePath}";
        }
        catch (NotSupportedException ex)
        {
            return $"Feature not available in standalone mode: {ex.Message} Please use 'create_script' or 'create_scene'.";
        }
    }
}
