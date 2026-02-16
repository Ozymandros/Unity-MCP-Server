using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using UnityMcp.Application.Routing;
using UnityMcp.Core.Interfaces;
using UnityMcp.Core.Models;

namespace UnityMcp.Application.Handlers.Tools;

/// <summary>
/// Parameters for building a project.
/// </summary>
public class BuildProjectParameters
{
    /// <summary>
    /// The target platform (e.g. Win64, OSX, Linux).
    /// </summary>
    [Required]
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// The output path for the build.
    /// </summary>
    [Required]
    public string OutputPath { get; set; } = string.Empty;
}

/// <summary>
/// Handler for building the Unity project.
/// </summary>
public class BuildProjectHandler : McpHandlerBase<BuildProjectParameters, string>
{
    private readonly IUnityService _unityService;

    public BuildProjectHandler(IUnityService unityService)
    {
        _unityService = unityService;
    }

    /// <inheritdoc />
    public override string Method => McpMethods.BuildProject;

    /// <inheritdoc />
    public override async Task<string> HandleAsync(BuildProjectParameters parameters)
    {
        await _unityService.BuildProjectAsync(parameters.Target, parameters.OutputPath);
        return $"Build initiated for target {parameters.Target} to {parameters.OutputPath}";
    }
}
