using UnityMcp.Core;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Infrastructure.Services;

/// <summary>
/// Resolves project-relative paths and enforces project-root boundaries.
/// </summary>
public sealed class PathResolver : IPathResolver
{
    public PathResolver(string projectRoot)
    {
        if (string.IsNullOrWhiteSpace(projectRoot))
            throw new ArgumentException("Project root is required.", nameof(projectRoot));

        ProjectRoot = Path.GetFullPath(projectRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public string ProjectRoot { get; }

    public static PathResolver ForProject(string projectRoot) => new(projectRoot);

    public string ResolvePath(string path) => ResolveUnderProject(ProjectRoot, path);

    public string ResolveUnderProject(string projectRoot, string nameOrPath)
    {
        if (string.IsNullOrWhiteSpace(nameOrPath))
            throw new InvalidOperationException("Path is required.");

        var root = Path.GetFullPath(projectRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var trimmed = nameOrPath.Trim();

        if (ProjectPathSyntax.ContainsUriSchemeAuthority(trimmed))
            throw new InvalidOperationException("Path schemes are not supported. Use absolute or project-relative filesystem paths.");

        trimmed = ProjectPathSyntax.CollapseDuplicateDirectorySeparators(trimmed);

        if (ProjectPathSyntax.IsUncPath(trimmed))
        {
            var uncFull = ResolveFinalPath(trimmed);
            EnsureInside(root, uncFull);
            return uncFull;
        }

        if (OperatingSystem.IsWindows() && !ProjectPathSyntax.IsWindowsDriveAbsolutePath(trimmed))
        {
            var withoutLeading = ProjectPathSyntax.TrimAllLeadingDirectorySeparators(trimmed);
            if (string.IsNullOrEmpty(withoutLeading))
            {
                EnsureInside(root, root);
                return root;
            }

            trimmed = withoutLeading;
        }

        if (Path.IsPathRooted(trimmed))
        {
            var full = ResolveFinalPath(trimmed);
            if (IsWithin(root, full))
                return full;

            if (!OperatingSystem.IsWindows()
                && ProjectPathSyntax.ShouldReinterpretUnixAbsoluteAsProjectRelative(full))
            {
                return ResolveRelative(root, ProjectPathSyntax.TrimAllLeadingDirectorySeparators(trimmed));
            }

            EnsureInside(root, full);
            return full;
        }

        return ResolveRelative(root, trimmed);
    }

    public string GetProjectRelativePath(string absolutePath)
    {
        var full = ResolveFinalPath(absolutePath);
        EnsureInsideProject(full);
        return Path.GetRelativePath(ProjectRoot, full).Replace('\\', '/');
    }

    public void EnsureInsideProject(string absolutePath)
        => EnsureInside(ProjectRoot, ResolveFinalPath(absolutePath));

    private static string ResolveRelative(string root, string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/');
        var relative = normalized.TrimStart('/');
        var combined = ProjectPathSyntax.CombineAvoidingDuplicateSegments(root, relative);
        var finalPath = ResolveFinalPath(combined);
        EnsureInside(root, finalPath);
        return finalPath;
    }

    private static void EnsureInside(string projectRoot, string absolutePath)
    {
        if (!IsWithin(projectRoot, absolutePath))
            throw new InvalidOperationException($"Path escapes project root: {absolutePath}");
    }

    private static bool IsWithin(string projectRoot, string absolutePath)
    {
        var fullPath = absolutePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedRoot = projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var rootWithSeparator = normalizedRoot + Path.DirectorySeparatorChar;

        return string.Equals(fullPath, normalizedRoot, StringComparison.OrdinalIgnoreCase)
               || fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resolves to a canonical full path. When the path exists, also attempts to resolve
    /// the final target of junctions/symlinks via the existing filesystem entry.
    /// </summary>
    private static string ResolveFinalPath(string path)
    {
        var full = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        try
        {
            if (Directory.Exists(full))
            {
                return new DirectoryInfo(full).FullName
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            if (File.Exists(full))
            {
                return new FileInfo(full).FullName
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            // For not-yet-created paths, resolve parent if it exists.
            var parent = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
            {
                var parentFull = new DirectoryInfo(parent).FullName
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return Path.Combine(parentFull, Path.GetFileName(full));
            }
        }
        catch
        {
            // Fall back to GetFullPath result.
        }

        return full;
    }
}
