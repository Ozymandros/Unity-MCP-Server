using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using UnityMcp.Application.Routing;
using UnityMcp.Core.Interfaces;
using UnityMcp.Core.Models;

namespace UnityMcp.Application.Handlers.Tools;

/// <summary>
/// Parameters for creating a scene.
/// </summary>
public class CreateSceneParameters
{
    /// <summary>
    /// Relative path to the new scene.
    /// </summary>
    [Required]
    public string Path { get; set; } = string.Empty;
}

/// <summary>
/// Handler for creating a new Unity scene.
/// </summary>
public class CreateSceneHandler : McpHandlerBase<CreateSceneParameters, string>
{
    private readonly IUnityService _unityService;

    public CreateSceneHandler(IUnityService unityService)
    {
        _unityService = unityService;
    }

    /// <inheritdoc />
    public override string Method => McpMethods.CreateScene;

    /// <inheritdoc />
    public override async Task<string> HandleAsync(CreateSceneParameters parameters)
    {
        await _unityService.CreateSceneAsync(parameters.Path);
        return $"Scene created at {parameters.Path}";
    }
}
