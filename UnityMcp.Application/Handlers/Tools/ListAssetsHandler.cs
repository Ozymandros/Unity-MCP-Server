using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using UnityMcp.Application.Routing;
using UnityMcp.Core.Interfaces;
using UnityMcp.Core.Models;

namespace UnityMcp.Application.Handlers.Tools;

/// <summary>
/// Parameters for listing assets.
/// </summary>
public class ListAssetsParameters
{
    /// <summary>
    /// Relative path to start listing from.
    /// </summary>
    [Required]
    public string Path { get; set; } = "Assets";

    /// <summary>
    /// Search pattern.
    /// </summary>
    [Required]
    public string Pattern { get; set; } = "*";
}

/// <summary>
/// Handler for listing Unity assets.
/// </summary>
public class ListAssetsHandler : McpHandlerBase<ListAssetsParameters, IEnumerable<string>>
{
    private readonly IUnityService _unityService;

    public ListAssetsHandler(IUnityService unityService)
    {
        _unityService = unityService;
    }

    /// <inheritdoc />
    public override string Method => McpMethods.ListAssets;

    /// <inheritdoc />
    public override async Task<IEnumerable<string>> HandleAsync(ListAssetsParameters parameters)
    {
        var assets = await _unityService.ListAssetsAsync(parameters.Path, parameters.Pattern);
        return assets;
    }
}
