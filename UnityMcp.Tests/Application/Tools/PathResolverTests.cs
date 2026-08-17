using UnityMcp.Infrastructure.Services;

namespace UnityMcp.Tests.Application.Tools;

[TestFixture]
public class PathResolverTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "UnityMcpPathTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "Assets", "Scripts"));
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    [Test]
    public void ResolveUnderProject_RejectsUriSchemes()
    {
        var resolver = PathResolver.ForProject(_root);
        Assert.Throws<InvalidOperationException>(() => resolver.ResolveUnderProject(_root, "https://example.com/x"));
        Assert.Throws<InvalidOperationException>(() => resolver.ResolveUnderProject(_root, "file://C:/outside"));
    }

    [Test]
    public void ResolveUnderProject_RejectsTraversalEscape()
    {
        var resolver = PathResolver.ForProject(_root);
        Assert.Throws<InvalidOperationException>(() => resolver.ResolveUnderProject(_root, "../outside.txt"));
        Assert.Throws<InvalidOperationException>(() => resolver.ResolveUnderProject(_root, "Assets/../../outside.txt"));
    }

    [Test]
    public void ResolveUnderProject_RejectsOutOfProjectAbsolutePath()
    {
        var resolver = PathResolver.ForProject(_root);
        string outside = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "UnityMcpOutside", Guid.NewGuid().ToString("N"), "x.txt"));
        Assert.Throws<InvalidOperationException>(() => resolver.ResolveUnderProject(_root, outside));
    }

    [Test]
    public void ResolveUnderProject_MergesDuplicateAssetsSegment()
    {
        var resolver = PathResolver.ForProject(_root);
        string resolved = resolver.ResolveUnderProject(_root, "Assets/Scripts/Player.cs");
        Assert.That(resolved.Replace('\\', '/'), Does.EndWith("Assets/Scripts/Player.cs"));
        Assert.That(resolved.Replace('\\', '/').Split("Assets/").Length - 1, Is.EqualTo(1));
    }

    [Test]
    public void ResolveUnderProject_ResolvesRelativeInsideProject()
    {
        var resolver = PathResolver.ForProject(_root);
        string resolved = resolver.ResolveUnderProject(_root, "Assets/Scripts/Foo.cs");
        Assert.That(resolved.StartsWith(Path.GetFullPath(_root), StringComparison.OrdinalIgnoreCase));
        Assert.That(Directory.Exists(Path.GetDirectoryName(resolved)!));
    }

    [Test]
    public void GetProjectRelativePath_ReturnsForwardSlashRelative()
    {
        var resolver = PathResolver.ForProject(_root);
        string absolute = resolver.ResolveUnderProject(_root, "Assets/Scripts/Foo.cs");
        string relative = resolver.GetProjectRelativePath(absolute);
        Assert.That(relative, Is.EqualTo("Assets/Scripts/Foo.cs"));
    }
}
