namespace UnityMcp.Core.Interfaces;

/// <summary>
/// Facade over focused Unity MCP service interfaces. Preserves existing tool DI.
/// </summary>
public interface IUnityService : IUnityProjectService, IUnitySceneService, IUnityAssetService, IUnityValidationService
{
    /// <summary>
    /// How an agent should treat existing files when applying changes.
    /// </summary>
    public enum AgentEditMode
    {
        /// <summary>Create only; fail if file exists.</summary>
        CreateOnly = 0,
        /// <summary>Edit only; fail if file does not exist.</summary>
        EditOnly = 1,
        /// <summary>Create or edit as appropriate (auto).</summary>
        CreateOrEdit = 2,
    }
}
