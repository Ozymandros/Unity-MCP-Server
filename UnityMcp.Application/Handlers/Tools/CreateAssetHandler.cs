using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using UnityMcp.Application.Routing;
using UnityMcp.Core.Interfaces;
using UnityMcp.Core.Models;

namespace UnityMcp.Application.Handlers.Tools;

/// <summary>
/// Parameters for creating a generic asset.
/// </summary>
public class CreateAssetParameters
{
    /// <summary>
    /// Relative path to the new asset.
    /// </summary>
    [Required]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Content of the asset.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Handler for creating a new generic asset.
/// </summary>
public class CreateAssetHandler : McpHandlerBase<CreateAssetParameters, string>
{
    private readonly IUnityService _unityService;

    public CreateAssetHandler(IUnityService unityService)
    {
        _unityService = unityService;
    }

    /// <inheritdoc />
    public override string Method => McpMethods.CreateAsset;

    /// <inheritdoc />
    public override async Task<string> HandleAsync(CreateAssetParameters parameters)
    {
        await _unityService.CreateAssetAsync(parameters.Path, parameters.Content);
        return $"Asset created at {parameters.Path}";
    }
}
