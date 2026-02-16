using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using UnityMcp.Application.Routing;
using UnityMcp.Core.Interfaces;
using UnityMcp.Core.Models;

namespace UnityMcp.Application.Handlers.Tools;

/// <summary>
/// Parameters for creating a script.
/// </summary>
public class CreateScriptParameters
{
    /// <summary>
    /// Relative path to the new script.
    /// </summary>
    [Required]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Name of the class/script.
    /// </summary>
    [Required]
    [RegularExpression(@"^[a-zA-Z_][a-zA-Z0-9_]*$", ErrorMessage = "ScriptName must be a valid C# identifier.")]
    public string ScriptName { get; set; } = string.Empty;
}

/// <summary>
/// Handler for creating a new C# script.
/// </summary>
public class CreateScriptHandler : McpHandlerBase<CreateScriptParameters, string>
{
    private readonly IUnityService _unityService;

    public CreateScriptHandler(IUnityService unityService)
    {
        _unityService = unityService;
    }

    /// <inheritdoc />
    public override string Method => McpMethods.CreateScript;

    /// <inheritdoc />
    public override async Task<string> HandleAsync(CreateScriptParameters parameters)
    {
        await _unityService.CreateScriptAsync(parameters.Path, parameters.ScriptName);
        return $"Script {parameters.ScriptName} created at {parameters.Path}";
    }
}
