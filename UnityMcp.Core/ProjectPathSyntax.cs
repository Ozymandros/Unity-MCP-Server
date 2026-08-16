using System.Text;

namespace UnityMcp.Core;

/// <summary>
/// Central rules for filesystem paths passed into Unity MCP tools: mixed separators, optional leading
/// separators, UNC / drive paths, and safe Unix absolute-path handling.
/// </summary>
public static class ProjectPathSyntax
{
    public static readonly HashSet<string> UnixHostRootFirstSegments =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "afs", "bin", "boot", "cores", "dev", "etc", "home", "lib", "lib64", "lost+found", "media", "mnt",
            "net", "nix", "opt", "private", "proc", "root", "run", "sbin", "srv", "sys", "system", "tmp", "usr",
            "var", "users", "volumes", "snap", "containers",
            "applications", "library", "network", "system"
        };

    public static bool IsUncPath(string trimmed)
    {
        if (trimmed.Length < 2)
            return false;

        if (trimmed[0] == '\\' && trimmed[1] == '\\')
            return true;

        if (OperatingSystem.IsWindows() && trimmed[0] == '/' && trimmed[1] == '/')
            return trimmed.Length >= 3 && trimmed[2] != '/';

        return false;
    }

    public static bool IsWindowsDriveAbsolutePath(string trimmed)
    {
        if (trimmed.Length >= 2 && char.IsAsciiLetter(trimmed[0]) && trimmed[1] == ':')
            return true;

        return trimmed.Length >= 3
               && trimmed[0] is '/' or '\\'
               && char.IsAsciiLetter(trimmed[1])
               && trimmed[2] == ':';
    }

    public static bool IsWindowsDriveLetterPathPrefix(string path)
    {
        if (path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':')
            return true;

        return path.Length >= 3
               && path[0] is '/' or '\\'
               && char.IsAsciiLetter(path[1])
               && path[2] == ':';
    }

    public static string CollapseDuplicateDirectorySeparators(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        ReadOnlySpan<char> rest = path.AsSpan();
        if (rest.Length >= 2 && rest[0] == '\\' && rest[1] == '\\')
            return "\\\\" + CollapseDuplicateSeparatorsInFragment(rest[2..]);

        if (OperatingSystem.IsWindows() && rest.Length >= 2 && rest[0] == '/' && rest[1] == '/'
            && (rest.Length < 3 || rest[2] != '/'))
            return "//" + CollapseDuplicateSeparatorsInFragment(rest[2..]);

        return CollapseDuplicateSeparatorsInFragment(rest);
    }

    public static bool ContainsUriSchemeAuthority(string path)
    {
        if (!path.Contains("://", StringComparison.Ordinal))
            return false;

        return !IsWindowsDriveLetterPathPrefix(path);
    }

    private static string CollapseDuplicateSeparatorsInFragment(ReadOnlySpan<char> s)
    {
        if (s.IsEmpty)
            return string.Empty;

        var sb = new StringBuilder(s.Length);
        var i = 0;
        while (i < s.Length)
        {
            var c = s[i];
            if (c == '/')
            {
                sb.Append('/');
                i++;
                while (i < s.Length && s[i] == '/')
                    i++;
                continue;
            }

            if (c == '\\')
            {
                sb.Append('\\');
                i++;
                while (i < s.Length && s[i] == '\\')
                    i++;
                continue;
            }

            sb.Append(c);
            i++;
        }

        return sb.ToString();
    }

    public static string TrimAllLeadingDirectorySeparators(string path)
    {
        var i = 0;
        while (i < path.Length && (path[i] == '/' || path[i] == '\\'))
            i++;

        return i == 0 ? path : path[i..];
    }

    public static bool ShouldReinterpretUnixAbsoluteAsProjectRelative(string fullResolved)
    {
        var root = Path.GetPathRoot(fullResolved);
        if (string.IsNullOrEmpty(root))
            return false;

        var full = fullResolved.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (full.Equals(normalizedRoot, StringComparison.Ordinal))
            return false;

        var tail = full[root.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var end = tail.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
        var firstSegment = end < 0 ? tail : tail[..end];

        return !UnixHostRootFirstSegments.Contains(firstSegment);
    }

    public static string NormalizeRelativePathTokenForCombine(string fileName)
    {
        var t = CollapseDuplicateDirectorySeparators(fileName.Trim());
        if (string.IsNullOrEmpty(t))
            return t;

        if (IsUncPath(t))
            return t.Replace('\\', '/');

        if (OperatingSystem.IsWindows())
        {
            if (!IsWindowsDriveAbsolutePath(t))
                t = TrimAllLeadingDirectorySeparators(t);

            return t.Replace('\\', '/').TrimStart('/');
        }

        if (t.StartsWith('/') && t.Length > 1)
        {
            var full = Path.GetFullPath(t);
            if (ShouldReinterpretUnixAbsoluteAsProjectRelative(full))
                t = TrimAllLeadingDirectorySeparators(t);
        }

        return t.Replace('\\', '/').TrimStart('/');
    }

    public static string CombineAvoidingDuplicateSegments(string baseDir, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return Path.GetFullPath(baseDir);

        var separator = Path.DirectorySeparatorChar;
        var rootSegments = baseDir
            .Split([separator, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        var relativeSegments = relativePath
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var strip = 0;
        var rootIndex = rootSegments.Length - 1;
        while (rootIndex >= 0
               && strip < relativeSegments.Length
               && string.Equals(rootSegments[rootIndex], relativeSegments[strip], StringComparison.OrdinalIgnoreCase))
        {
            strip++;
            rootIndex--;
        }

        if (strip >= relativeSegments.Length)
            strip = 0;

        var tail = string.Join(separator.ToString(), relativeSegments.Skip(strip));
        return Path.GetFullPath(Path.Combine(baseDir, tail));
    }
}
