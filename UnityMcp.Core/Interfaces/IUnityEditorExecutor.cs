namespace UnityMcp.Core.Interfaces;

/// <summary>
/// Executes typed operations inside the Unity Editor via live bridge or batch mode.
/// The MCP server never references Unity assemblies.
/// </summary>
public interface IUnityEditorExecutor
{
    /// <summary>
    /// Executes a named Editor operation and returns a ToolResultEnvelope JSON string.
    /// Prefers live bridge when connected; otherwise batch mode when Unity is available.
    /// </summary>
    Task<string> ExecuteAsync(
        string projectPath,
        string operation,
        IReadOnlyDictionary<string, object?>? args = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when a live Editor bridge endpoint is available for the project.
    /// </summary>
    bool TryGetLiveBridgeStatus(string projectPath, out bool connected, out string? unityVersion);

    /// <summary>Locates a Unity Editor executable, or null when unavailable.</summary>
    string? FindUnityExecutable();

    /// <summary>
    /// Installs the UnityEditorBridge package sources into the target project.
    /// </summary>
    Task EnsureBridgeInstalledAsync(string projectPath, CancellationToken cancellationToken = default);
}
