using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityMcp.Core.Interfaces;
using UnityMcp.Infrastructure.Services;
using System.IO.Abstractions.TestingHelpers;

namespace UnityMcp.Tests.Application.Tools;

[TestFixture]
public class FileUnityServiceNewToolsTests
{
    private MockFileSystem _mockFs = null!;
    private FileUnityService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockFs = new MockFileSystem();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<FileUnityService>>();
        var processRunner = Substitute.For<IProcessRunner>();
        _service = new FileUnityService(logger, processRunner, _mockFs);
    }

    [Test]
    public async Task ScaffoldProject_CreatesAllFolders()
    {
        string projectPath = await _service.ScaffoldProjectAsync("TestProject", @"C:\output");

        Assert.That(_mockFs.Directory.Exists(_mockFs.Path.Combine(projectPath, "Assets")), Is.True);
        Assert.That(_mockFs.Directory.Exists(_mockFs.Path.Combine(projectPath, "Assets", "Scripts")), Is.True);
        Assert.That(_mockFs.Directory.Exists(_mockFs.Path.Combine(projectPath, "Assets", "Textures")), Is.True);
        Assert.That(_mockFs.Directory.Exists(_mockFs.Path.Combine(projectPath, "Assets", "Audio")), Is.True);
        Assert.That(_mockFs.Directory.Exists(_mockFs.Path.Combine(projectPath, "Assets", "Text")), Is.True);
        Assert.That(_mockFs.Directory.Exists(_mockFs.Path.Combine(projectPath, "ProjectSettings")), Is.True);
        Assert.That(_mockFs.Directory.Exists(_mockFs.Path.Combine(projectPath, "Packages")), Is.True);
    }

    [Test]
    public async Task ScaffoldProject_CreatesMetaSidecars()
    {
        string projectPath = await _service.ScaffoldProjectAsync("MetaTest", @"C:\output");

        Assert.That(_mockFs.File.Exists(_mockFs.Path.Combine(projectPath, "Assets") + ".meta"), Is.True);
        Assert.That(_mockFs.File.Exists(_mockFs.Path.Combine(projectPath, "Assets", "Scripts") + ".meta"), Is.True);
    }

    [Test]
    public async Task ScaffoldProject_CreatesProjectVersion()
    {
        string projectPath = await _service.ScaffoldProjectAsync("VersionTest", @"C:\output", "2023.2.0f1");

        string versionContent = _mockFs.File.ReadAllText(
            _mockFs.Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt"));
        Assert.That(versionContent, Does.Contain("2023.2.0f1"));
    }

    [Test]
    public async Task ScaffoldProject_IsIdempotent()
    {
        string path1 = await _service.ScaffoldProjectAsync("Idem", @"C:\output");
        string path2 = await _service.ScaffoldProjectAsync("Idem", @"C:\output");
        Assert.That(path1, Is.EqualTo(path2));
    }

    [Test]
    public async Task ScaffoldProject_SanitizesName()
    {
        string path = await _service.ScaffoldProjectAsync("My Game!!! (alpha)", @"C:\output");
        Assert.That(_mockFs.Path.GetFileName(path), Does.Not.Contain(" "));
        Assert.That(_mockFs.Path.GetFileName(path), Does.Not.Contain("!"));
    }

    [Test]
    public async Task CreateFolder_CreatesWithMeta()
    {
        string projectPath = await _service.ScaffoldProjectAsync("FolderTest", @"C:\output");
        await _service.CreateFolderAsync(projectPath, "Assets/Custom");

        string folder = _mockFs.Path.Combine(projectPath, "Assets", "Custom");
        Assert.That(_mockFs.Directory.Exists(folder), Is.True);
        Assert.That(_mockFs.File.Exists(folder + ".meta"), Is.True);
    }

    [Test]
    public async Task SaveScript_CreatesFileAndMeta()
    {
        string proj = await _service.ScaffoldProjectAsync("ScriptTest", @"C:\output");
        await _service.SaveScriptAsync(proj, "Player.cs", "using UnityEngine;\npublic class Player : MonoBehaviour {}");

        string scriptPath = _mockFs.Path.Combine(proj, "Assets", "Scripts", "Player.cs");
        Assert.That(_mockFs.File.Exists(scriptPath), Is.True);
        Assert.That(_mockFs.File.Exists(scriptPath + ".meta"), Is.True);

        string meta = _mockFs.File.ReadAllText(scriptPath + ".meta");
        Assert.That(meta, Does.Contain("MonoImporter:"));
    }

    [Test]
    public async Task SaveScript_WhenFoldersMissing_CreatesAssetsAndScripts()
    {
        // Existing project root without Unity subfolders
        string projRoot = _mockFs.Path.GetFullPath(@"C:\ManualScriptProject");
        _mockFs.Directory.CreateDirectory(projRoot);

        await _service.SaveScriptAsync(projRoot, "Player.cs", "using UnityEngine;\npublic class Player : MonoBehaviour {}");

        string scriptsDir = _mockFs.Path.Combine(projRoot, "Assets", "Scripts");
        string scriptPath = _mockFs.Path.Combine(scriptsDir, "Player.cs");

        Assert.That(_mockFs.Directory.Exists(scriptsDir), Is.True);
        Assert.That(_mockFs.File.Exists(scriptPath), Is.True);
        Assert.That(_mockFs.File.Exists(scriptPath + ".meta"), Is.True);
    }

    [Test]
    public async Task SaveTextAsset_CreatesFileAndMeta()
    {
        string proj = await _service.ScaffoldProjectAsync("TextTest", @"C:\output");
        await _service.SaveTextAssetAsync(proj, "info.txt", "Hello World");

        string filePath = _mockFs.Path.Combine(proj, "Assets", "Text", "info.txt");
        Assert.That(_mockFs.File.Exists(filePath), Is.True);
        Assert.That(_mockFs.File.Exists(filePath + ".meta"), Is.True);
    }

    [Test]
    public async Task SaveTextAsset_WhenFoldersMissing_CreatesAssetsAndText()
    {
        string projRoot = _mockFs.Path.GetFullPath(@"C:\ManualTextProject");
        _mockFs.Directory.CreateDirectory(projRoot);

        await _service.SaveTextAssetAsync(projRoot, "info.txt", "Hello World");

        string textDir = _mockFs.Path.Combine(projRoot, "Assets", "Text");
        string filePath = _mockFs.Path.Combine(textDir, "info.txt");

        Assert.That(_mockFs.Directory.Exists(textDir), Is.True);
        Assert.That(_mockFs.File.Exists(filePath), Is.True);
        Assert.That(_mockFs.File.Exists(filePath + ".meta"), Is.True);
    }

    [Test]
    public async Task SaveTexture_CreatesFileAndMeta()
    {
        string proj = await _service.ScaffoldProjectAsync("TexTest", @"C:\output");
        string base64 = Convert.ToBase64String(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        await _service.SaveTextureAsync(proj, "sprite.png", base64);

        string filePath = _mockFs.Path.Combine(proj, "Assets", "Textures", "sprite.png");
        Assert.That(_mockFs.File.Exists(filePath), Is.True);

        string meta = _mockFs.File.ReadAllText(filePath + ".meta");
        Assert.That(meta, Does.Contain("TextureImporter:"));
    }

    [Test]
    public async Task SaveTexture_WhenFoldersMissing_CreatesAssetsAndTextures()
    {
        string projRoot = _mockFs.Path.GetFullPath(@"C:\ManualTexProject");
        _mockFs.Directory.CreateDirectory(projRoot);

        string base64 = Convert.ToBase64String(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        await _service.SaveTextureAsync(projRoot, "sprite.png", base64);

        string texDir = _mockFs.Path.Combine(projRoot, "Assets", "Textures");
        string filePath = _mockFs.Path.Combine(texDir, "sprite.png");

        Assert.That(_mockFs.Directory.Exists(texDir), Is.True);
        Assert.That(_mockFs.File.Exists(filePath), Is.True);
        Assert.That(_mockFs.File.Exists(filePath + ".meta"), Is.True);
    }

    [Test]
    public async Task SaveAudio_CreatesFileAndMeta()
    {
        string proj = await _service.ScaffoldProjectAsync("AudioTest", @"C:\output");
        string base64 = Convert.ToBase64String(new byte[] { 0xFF, 0xFB, 0x90 });
        await _service.SaveAudioAsync(proj, "sfx.mp3", base64);

        string filePath = _mockFs.Path.Combine(proj, "Assets", "Audio", "sfx.mp3");
        Assert.That(_mockFs.File.Exists(filePath), Is.True);

        string meta = _mockFs.File.ReadAllText(filePath + ".meta");
        Assert.That(meta, Does.Contain("AudioImporter:"));
    }

    [Test]
    public async Task CreateGameObjectAsync_WhenSceneFolderMissing_CreatesDirectory()
    {
        // Existing root without Assets/Scenes
        string projRoot = _mockFs.Path.GetFullPath(@"C:\ManualSceneProject");
        _mockFs.Directory.CreateDirectory(projRoot);

        await _service.CreateGameObjectAsync(projRoot, "Assets/Scenes/Main.unity", "Cube");

        string scenesDir = _mockFs.Path.Combine(projRoot, "Assets", "Scenes");
        string scenePath = _mockFs.Path.Combine(scenesDir, "Main.unity");

        Assert.That(_mockFs.Directory.Exists(scenesDir), Is.True);
        Assert.That(_mockFs.File.Exists(scenePath), Is.True);
    }

    [Test]
    public async Task SaveAudio_WhenFoldersMissing_CreatesAssetsAndAudio()
    {
        string projRoot = _mockFs.Path.GetFullPath(@"C:\ManualAudioProject");
        _mockFs.Directory.CreateDirectory(projRoot);

        string base64 = Convert.ToBase64String(new byte[] { 0xFF, 0xFB, 0x90 });
        await _service.SaveAudioAsync(projRoot, "sfx.mp3", base64);

        string audioDir = _mockFs.Path.Combine(projRoot, "Assets", "Audio");
        string filePath = _mockFs.Path.Combine(audioDir, "sfx.mp3");

        Assert.That(_mockFs.Directory.Exists(audioDir), Is.True);
        Assert.That(_mockFs.File.Exists(filePath), Is.True);
        Assert.That(_mockFs.File.Exists(filePath + ".meta"), Is.True);
    }

    [Test]
    public async Task GetProjectInfo_ReturnsCorrectJson()
    {
        string proj = await _service.ScaffoldProjectAsync("InfoTest", @"C:\output", "2022.3.0f1");
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
        string proj = await _service.ScaffoldProjectAsync("PkgTest", @"C:\output");
        await _service.AddPackagesAsync(proj, "{\"com.unity.textmeshpro\":\"3.0.6\"}");

        string manifest = _mockFs.File.ReadAllText(_mockFs.Path.Combine(proj, "Packages", "manifest.json"));
        Assert.That(manifest, Does.Contain("com.unity.textmeshpro"));
        Assert.That(manifest, Does.Contain("3.0.6"));
    }

    [Test]
    public async Task AddPackages_WhenPackagesFolderMissing_CreatesPackagesAndManifest()
    {
        string projRoot = _mockFs.Path.GetFullPath(@"C:\ManualPkgProject");
        _mockFs.Directory.CreateDirectory(projRoot);

        await _service.AddPackagesAsync(projRoot, "{\"com.unity.textmeshpro\":\"3.0.6\"}");

        string packagesDir = _mockFs.Path.Combine(projRoot, "Packages");
        string manifestPath = _mockFs.Path.Combine(packagesDir, "manifest.json");

        Assert.That(_mockFs.Directory.Exists(packagesDir), Is.True);
        Assert.That(_mockFs.File.Exists(manifestPath), Is.True);
    }

    [Test]
    public async Task AddPackages_MergesWithExisting()
    {
        string proj = await _service.ScaffoldProjectAsync("MergeTest", @"C:\output");
        await _service.AddPackagesAsync(proj, "{\"com.unity.urp\":\"14.0.0\"}");
        await _service.AddPackagesAsync(proj, "{\"com.unity.textmeshpro\":\"3.0.6\"}");

        string manifest = _mockFs.File.ReadAllText(_mockFs.Path.Combine(proj, "Packages", "manifest.json"));
        Assert.That(manifest, Does.Contain("com.unity.urp"));
        Assert.That(manifest, Does.Contain("com.unity.textmeshpro"));
    }

    [Test]
    public async Task InstallPackagesAsync_AddsToManifest()
    {
        string proj = await _service.ScaffoldProjectAsync("InstallPkg", @"C:\output");
        string json = await _service.InstallPackagesAsync(proj, new List<string> { "com.unity.render-pipelines.universal", "com.unity.textmeshpro" });

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("installed"));
        string manifest = _mockFs.File.ReadAllText(_mockFs.Path.Combine(proj, "Packages", "manifest.json"));
        Assert.That(manifest, Does.Contain("com.unity.render-pipelines.universal"));
        Assert.That(manifest, Does.Contain("com.unity.textmeshpro"));
    }

    [Test]
    public async Task CreateDefaultSceneAsync_CreatesSceneAndPrefab()
    {
        string proj = await _service.ScaffoldProjectAsync("DefaultScene", @"C:\output");
        string json = await _service.CreateDefaultSceneAsync(proj, "MainScene");

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("scene_path"));
        Assert.That(json, Does.Contain("prefab_path"));
        Assert.That(json, Does.Contain("Assets/Scenes/MainScene.unity"));
        Assert.That(json, Does.Contain("Assets/Prefabs/Ground.prefab"));

        string scenePath = _mockFs.Path.Combine(proj, "Assets", "Scenes", "MainScene.unity");
        string prefabPath = _mockFs.Path.Combine(proj, "Assets", "Prefabs", "Ground.prefab");
        Assert.That(_mockFs.File.Exists(scenePath), Is.True);
        Assert.That(_mockFs.File.Exists(prefabPath), Is.True);
        Assert.That(_mockFs.File.Exists(scenePath + ".meta"), Is.True);
        Assert.That(_mockFs.File.Exists(prefabPath + ".meta"), Is.True);

        string sceneContent = _mockFs.File.ReadAllText(scenePath);
        Assert.That(sceneContent, Does.Contain("Main Camera"));
        Assert.That(sceneContent, Does.Contain("Directional Light"));
        Assert.That(sceneContent, Does.Contain("Ground"));
    }

    [Test]
    public async Task ValidateImportAsync_ReturnsStubJson()
    {
        string proj = await _service.ScaffoldProjectAsync("ValidateStub", @"C:\output");
        string json = await _service.ValidateImportAsync(proj);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("\"error_count\":0"));
        Assert.That(json, Does.Contain("\"warning_count\":0"));
    }

    [Test]
    public async Task CreateUiCanvasAsync_NewScene_CreatesFileWithCanvasAndEventSystem()
    {
        string proj = await _service.ScaffoldProjectAsync("UiCanvas", @"C:\output");
        string json = await _service.CreateUiCanvasAsync(proj, "Assets/Scenes/MainMenu.unity");

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("\"path\""));
        Assert.That(json, Does.Contain("MainMenu.unity"));

        string scenePath = _mockFs.Path.Combine(proj, "Assets", "Scenes", "MainMenu.unity");
        Assert.That(_mockFs.File.Exists(scenePath), Is.True);
        Assert.That(_mockFs.File.Exists(scenePath + ".meta"), Is.True);

        string sceneContent = _mockFs.File.ReadAllText(scenePath);
        Assert.That(sceneContent, Does.Contain("Canvas"));
        Assert.That(sceneContent, Does.Contain("EventSystem"));
        Assert.That(sceneContent, Does.Contain("Main Camera"));
    }

    [Test]
    public async Task CreateUiLayoutAsync_AfterCanvas_AppendsHierarchyAndReturnsSuccess()
    {
        string proj = await _service.ScaffoldProjectAsync("UiLayout", @"C:\output");
        await _service.CreateUiCanvasAsync(proj, "Assets/Scenes/MainMenu.unity");

        const string layoutJson = "{\"name\":\"MainMenu\",\"panels\":[{\"name\":\"RootPanel\",\"controls\":[{\"name\":\"StartButton\",\"type\":0,\"text\":\"Start Game\"}]}]}";
        string json = await _service.CreateUiLayoutAsync(proj, "Assets/Scenes/MainMenu.unity", layoutJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("UI layout applied successfully"));

        string scenePath = _mockFs.Path.Combine(proj, "Assets", "Scenes", "MainMenu.unity");
        string sceneContent = _mockFs.File.ReadAllText(scenePath);
        Assert.That(sceneContent, Does.Contain("RootPanel"));
        Assert.That(sceneContent, Does.Contain("StartButton"));
    }

    [Test]
    public async Task CreateUiLayoutAsync_EmptyLayout_ReturnsSuccessWithWarning()
    {
        string proj = await _service.ScaffoldProjectAsync("UiLayoutEmpty", @"C:\output");
        await _service.CreateUiCanvasAsync(proj, "Assets/Scenes/Empty.unity");

        string json = await _service.CreateUiLayoutAsync(proj, "Assets/Scenes/Empty.unity", "{\"name\":\"Empty\",\"panels\":[]}");

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("UiLayout.Empty"));
    }

    [Test]
    public void CreateUiLayoutAsync_TargetNotFound_Throws()
    {
        string proj = _mockFs.Path.GetFullPath(@"C:\proj");
        _mockFs.Directory.CreateDirectory(proj);
        _mockFs.Directory.CreateDirectory(_mockFs.Path.Combine(proj, "Assets"));
        _mockFs.Directory.CreateDirectory(_mockFs.Path.Combine(proj, "Assets", "Scenes"));

        Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await _service.CreateUiLayoutAsync(proj, "Assets/Scenes/NoSuch.unity", "{\"name\":\"X\",\"panels\":[]}"));
    }

    [Test]
    public async Task ConfigureNavmeshAsync_ValidJson_WritesAssetAndReturnsSuccess()
    {
        string proj = await _service.ScaffoldProjectAsync("NavMesh", @"C:\output");
        const string configJson = "{\"agentRadius\":0.5,\"agentHeight\":2,\"agentSlope\":45,\"agentClimb\":0.4,\"cellSize\":0.1,\"cellHeight\":0.2,\"manualVoxelSize\":false}";
        string json = await _service.ConfigureNavmeshAsync(proj, configJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("path"));
        Assert.That(json, Does.Contain("NavMeshConfig"));

        string configPath = _mockFs.Path.Combine(proj, "Assets", "Settings", "NavMeshConfig.json");
        Assert.That(_mockFs.File.Exists(configPath), Is.True);
        Assert.That(_mockFs.File.Exists(configPath + ".meta"), Is.True);
        string content = _mockFs.File.ReadAllText(configPath);
        Assert.That(content, Does.Contain("agentRadius"));
        Assert.That(content, Does.Contain("0.5"));
    }

    [Test]
    public async Task ConfigureNavmeshAsync_InvalidJson_ReturnsValidationError()
    {
        string proj = await _service.ScaffoldProjectAsync("NavMeshInvalid", @"C:\output");
        string json = await _service.ConfigureNavmeshAsync(proj, "{ invalid json }");

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("NavMeshConfig.InvalidJson"));
    }

    [Test]
    public async Task CreateWaypointGraphAsync_ValidGraph_WritesAssetAndReturnsSuccess()
    {
        string proj = await _service.ScaffoldProjectAsync("Waypoints", @"C:\output");
        const string graphJson = "{\"name\":\"PatrolRoute\",\"nodes\":[{\"id\":\"A\",\"position\":{\"x\":0,\"y\":0,\"z\":0}},{\"id\":\"B\",\"position\":{\"x\":5,\"y\":0,\"z\":0}}],\"edges\":[{\"from\":\"A\",\"to\":\"B\",\"bidirectional\":true}]}";
        string json = await _service.CreateWaypointGraphAsync(proj, "Assets/Data/PatrolRoute.waypoints.json", graphJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("path"));
        Assert.That(json, Does.Contain("PatrolRoute"));

        string graphPath = _mockFs.Path.Combine(proj, "Assets", "Data", "PatrolRoute.waypoints.json");
        Assert.That(_mockFs.File.Exists(graphPath), Is.True);
        Assert.That(_mockFs.File.Exists(graphPath + ".meta"), Is.True);
        string content = _mockFs.File.ReadAllText(graphPath);
        Assert.That(content, Does.Contain("PatrolRoute"));
        Assert.That(content, Does.Contain("A"));
        Assert.That(content, Does.Contain("B"));
    }

    [Test]
    public async Task CreateWaypointGraphAsync_InvalidGraph_ReturnsValidationError()
    {
        string proj = await _service.ScaffoldProjectAsync("WaypointsInvalid", @"C:\output");
        const string graphJson = "{\"name\":\"X\",\"nodes\":[{\"id\":\"A\",\"position\":{\"x\":0,\"y\":0,\"z\":0}}],\"edges\":[{\"from\":\"A\",\"to\":\"C\",\"bidirectional\":false}]}";
        string json = await _service.CreateWaypointGraphAsync(proj, "Assets/Data/X.waypoints.json", graphJson);

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("WaypointGraph.InvalidEdge"));
        Assert.That(json, Does.Contain("C"));
    }

    [Test]
    public async Task CreateInputActionsAsync_ValidJson_WritesInputAsset()
    {
        string proj = await _service.ScaffoldProjectAsync("InputActions", @"C:\output");
        const string actionsJson = "{\"name\":\"PlayerControls\",\"maps\":[{\"name\":\"Gameplay\",\"actions\":[{\"name\":\"Move\",\"type\":\"Value\",\"expectedControlType\":\"Vector2\"},{\"name\":\"Jump\",\"type\":\"Button\",\"expectedControlType\":\"Button\"}],\"bindings\":[{\"action\":\"Move\",\"path\":\"<Keyboard>/wasd\"},{\"action\":\"Jump\",\"path\":\"<Keyboard>/space\"}]}]}";
        string json = await _service.CreateInputActionsAsync(proj, "Assets/Input/PlayerControls.inputactions", actionsJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("path"));
        string assetPath = _mockFs.Path.Combine(proj, "Assets", "Input", "PlayerControls.inputactions");
        Assert.That(_mockFs.File.Exists(assetPath), Is.True);
        Assert.That(_mockFs.File.Exists(assetPath + ".meta"), Is.True);
        string content = _mockFs.File.ReadAllText(assetPath);
        Assert.That(content, Does.Contain("PlayerControls"));
        Assert.That(content, Does.Contain("Gameplay"));
        Assert.That(content, Does.Contain("Move"));
    }

    [Test]
    public async Task CreateInputActionsAsync_InvalidJson_ReturnsValidationError()
    {
        string proj = await _service.ScaffoldProjectAsync("InputInvalid", @"C:\output");
        string json = await _service.CreateInputActionsAsync(proj, "Assets/Input/Invalid.inputactions", "{ broken }");

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("InputActions.InvalidJson"));
    }

    [Test]
    public async Task CreateBasicAnimatorAsync_ValidDefinition_WritesAsset()
    {
        string proj = await _service.ScaffoldProjectAsync("Animator", @"C:\output");
        const string animatorJson = "{\"name\":\"CharacterAnimator\",\"defaultState\":\"Idle\",\"states\":[{\"name\":\"Idle\",\"clip\":\"Assets/Animations/Idle.anim\"},{\"name\":\"Run\",\"clip\":\"Assets/Animations/Run.anim\"}],\"transitions\":[{\"from\":\"Idle\",\"to\":\"Run\",\"condition\":\"Speed > 0.1\"}]}";
        string json = await _service.CreateBasicAnimatorAsync(proj, "Assets/Animations/Character.animator.json", animatorJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("path"));
        string assetPath = _mockFs.Path.Combine(proj, "Assets", "Animations", "Character.animator.json");
        Assert.That(_mockFs.File.Exists(assetPath), Is.True);
        Assert.That(_mockFs.File.Exists(assetPath + ".meta"), Is.True);
        string content = _mockFs.File.ReadAllText(assetPath);
        Assert.That(content, Does.Contain("CharacterAnimator"));
        Assert.That(content, Does.Contain("Idle"));
        Assert.That(content, Does.Contain("Run"));
    }

    [Test]
    public async Task CreateBasicAnimatorAsync_InvalidJson_ReturnsValidationError()
    {
        string proj = await _service.ScaffoldProjectAsync("AnimatorInvalid", @"C:\output");
        string json = await _service.CreateBasicAnimatorAsync(proj, "Assets/Animations/X.animator.json", "{ invalid }");

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("BasicAnimator.InvalidJson"));
    }

    [Test]
    public async Task CreateBasicAnimatorAsync_MissingClip_ReportsWarning()
    {
        string proj = await _service.ScaffoldProjectAsync("AnimatorMissingClip", @"C:\output");
        const string animatorJson = "{\"name\":\"X\",\"defaultState\":\"Idle\",\"states\":[{\"name\":\"Idle\",\"clip\":\"Assets/Animations/NonExistent.anim\"}],\"transitions\":[]}";
        string json = await _service.CreateBasicAnimatorAsync(proj, "Assets/Animations/X.animator.json", animatorJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("warnings"));
        Assert.That(json, Does.Contain("BasicAnimator.MissingClip"));
        Assert.That(json, Does.Contain("NonExistent"));
    }

    [Test]
    public async Task CreateAdvancedAnimatorAsync_ValidDefinition_WritesAsset()
    {
        string proj = await _service.ScaffoldProjectAsync("AdvancedAnimator", @"C:\output");
        const string advancedJson = "{\"layers\":[{\"name\":\"Base Layer\",\"defaultState\":\"Locomotion\",\"states\":[{\"name\":\"Locomotion\",\"clip\":\"Assets/Animations/Locomotion.anim\"}],\"subStateMachines\":[]}]}";
        string json = await _service.CreateAdvancedAnimatorAsync(proj, "Assets/Animations/CharacterAdvanced.animator.json", advancedJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("path"));
        string assetPath = _mockFs.Path.Combine(proj, "Assets", "Animations", "CharacterAdvanced.animator.json");
        Assert.That(_mockFs.File.Exists(assetPath), Is.True);
        Assert.That(_mockFs.File.Exists(assetPath + ".meta"), Is.True);
        string content = _mockFs.File.ReadAllText(assetPath);
        Assert.That(content, Does.Contain("Base Layer"));
        Assert.That(content, Does.Contain("Locomotion"));
    }

    [Test]
    public async Task CreateAdvancedAnimatorAsync_InvalidJson_ReturnsValidationError()
    {
        string proj = await _service.ScaffoldProjectAsync("AdvancedAnimatorInvalid", @"C:\output");
        string json = await _service.CreateAdvancedAnimatorAsync(proj, "Assets/Animations/Bad.animator.json", "{ invalid }");

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("AdvancedAnimator.InvalidJson"));
    }

    [Test]
    public async Task CreateAdvancedAnimatorAsync_InvalidStructure_ReturnsValidationErrors()
    {
        string proj = await _service.ScaffoldProjectAsync("AdvancedAnimatorBadStructure", @"C:\output");
        const string advancedJson = "{\"layers\":[{\"name\":\"Base Layer\",\"defaultState\":\"MissingState\",\"states\":[],\"subStateMachines\":[]}]}";
        string json = await _service.CreateAdvancedAnimatorAsync(proj, "Assets/Animations/BadStructure.animator.json", advancedJson);

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("AdvancedAnimator.InvalidLayer"));
        Assert.That(json, Does.Contain("MissingState"));
    }

    [Test]
    public async Task CreateTimelineAsync_ValidDefinition_WritesAssetAndReturnsSuccess()
    {
        string proj = await _service.ScaffoldProjectAsync("Timeline", @"C:\output");
        const string timelineJson = "{\"name\":\"IntroCutscene\",\"tracks\":[{\"type\":\"Animation\",\"binding\":\"Player\",\"clips\":[{\"name\":\"IntroPose\",\"clip\":\"Assets/Animations/IntroPose.anim\",\"start\":0.0,\"duration\":2.0}]}]}";
        string json = await _service.CreateTimelineAsync(proj, "Assets/Timelines/IntroCutscene.timeline.json", timelineJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("path"));
        string assetPath = _mockFs.Path.Combine(proj, "Assets", "Timelines", "IntroCutscene.timeline.json");
        Assert.That(_mockFs.File.Exists(assetPath), Is.True);
        Assert.That(_mockFs.File.Exists(assetPath + ".meta"), Is.True);
        string content = _mockFs.File.ReadAllText(assetPath);
        Assert.That(content, Does.Contain("IntroCutscene"));
        Assert.That(content, Does.Contain("IntroPose"));
    }

    [Test]
    public async Task CreateTimelineAsync_InvalidJson_ReturnsValidationError()
    {
        string proj = await _service.ScaffoldProjectAsync("TimelineInvalid", @"C:\output");
        string json = await _service.CreateTimelineAsync(proj, "Assets/Timelines/Bad.timeline.json", "{ invalid }");

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("Timeline.InvalidJson"));
    }

    [Test]
    public async Task CreateTimelineAsync_MissingAssets_EmitsWarnings()
    {
        string proj = await _service.ScaffoldProjectAsync("TimelineMissingAssets", @"C:\output");
        const string timelineJson = "{\"name\":\"IntroCutscene\",\"tracks\":[{\"type\":\"Animation\",\"binding\":\"Player\",\"clips\":[{\"name\":\"IntroPose\",\"clip\":\"Assets/Animations/Missing.anim\",\"start\":0.0,\"duration\":2.0}]},{\"type\":\"Audio\",\"binding\":\"MusicSource\",\"clips\":[{\"name\":\"IntroMusic\",\"audio\":\"Assets/Audio/Missing.ogg\",\"start\":0.0,\"duration\":30.0}]}]}";
        string json = await _service.CreateTimelineAsync(proj, "Assets/Timelines/IntroCutscene.timeline.json", timelineJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("warnings"));
        Assert.That(json, Does.Contain("Timeline.MissingClip"));
        Assert.That(json, Does.Contain("Timeline.MissingAudio"));
    }

    [Test]
    public async Task CreatePhysicsSetupAsync_ValidDefinition_WritesAssetAndReturnsSuccess()
    {
        string proj = await _service.ScaffoldProjectAsync("PhysicsSetup", @"C:\output");
        const string physicsJson = "{\"name\":\"HumanoidRagdoll\",\"bones\":[{\"name\":\"Hips\",\"colliderType\":\"Capsule\",\"mass\":10.0},{\"name\":\"Spine\",\"colliderType\":\"Box\",\"mass\":8.0}],\"joints\":[{\"name\":\"SpineJoint\",\"type\":\"Configurable\",\"bone\":\"Spine\",\"connectedBodyName\":\"Hips\"}]}";
        string json = await _service.CreatePhysicsSetupAsync(proj, "Assets/Physics/HumanoidRagdoll.physics.json", physicsJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("path"));

        string assetPath = _mockFs.Path.Combine(proj, "Assets", "Physics", "HumanoidRagdoll.physics.json");
        Assert.That(_mockFs.File.Exists(assetPath), Is.True);
        Assert.That(_mockFs.File.Exists(assetPath + ".meta"), Is.True);
        string content = _mockFs.File.ReadAllText(assetPath);
        Assert.That(content, Does.Contain("HumanoidRagdoll"));
        Assert.That(content, Does.Contain("Hips"));
        Assert.That(content, Does.Contain("Spine"));
    }

    [Test]
    public async Task CreatePhysicsSetupAsync_InvalidJson_ReturnsValidationError()
    {
        string proj = await _service.ScaffoldProjectAsync("PhysicsInvalidJson", @"C:\output");
        string json = await _service.CreatePhysicsSetupAsync(proj, "Assets/Physics/Invalid.physics.json", "{ invalid }");

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("PhysicsSetup.InvalidJson"));
    }

    [Test]
    public async Task CreatePhysicsSetupAsync_InvalidReferences_ReturnsValidationErrors()
    {
        string proj = await _service.ScaffoldProjectAsync("PhysicsInvalidRefs", @"C:\output");
        const string physicsJson = "{\"name\":\"HumanoidRagdoll\",\"bones\":[{\"name\":\"Hips\",\"colliderType\":\"Capsule\",\"mass\":10.0}],\"joints\":[{\"name\":\"BadJoint\",\"type\":\"Hinge\",\"bone\":\"Spine\",\"connectedBodyName\":\"Head\"}]}";
        string json = await _service.CreatePhysicsSetupAsync(proj, "Assets/Physics/HumanoidRagdoll.physics.json", physicsJson);

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("PhysicsSetup.InvalidReference"));
        Assert.That(json, Does.Contain("Spine"));
        Assert.That(json, Does.Contain("Head"));
    }

    [Test]
    public async Task CreateVfxAssetAsync_ValidDefinition_WritesAssetAndReturnsSuccess()
    {
        string proj = await _service.ScaffoldProjectAsync("Vfx", @"C:\output");
        const string vfxJson = "{\"name\":\"ExplosionSmall\",\"duration\":1.0,\"looping\":false,\"startLifetime\":0.5,\"startSpeed\":5.0,\"startSize\":1.0,\"startColor\":{\"r\":1,\"g\":0.6,\"b\":0.1,\"a\":1},\"emission\":{\"rateOverTime\":0,\"bursts\":[{\"time\":0.0,\"count\":50}]},\"shape\":{\"type\":\"Sphere\",\"radius\":0.5}}";
        string json = await _service.CreateVfxAssetAsync(proj, "Assets/VFX/ExplosionSmall.vfx.json", vfxJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("ExplosionSmall"));

        string assetPath = _mockFs.Path.Combine(proj, "Assets", "VFX", "ExplosionSmall.vfx.json");
        Assert.That(_mockFs.File.Exists(assetPath), Is.True);
        Assert.That(_mockFs.File.Exists(assetPath + ".meta"), Is.True);
        string content = _mockFs.File.ReadAllText(assetPath);
        Assert.That(content, Does.Contain("ExplosionSmall"));
        Assert.That(content, Does.Contain("\"duration\": 1"));
    }

    [Test]
    public async Task CreateVfxAssetAsync_InvalidJson_ReturnsValidationError()
    {
        string proj = await _service.ScaffoldProjectAsync("VfxInvalidJson", @"C:\output");
        string json = await _service.CreateVfxAssetAsync(proj, "Assets/VFX/Bad.vfx.json", "{ invalid json }");

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("Vfx.InvalidJson"));
    }

    [Test]
    public async Task CreateVfxAssetAsync_InvalidParameters_ReturnsValidationErrors()
    {
        string proj = await _service.ScaffoldProjectAsync("VfxInvalidParams", @"C:\output");
        const string vfxJson = "{\"name\":\"ExplosionBad\",\"duration\":-1.0,\"looping\":false,\"startLifetime\":-0.5,\"startSpeed\":-5.0,\"startSize\":-1.0,\"startColor\":{\"r\":1,\"g\":0.6,\"b\":0.1,\"a\":1},\"emission\":{\"rateOverTime\":-1,\"bursts\":[{\"time\":-0.1,\"count\":-10}]},\"shape\":{\"type\":\"Sphere\",\"radius\":0.5}}";
        string json = await _service.CreateVfxAssetAsync(proj, "Assets/VFX/ExplosionBad.vfx.json", vfxJson);

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("Vfx.InvalidParameters"));
        Assert.That(json, Does.Contain("validation failed"));
    }

    [Test]
    public async Task AllOperations_UseMockFileSystem_NoPhysicalDiskWritten()
    {
        const string sentinelPath = "UnityMcp_TestIsolation_Sentinel_DoNotCreate";
        string outputRoot = _mockFs.Path.Combine(_mockFs.Path.GetTempPath(), sentinelPath);
        string proj = await _service.ScaffoldProjectAsync("Proj", outputRoot);
        await _service.CreateDefaultSceneAsync(proj, "MainScene");

        bool existsOnRealDisk = System.IO.Directory.Exists(proj);
        Assert.That(existsOnRealDisk, Is.False,
            "Test isolation broken: project path exists on real filesystem. Ensure FileUnityService is constructed with MockFileSystem only.");
    }
}
