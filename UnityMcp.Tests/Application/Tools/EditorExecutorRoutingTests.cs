using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using UnityMcp.Core.Interfaces;
using UnityMcp.Infrastructure.Services;

namespace UnityMcp.Tests.Application.Tools;

[TestFixture]
public class EditorExecutorRoutingTests
{
    [Test]
    public async Task ValidateImport_UsesExecutor_WhenEditorAvailable()
    {
        var logger = Substitute.For<ILogger<FileUnityService>>();
        var processRunner = Substitute.For<IProcessRunner>();
        var executor = Substitute.For<IUnityEditorExecutor>();
        executor.FindUnityExecutable().Returns(@"C:\Unity\Editor\Unity.exe");
        executor.ExecuteAsync(Arg.Any<string>(), "validate_import", Arg.Any<IReadOnlyDictionary<string, object?>?>(), Arg.Any<CancellationToken>())
            .Returns("{\"success\":true,\"error_count\":0,\"warning_count\":0,\"errors\":[],\"warnings\":[],\"message\":\"ok\"}");

        var service = new FileUnityService(logger, processRunner, editorExecutor: executor);
        string json = await service.ValidateImportAsync(@"C:\proj");

        Assert.That(json, Does.Contain("\"success\":true"));
        await executor.Received(1).ExecuteAsync(@"C:\proj", "validate_import", Arg.Any<IReadOnlyDictionary<string, object?>?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Capabilities_ReportLiveBridgeFlag()
    {
        var logger = Substitute.For<ILogger<FileUnityService>>();
        var processRunner = Substitute.For<IProcessRunner>();
        var executor = Substitute.For<IUnityEditorExecutor>();
        executor.FindUnityExecutable().Returns(@"C:\Unity\Editor\Unity.exe");
        bool connected;
        string? version;
        executor.TryGetLiveBridgeStatus(Arg.Any<string>(), out connected, out version)
            .Returns(x =>
            {
                x[1] = true;
                x[2] = "6000.0.0f1";
                return true;
            });

        var service = new FileUnityService(logger, processRunner, editorExecutor: executor);
        // Seed project path via IsValidProjectAsync side effect is optional; capabilities still works.
        string caps = await service.GetCapabilitiesAsync();
        using var doc = JsonDocument.Parse(caps);
        Assert.That(caps, Does.Contain("native-editor").Or.Contain("file+"));
        Assert.That(doc.RootElement.GetProperty("server").TryGetProperty("liveBridgeConnected", out _), Is.True);
    }

    [Test]
    public async Task AddSceneGameObject_FallsBackToFile_WithoutEditor()
    {
        var mockFs = new System.IO.Abstractions.TestingHelpers.MockFileSystem();
        var logger = Substitute.For<ILogger<FileUnityService>>();
        var processRunner = Substitute.For<IProcessRunner>();
        var service = new FileUnityService(logger, processRunner, mockFs);

        string proj = await service.ScaffoldProjectAsync("AddGo", @"C:\output");
        await service.CreateSceneAsync(proj, "Assets/Scenes/Main.unity");
        string json = await service.AddSceneGameObjectAsync(proj, "Assets/Scenes/Main.unity", "Main Camera", "Child");

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("Scene.ParentIgnoredFileOnly"));
    }
}
