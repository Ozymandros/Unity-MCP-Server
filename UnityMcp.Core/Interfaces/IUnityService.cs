namespace UnityMcp.Core.Interfaces;

/// <summary>
/// Facade over focused Unity MCP service interfaces. Preserves existing tool DI.
/// </summary>
public interface IUnityService : IUnityProjectService, IUnitySceneService, IUnityAssetService, IUnityValidationService
{
}
