using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using UnityMcp.Application.Tools;
using UnityMcp.Core.Interfaces;
using UnityMcp.Infrastructure.Services;
using UnityMcp.Infrastructure.Unity;

namespace UnityMcp.Tests.Application.Tools;

[TestFixture]
public class UnityToolsTests
{
    private IUnityService _unityService = null!;

    [SetUp]
    public void SetUp()
    {
        _unityService = Substitute.For<IUnityService>();
    }

    // -----------------------------------------------------------------------
    // Ping
    // -----------------------------------------------------------------------

    [Test]
    public void Ping_ReturnsPong()
    {
        Assert.That(UnityTools.Ping(), Is.EqualTo("pong"));
    }

    // -----------------------------------------------------------------------
    // CreateScene
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreateScene_ValidPath_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.CreateScene(_unityService, "Scenes/TestScene.unity");
        await _unityService.Received(1).CreateSceneAsync("Scenes/TestScene.unity", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Scenes/TestScene.unity"));
    }

    // -----------------------------------------------------------------------
    // CreateScript
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreateScript_ValidParams_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.CreateScript(_unityService, "Scripts/Player.cs", "Player");
        await _unityService.Received(1).CreateScriptAsync("Scripts/Player.cs", "Player", null, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Player"));
    }

    [Test]
    public async Task CreateScript_WithAIContent_PassesContentToService()
    {
        string aiCode = "using UnityEngine;\npublic class Player : MonoBehaviour { void Update() { } }";
        var result = await UnityTools.CreateScript(_unityService, "Scripts/Player.cs", "Player", aiCode);
        await _unityService.Received(1).CreateScriptAsync("Scripts/Player.cs", "Player", aiCode, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Player"));
    }

    // -----------------------------------------------------------------------
    // ListAssets
    // -----------------------------------------------------------------------

    [Test]
    public async Task ListAssets_ValidRequest_ReturnsAssetsList()
    {
        var expected = new List<string> { "Assets/Textures/Player.png", "Assets/Textures/Ground.png" };
        _unityService.ListAssetsAsync("Assets/Textures", "*.png", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IEnumerable<string>)expected));

        var result = await UnityTools.ListAssets(_unityService, "Assets/Textures", "*.png");
        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task ListAssets_NoMatches_ReturnsEmptyList()
    {
        _unityService.ListAssetsAsync("Assets/Audio", "*.wav", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Enumerable.Empty<string>()));
        var result = await UnityTools.ListAssets(_unityService, "Assets/Audio", "*.wav");
        Assert.That(result, Is.Empty);
    }

    // -----------------------------------------------------------------------
    // BuildProject
    // -----------------------------------------------------------------------

    [Test]
    public async Task BuildProject_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.BuildProject(_unityService, "Win64", "C:/Builds/Output");
        await _unityService.Received(1).BuildProjectAsync("Win64", "C:/Builds/Output", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Win64"));
    }

    // -----------------------------------------------------------------------
    // CreateAsset
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreateAsset_ValidParams_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.CreateAsset(_unityService, "Materials/Test.mat", "content");
        await _unityService.Received(1).CreateAssetAsync("Materials/Test.mat", "content", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Materials/Test.mat"));
    }

    [Test]
    public async Task CreateAsset_EmptyContent_DefaultsToEmptyString()
    {
        var result = await UnityTools.CreateAsset(_unityService, "Materials/Empty.mat");
        await _unityService.Received(1).CreateAssetAsync("Materials/Empty.mat", "", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Materials/Empty.mat"));
    }

    // -----------------------------------------------------------------------
    // CreateGameObject (legacy)
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreateGameObject_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.CreateGameObject(_unityService, "Scenes/Main.unity", "Cube");
        await _unityService.Received(1).CreateGameObjectAsync("Scenes/Main.unity", "Cube", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Cube"));
    }

    // -----------------------------------------------------------------------
    // CreateDetailedScene (NEW)
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreateDetailedScene_ValidJson_CallsServiceAndReturnsMessage()
    {
        string json = """[{"name":"Camera","tag":"MainCamera","position":{"x":0,"y":1,"z":-10},"components":[{"type":"Camera"}]}]""";
        var result = await UnityTools.CreateDetailedScene(_unityService, "Assets/Scenes/Level1.unity", json);
        await _unityService.Received(1).CreateDetailedSceneAsync("Assets/Scenes/Level1.unity", json, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Level1.unity"));
    }

    // -----------------------------------------------------------------------
    // AddGameObject (NEW)
    // -----------------------------------------------------------------------

    [Test]
    public async Task AddGameObject_ValidJson_CallsServiceAndReturnsMessage()
    {
        string json = """{"name":"Sphere","position":{"x":3,"y":0,"z":0},"components":[{"type":"MeshFilter","mesh":"Sphere"},{"type":"MeshRenderer"}]}""";
        var result = await UnityTools.AddGameObject(_unityService, "Scenes/Main.unity", json);
        await _unityService.Received(1).AddGameObjectToSceneAsync("Scenes/Main.unity", json, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Scenes/Main.unity"));
    }

    // -----------------------------------------------------------------------
    // CreateMaterial (NEW)
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreateMaterial_ValidJson_CallsServiceAndReturnsMessage()
    {
        string json = """{"name":"RedMetal","color":{"r":1,"g":0,"b":0,"a":1},"metallic":0.8,"smoothness":0.6}""";
        var result = await UnityTools.CreateMaterial(_unityService, "Assets/Materials/RedMetal.mat", json);
        await _unityService.Received(1).CreateMaterialAsync("Assets/Materials/RedMetal.mat", json, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("RedMetal.mat"));
    }

    // -----------------------------------------------------------------------
    // CreatePrefab (NEW)
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreatePrefab_ValidJson_CallsServiceAndReturnsMessage()
    {
        string json = """{"name":"Enemy","components":[{"type":"MeshFilter","mesh":"Capsule"},{"type":"Rigidbody","mass":2}]}""";
        var result = await UnityTools.CreatePrefab(_unityService, "Assets/Prefabs/Enemy.prefab", json);
        await _unityService.Received(1).CreatePrefabAsync("Assets/Prefabs/Enemy.prefab", json, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Enemy.prefab"));
    }

    // -----------------------------------------------------------------------
    // ReadAsset (NEW)
    // -----------------------------------------------------------------------

    [Test]
    public async Task ReadAsset_ReturnsFileContent()
    {
        _unityService.ReadAssetAsync("Assets/Scripts/Player.cs", Arg.Any<CancellationToken>())
            .Returns("using UnityEngine; class Player {}");

        var result = await UnityTools.ReadAsset(_unityService, "Assets/Scripts/Player.cs");
        Assert.That(result, Does.Contain("using UnityEngine"));
    }

    // -----------------------------------------------------------------------
    // DeleteAsset (NEW)
    // -----------------------------------------------------------------------

    [Test]
    public async Task DeleteAsset_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.DeleteAsset(_unityService, "Assets/Old/Legacy.cs");
        await _unityService.Received(1).DeleteAssetAsync("Assets/Old/Legacy.cs", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Deleted"));
    }
}

// ============================================================================
// Unity YAML Writer Tests (infrastructure)
// ============================================================================

[TestFixture]
public class UnityYamlWriterTests
{
    [SetUp]
    public void SetUp()
    {
        UnityYamlWriter.ResetFileIdCounter();
    }

    [Test]
    public void Header_ContainsYamlTag()
    {
        var header = UnityYamlWriter.Header();
        Assert.That(header, Does.Contain("%YAML 1.1"));
        Assert.That(header, Does.Contain("tag:unity3d.com,2011:"));
    }

    [Test]
    public void WriteScene_WithCameraAndLight_ContainsExpectedElements()
    {
        var gos = new List<GameObjectDef>
        {
            new() { Name = "Main Camera", Tag = "MainCamera", Position = new Vector3Def(0, 1, -10),
                     Components = [new ComponentDef(UnityYamlWriter.ClassId_Camera)] },
            new() { Name = "Directional Light", EulerAngles = new Vector3Def(50, -30, 0),
                     Components = [new ComponentDef(UnityYamlWriter.ClassId_Light)] },
        };

        var yaml = UnityYamlWriter.WriteScene(gos);

        Assert.That(yaml, Does.Contain("%YAML 1.1"));
        Assert.That(yaml, Does.Contain("OcclusionCullingSettings"));
        Assert.That(yaml, Does.Contain("RenderSettings"));
        Assert.That(yaml, Does.Contain("m_Name: Main Camera"));
        Assert.That(yaml, Does.Contain("m_TagString: MainCamera"));
        Assert.That(yaml, Does.Contain("m_Name: Directional Light"));
        Assert.That(yaml, Does.Contain("Camera:"));
        Assert.That(yaml, Does.Contain("Light:"));
    }

    [Test]
    public void WriteScene_WithMeshObject_ContainsMeshFilterAndRenderer()
    {
        var gos = new List<GameObjectDef>
        {
            new() { Name = "Cube", Position = new Vector3Def(0, 0.5f, 0),
                     Components = [
                         new ComponentDef(UnityYamlWriter.ClassId_MeshFilter)
                         { Properties = { ["mesh"] = "Cube" } },
                         new ComponentDef(UnityYamlWriter.ClassId_MeshRenderer),
                         new ComponentDef(UnityYamlWriter.ClassId_BoxCollider),
                     ] },
        };

        var yaml = UnityYamlWriter.WriteScene(gos);

        Assert.That(yaml, Does.Contain("MeshFilter:"));
        Assert.That(yaml, Does.Contain("MeshRenderer:"));
        Assert.That(yaml, Does.Contain("BoxCollider:"));
        Assert.That(yaml, Does.Contain("m_Name: Cube"));
    }

    [Test]
    public void WritePrefab_ContainsGameObjectAndTransform()
    {
        var go = new GameObjectDef
        {
            Name = "Enemy",
            Scale = new Vector3Def(2, 2, 2),
            Components = [
                new ComponentDef(UnityYamlWriter.ClassId_Rigidbody)
                { Properties = { ["mass"] = 5f, ["useGravity"] = true } },
            ]
        };

        var yaml = UnityYamlWriter.WritePrefab(go);

        Assert.That(yaml, Does.Contain("%YAML 1.1"));
        Assert.That(yaml, Does.Contain("m_Name: Enemy"));
        Assert.That(yaml, Does.Contain("m_LocalScale: {x: 2, y: 2, z: 2}"));
        Assert.That(yaml, Does.Contain("Rigidbody:"));
        Assert.That(yaml, Does.Contain("m_Mass: 5"));
        Assert.That(yaml, Does.Contain("m_UseGravity: 1"));
    }

    [Test]
    public void WriteMaterial_ContainsShaderAndProperties()
    {
        var mat = new MaterialDef
        {
            Name = "BluePlastic",
            Color = new ColorDef(0, 0.3f, 0.8f, 1),
            Metallic = 0.1f,
            Smoothness = 0.7f,
        };

        var yaml = UnityYamlWriter.WriteMaterial(mat);

        Assert.That(yaml, Does.Contain("%YAML 1.1"));
        Assert.That(yaml, Does.Contain("m_Name: BluePlastic"));
        Assert.That(yaml, Does.Contain("_Color:"));
        Assert.That(yaml, Does.Contain("_Metallic: 0.1"));
        Assert.That(yaml, Does.Contain("_Smoothness: 0.7"));
    }

    [Test]
    public void WriteGameObjectFragment_NoHeader()
    {
        var go = new GameObjectDef { Name = "TestGO" };
        var fragment = UnityYamlWriter.WriteGameObjectFragment(go);

        Assert.That(fragment, Does.Not.Contain("%YAML"));
        Assert.That(fragment, Does.Contain("m_Name: TestGO"));
    }

    [Test]
    public void FileIdCounter_IsSequential()
    {
        var id1 = UnityYamlWriter.NextFileId();
        var id2 = UnityYamlWriter.NextFileId();
        Assert.That(id2, Is.EqualTo(id1 + 1));
    }
}

// -----------------------------------------------------------------------
// New tools tests (scaffold, typed saves, validation, packages)
// -----------------------------------------------------------------------

[TestFixture]
public class UnityToolsNewTests
{
    private IUnityService _unityService = null!;

    [SetUp]
    public void SetUp()
    {
        _unityService = Substitute.For<IUnityService>();
    }

    // ---- Scaffold ----

    [Test]
    public async Task ScaffoldProject_CallsServiceAndReturnsPath()
    {
        _unityService.ScaffoldProjectAsync("MyGame", null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(@"C:\output\MyGame"));

        var result = await UnityTools.ScaffoldProject(_unityService, "MyGame");
        Assert.That(result, Does.Contain(@"C:\output\MyGame"));
        await _unityService.Received(1).ScaffoldProjectAsync("MyGame", null, null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ScaffoldProject_WithOutputRoot_PassesThrough()
    {
        _unityService.ScaffoldProjectAsync("Proj", @"D:\games", "2023.1.0f1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(@"D:\games\Proj"));

        var result = await UnityTools.ScaffoldProject(_unityService, "Proj", @"D:\games", "2023.1.0f1");
        Assert.That(result, Does.Contain(@"D:\games\Proj"));
    }

    // ---- GetProjectInfo ----

    [Test]
    public async Task GetProjectInfo_ReturnsServiceJson()
    {
        _unityService.GetProjectInfoAsync(@"C:\proj", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"projectName\":\"proj\"}"));

        var result = await UnityTools.GetProjectInfo(_unityService, @"C:\proj");
        Assert.That(result, Does.Contain("projectName"));
    }

    // ---- CreateFolder ----

    [Test]
    public async Task CreateFolder_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.CreateFolder(_unityService, "Assets/Custom");
        await _unityService.Received(1).CreateFolderAsync("Assets/Custom", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Assets/Custom"));
        Assert.That(result, Does.Contain(".meta"));
    }

    // ---- SaveScript ----

    [Test]
    public async Task SaveScript_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.SaveScript(_unityService, @"C:\proj", "Player.cs", "class Player {}");
        await _unityService.Received(1).SaveScriptAsync(@"C:\proj", "Player.cs", "class Player {}", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Player.cs"));
    }

    // ---- SaveText ----

    [Test]
    public async Task SaveText_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.SaveText(_unityService, @"C:\proj", "dialogue.txt", "Hello world");
        await _unityService.Received(1).SaveTextAssetAsync(@"C:\proj", "dialogue.txt", "Hello world", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("dialogue.txt"));
    }

    // ---- SaveTexture ----

    [Test]
    public async Task SaveTexture_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.SaveTexture(_unityService, @"C:\proj", "sprite.png", "AAAA");
        await _unityService.Received(1).SaveTextureAsync(@"C:\proj", "sprite.png", "AAAA", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("sprite.png"));
    }

    // ---- SaveAudio ----

    [Test]
    public async Task SaveAudio_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.SaveAudio(_unityService, @"C:\proj", "sfx.mp3", "BBBB");
        await _unityService.Received(1).SaveAudioAsync(@"C:\proj", "sfx.mp3", "BBBB", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("sfx.mp3"));
    }

    // ---- ValidateCSharp ----

    [Test]
    public async Task ValidateCSharp_CallsServiceAndReturnsResult()
    {
        _unityService.ValidateCSharpAsync("code", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"isValid\":true}"));

        var result = await UnityTools.ValidateCSharp(_unityService, "code");
        Assert.That(result, Does.Contain("isValid"));
    }

    // ---- AddPackages ----

    [Test]
    public async Task AddPackages_CallsServiceAndReturnsMessage()
    {
        string json = "{\"com.unity.render-pipelines.universal\":\"14.0.11\"}";
        var result = await UnityTools.AddPackages(_unityService, @"C:\proj", json);
        await _unityService.Received(1).AddPackagesAsync(@"C:\proj", json, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("manifest.json"));
    }
}

// -----------------------------------------------------------------------
// MetaFileWriter + FileUnityService integration tests
// -----------------------------------------------------------------------

[TestFixture]
public class MetaFileWriterTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "unity_meta_tests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Test]
    public async Task WriteScriptMeta_CreatesValidMetaFile()
    {
        string scriptPath = Path.Combine(_tempDir, "Test.cs");
        await File.WriteAllTextAsync(scriptPath, "class Test {}");
        await MetaFileWriter.WriteScriptMetaAsync(scriptPath);

        string metaPath = scriptPath + ".meta";
        Assert.That(File.Exists(metaPath), Is.True);
        string content = await File.ReadAllTextAsync(metaPath);
        Assert.That(content, Does.Contain("MonoImporter:"));
        Assert.That(content, Does.Contain("fileFormatVersion: 2"));
        Assert.That(content, Does.Contain("guid:"));
    }

    [Test]
    public async Task WriteDefaultMeta_CreatesValidMetaFile()
    {
        string txtPath = Path.Combine(_tempDir, "readme.txt");
        await File.WriteAllTextAsync(txtPath, "hello");
        await MetaFileWriter.WriteDefaultMetaAsync(txtPath);

        string content = await File.ReadAllTextAsync(txtPath + ".meta");
        Assert.That(content, Does.Contain("DefaultImporter:"));
    }

    [Test]
    public async Task WriteTextureMeta_ContainsTextureImporter()
    {
        string imgPath = Path.Combine(_tempDir, "img.png");
        await File.WriteAllBytesAsync(imgPath, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        await MetaFileWriter.WriteTextureMetaAsync(imgPath);

        string content = await File.ReadAllTextAsync(imgPath + ".meta");
        Assert.That(content, Does.Contain("TextureImporter:"));
        Assert.That(content, Does.Contain("maxTextureSize: 2048"));
    }

    [Test]
    public async Task WriteAudioMeta_ContainsAudioImporter()
    {
        string audioPath = Path.Combine(_tempDir, "clip.mp3");
        await File.WriteAllBytesAsync(audioPath, new byte[] { 0xFF, 0xFB });
        await MetaFileWriter.WriteAudioMetaAsync(audioPath);

        string content = await File.ReadAllTextAsync(audioPath + ".meta");
        Assert.That(content, Does.Contain("AudioImporter:"));
        Assert.That(content, Does.Contain("sampleRateOverride: 44100"));
    }

    [Test]
    public async Task WriteFolderMeta_ContainsFolderAsset()
    {
        string folder = Path.Combine(_tempDir, "MyFolder");
        Directory.CreateDirectory(folder);
        await MetaFileWriter.WriteFolderMetaAsync(folder);

        string content = await File.ReadAllTextAsync(folder + ".meta");
        Assert.That(content, Does.Contain("folderAsset: yes"));
        Assert.That(content, Does.Contain("DefaultImporter:"));
    }

    [Test]
    public void NewGuid_Returns32HexChars()
    {
        string guid = MetaFileWriter.NewGuid();
        Assert.That(guid.Length, Is.EqualTo(32));
        Assert.That(guid, Does.Match("^[0-9a-f]{32}$"));
    }
}

// -----------------------------------------------------------------------
// FileUnityService integration tests (new tools)
// -----------------------------------------------------------------------

[TestFixture]
public class FileUnityServiceNewToolsTests
{
    private FileUnityService _service = null!;
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "unity_svc_tests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);

        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<FileUnityService>>();
        var processRunner = Substitute.For<IProcessRunner>();
        _service = new FileUnityService(logger, processRunner);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Test]
    public async Task ScaffoldProject_CreatesAllFolders()
    {
        string projectPath = await _service.ScaffoldProjectAsync("TestProject", _tempDir);

        Assert.That(Directory.Exists(Path.Combine(projectPath, "Assets")), Is.True);
        Assert.That(Directory.Exists(Path.Combine(projectPath, "Assets", "Scripts")), Is.True);
        Assert.That(Directory.Exists(Path.Combine(projectPath, "Assets", "Textures")), Is.True);
        Assert.That(Directory.Exists(Path.Combine(projectPath, "Assets", "Audio")), Is.True);
        Assert.That(Directory.Exists(Path.Combine(projectPath, "Assets", "Text")), Is.True);
        Assert.That(Directory.Exists(Path.Combine(projectPath, "ProjectSettings")), Is.True);
        Assert.That(Directory.Exists(Path.Combine(projectPath, "Packages")), Is.True);
    }

    [Test]
    public async Task ScaffoldProject_CreatesMetaSidecars()
    {
        string projectPath = await _service.ScaffoldProjectAsync("MetaTest", _tempDir);

        // Assets folder should have a .meta
        Assert.That(File.Exists(Path.Combine(projectPath, "Assets") + ".meta"), Is.True);
        Assert.That(File.Exists(Path.Combine(projectPath, "Assets", "Scripts") + ".meta"), Is.True);
    }

    [Test]
    public async Task ScaffoldProject_CreatesProjectVersion()
    {
        string projectPath = await _service.ScaffoldProjectAsync("VersionTest", _tempDir, "2023.2.0f1");

        string versionContent = await File.ReadAllTextAsync(
            Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt"));
        Assert.That(versionContent, Does.Contain("2023.2.0f1"));
    }

    [Test]
    public async Task ScaffoldProject_IsIdempotent()
    {
        string path1 = await _service.ScaffoldProjectAsync("Idem", _tempDir);
        string path2 = await _service.ScaffoldProjectAsync("Idem", _tempDir);
        Assert.That(path1, Is.EqualTo(path2));
    }

    [Test]
    public async Task ScaffoldProject_SanitizesName()
    {
        string path = await _service.ScaffoldProjectAsync("My Game!!! (alpha)", _tempDir);
        Assert.That(Path.GetFileName(path), Does.Not.Contain(" "));
        Assert.That(Path.GetFileName(path), Does.Not.Contain("!"));
    }

    [Test]
    public async Task CreateFolder_CreatesWithMeta()
    {
        string folder = Path.Combine(_tempDir, "Assets", "Custom");
        await _service.CreateFolderAsync(folder);

        Assert.That(Directory.Exists(folder), Is.True);
        Assert.That(File.Exists(folder + ".meta"), Is.True);
    }

    [Test]
    public async Task SaveScript_CreatesFileAndMeta()
    {
        // First scaffold so the project structure exists
        string proj = await _service.ScaffoldProjectAsync("ScriptTest", _tempDir);
        await _service.SaveScriptAsync(proj, "Player.cs", "using UnityEngine;\npublic class Player : MonoBehaviour {}");

        string scriptPath = Path.Combine(proj, "Assets", "Scripts", "Player.cs");
        Assert.That(File.Exists(scriptPath), Is.True);
        Assert.That(File.Exists(scriptPath + ".meta"), Is.True);

        string meta = await File.ReadAllTextAsync(scriptPath + ".meta");
        Assert.That(meta, Does.Contain("MonoImporter:"));
    }

    [Test]
    public async Task SaveTextAsset_CreatesFileAndMeta()
    {
        string proj = await _service.ScaffoldProjectAsync("TextTest", _tempDir);
        await _service.SaveTextAssetAsync(proj, "info.txt", "Hello World");

        string filePath = Path.Combine(proj, "Assets", "Text", "info.txt");
        Assert.That(File.Exists(filePath), Is.True);
        Assert.That(File.Exists(filePath + ".meta"), Is.True);
    }

    [Test]
    public async Task SaveTexture_CreatesFileAndMeta()
    {
        string proj = await _service.ScaffoldProjectAsync("TexTest", _tempDir);
        string base64 = Convert.ToBase64String(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        await _service.SaveTextureAsync(proj, "sprite.png", base64);

        string filePath = Path.Combine(proj, "Assets", "Textures", "sprite.png");
        Assert.That(File.Exists(filePath), Is.True);

        string meta = await File.ReadAllTextAsync(filePath + ".meta");
        Assert.That(meta, Does.Contain("TextureImporter:"));
    }

    [Test]
    public async Task SaveAudio_CreatesFileAndMeta()
    {
        string proj = await _service.ScaffoldProjectAsync("AudioTest", _tempDir);
        string base64 = Convert.ToBase64String(new byte[] { 0xFF, 0xFB, 0x90 });
        await _service.SaveAudioAsync(proj, "sfx.mp3", base64);

        string filePath = Path.Combine(proj, "Assets", "Audio", "sfx.mp3");
        Assert.That(File.Exists(filePath), Is.True);

        string meta = await File.ReadAllTextAsync(filePath + ".meta");
        Assert.That(meta, Does.Contain("AudioImporter:"));
    }

    [Test]
    public async Task GetProjectInfo_ReturnsCorrectJson()
    {
        string proj = await _service.ScaffoldProjectAsync("InfoTest", _tempDir, "2022.3.0f1");
        string info = await _service.GetProjectInfoAsync(proj);

        Assert.That(info, Does.Contain("InfoTest"));
        Assert.That(info, Does.Contain("2022.3.0f1"));
        Assert.That(info, Does.Contain("\"hasAssets\":true"));
    }

    [Test]
    public async Task ValidateCSharp_ValidCode_ReturnsTrue()
    {
        string code = "using UnityEngine;\npublic class Player : MonoBehaviour {\n    void Start() { }\n}";
        string result = await _service.ValidateCSharpAsync(code);
        Assert.That(result, Does.Contain("\"isValid\":true"));
    }

    [Test]
    public async Task ValidateCSharp_MissingBrace_ReturnsFalse()
    {
        string code = "using UnityEngine;\npublic class Player : MonoBehaviour {\n    void Start() { }";
        string result = await _service.ValidateCSharpAsync(code);
        Assert.That(result, Does.Contain("\"isValid\":false"));
        Assert.That(result, Does.Contain("closing brace"));
    }

    [Test]
    public async Task ValidateCSharp_NoClassKeyword_ReturnsFalse()
    {
        string code = "using UnityEngine;\nvoid Main() { }";
        string result = await _service.ValidateCSharpAsync(code);
        Assert.That(result, Does.Contain("\"isValid\":false"));
    }

    [Test]
    public async Task AddPackages_CreatesManifest()
    {
        string proj = await _service.ScaffoldProjectAsync("PkgTest", _tempDir);
        await _service.AddPackagesAsync(proj, "{\"com.unity.textmeshpro\":\"3.0.6\"}");

        string manifest = await File.ReadAllTextAsync(Path.Combine(proj, "Packages", "manifest.json"));
        Assert.That(manifest, Does.Contain("com.unity.textmeshpro"));
        Assert.That(manifest, Does.Contain("3.0.6"));
    }

    [Test]
    public async Task AddPackages_MergesWithExisting()
    {
        string proj = await _service.ScaffoldProjectAsync("MergeTest", _tempDir);
        await _service.AddPackagesAsync(proj, "{\"com.unity.urp\":\"14.0.0\"}");
        await _service.AddPackagesAsync(proj, "{\"com.unity.textmeshpro\":\"3.0.6\"}");

        string manifest = await File.ReadAllTextAsync(Path.Combine(proj, "Packages", "manifest.json"));
        Assert.That(manifest, Does.Contain("com.unity.urp"));
        Assert.That(manifest, Does.Contain("com.unity.textmeshpro"));
    }
}

