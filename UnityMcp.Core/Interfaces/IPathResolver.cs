namespace UnityMcp.Core.Interfaces;

/// <summary>
/// Resolves and validates paths under a Unity project root.
/// </summary>
public interface IPathResolver
{
    /// <summary>Absolute project root path.</summary>
    string ProjectRoot { get; }

    /// <summary>
    /// Resolves a path to an absolute filesystem path inside the project.
    /// Accepts absolute paths and paths relative to the project root.
    /// </summary>
    string ResolvePath(string path);

    /// <summary>
    /// Resolves <paramref name="nameOrPath"/> under an explicit project root (per-call projectPath).
    /// </summary>
    string ResolveUnderProject(string projectRoot, string nameOrPath);

    /// <summary>Returns a project-relative path using forward slashes.</summary>
    string GetProjectRelativePath(string absolutePath);

    /// <summary>
    /// Validates that an absolute path is within project boundaries after full-path normalization
    /// (rejects traversal and out-of-project targets; follows resolved final paths where possible).
    /// </summary>
    void EnsureInsideProject(string absolutePath);
}
