using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using UnityMcp.Core.Contracts;
using UnityMcp.Core.Interfaces;
using UnityMcp.Infrastructure.Unity;

namespace UnityMcp.Infrastructure.Services;

/// <summary>
/// Pure .NET implementation of IUnityService.
/// Creates Unity project files (scenes, scripts, materials, prefabs) by writing
/// valid Unity YAML directly to disk. No Unity DLL dependencies.
/// </summary>
public class FileUnityService : IUnityService
{
    private readonly ILogger<FileUnityService> _logger;
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fs;
    private readonly MetaFileWriter _metaWriter;
    private readonly IUnityEditorExecutor? _editorExecutor;
    private string? _projectPath;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // Helper type for advanced animator root (layers collection).
    private sealed class AdvancedAnimatorWrapper
    {
        public IReadOnlyList<AnimatorLayerContract> Layers { get; init; } = new List<AnimatorLayerContract>();
    }

    public FileUnityService(
        ILogger<FileUnityService> logger,
        IProcessRunner processRunner,
        IFileSystem? fileSystem = null,
        IUnityEditorExecutor? editorExecutor = null)
    {
        _logger = logger;
        _processRunner = processRunner;
        _fs = fileSystem ?? new FileSystem();
        _metaWriter = new MetaFileWriter(_fs);
        _editorExecutor = editorExecutor;
    }

    public Task<bool> IsValidProjectAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        _projectPath = projectPath;
        bool isValid = _fs.Directory.Exists(_fs.Path.Combine(projectPath, "Assets"));
        return Task.FromResult(isValid);
    }

    public Task<string> GetServerInfoAsync(CancellationToken cancellationToken = default)
    {
        string? editorPath = _editorExecutor?.FindUnityExecutable() ?? TryFindUnityExecutable();
        bool liveConnected = false;
        if (!string.IsNullOrWhiteSpace(_projectPath) && _editorExecutor is not null)
            _editorExecutor.TryGetLiveBridgeStatus(_projectPath, out liveConnected, out _);

        var info = new UnityServerInfo
        {
            Version = typeof(FileUnityService).Assembly.GetName().Version?.ToString() ?? "unknown",
            Backend = editorPath is null ? "file-only" : liveConnected ? "file+live-editor" : "file+unity-editor",
            EditorAvailable = editorPath is not null || liveConnected,
            EditorPath = editorPath,
            LiveBridgeConnected = liveConnected,
        };

        return Task.FromResult(JsonSerializer.Serialize(info));
    }

    public Task<string> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        string? editorPath = _editorExecutor?.FindUnityExecutable() ?? TryFindUnityExecutable();
        bool liveConnected = false;
        if (!string.IsNullOrWhiteSpace(_projectPath) && _editorExecutor is not null)
            _editorExecutor.TryGetLiveBridgeStatus(_projectPath, out liveConnected, out _);
        bool editorBacked = editorPath is not null || liveConnected;

        var manifest = new UnityCapabilityManifest
        {
            Server = new UnityServerInfo
            {
                Version = typeof(FileUnityService).Assembly.GetName().Version?.ToString() ?? "unknown",
                Backend = editorPath is null ? "file-only" : liveConnected ? "file+live-editor" : "file+unity-editor",
                EditorAvailable = editorBacked,
                EditorPath = editorPath,
                LiveBridgeConnected = liveConnected,
            },
            Capabilities =
            [
                new UnityCapability { Name = "project", Status = "native-file", Description = "Scaffold projects, folders, packages, and safe ProjectSettings sidecar data." },
                new UnityCapability { Name = "scene-graph", Status = editorBacked ? "native-editor" : "partial-file", Description = "Hierarchy CRUD, component inspect/update, and prefab workflows. Editor-backed when UNITY_EDITOR_PATH or a live bridge is available." },
                new UnityCapability { Name = "assets", Status = editorBacked ? "native-editor" : "native-file", Description = "Create/read/move/delete assets with .meta sidecars; Editor uses AssetDatabase for reference-preserving moves." },
                new UnityCapability { Name = "validation", Status = editorBacked ? "roslyn+unity-editor" : "roslyn+file-lint", Description = "Roslyn syntax diagnostics, project lint, and Unity batch/live import validation when an Editor is available." },
                new UnityCapability { Name = "ui", Status = editorBacked ? "native-editor" : "compatibility-file", Description = "UGUI Canvas/RectTransform/Graphic operations via Editor bridge; file-only mode remains compatibility YAML." },
                new UnityCapability { Name = "advanced-systems", Status = editorBacked ? "native-editor" : "compatibility-surrogate", Description = "Animator, Timeline, VFX, NavMesh, Input System native writers when Editor-backed; JSON surrogates otherwise." },
            ],
            Notes =
            [
                "File-only mode cannot prove Unity importability. Use unity_validate_import with UNITY_EDITOR_PATH or an open Editor bridge for authoritative validation.",
                "JSON surrogate tools remain compatibility surfaces until native Editor-backed writers are used.",
                "Call unity_install_editor_bridge (or any native op) to embed com.unitymcp.bridge into the target project.",
            ],
        };

        return Task.FromResult(JsonSerializer.Serialize(manifest));
    }

    public async Task EnsureEditorBridgeInstalledAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        _projectPath = projectPath;
        if (_editorExecutor is null)
            throw new InvalidOperationException("No Unity Editor executor is registered. Register IUnityEditorExecutor in DI.");

        await _editorExecutor.EnsureBridgeInstalledAsync(projectPath, cancellationToken).ConfigureAwait(false);
    }

    public async Task CreateSceneAsync(string projectPath, string fileName, CancellationToken cancellationToken = default)
    {
        string resolvedPath = ResolvePath(projectPath, fileName);
        EnsureDirectoryExists(resolvedPath);
        var gameObjects = new List<GameObjectDef>
        {
            CreateDefaultCamera(),
            CreateDefaultLight(),
        };
        UnityYamlWriter.ResetFileIdCounter();
        string yaml = UnityYamlWriter.WriteScene(gameObjects);
        await _fs.File.WriteAllTextAsync(resolvedPath, yaml, cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(resolvedPath, ct: cancellationToken);
        _logger.LogInformation("Scene created at {Path}", resolvedPath);
    }

    public async Task CreateScriptAsync(string projectPath, string fileName, string scriptName, string? content = null, CancellationToken cancellationToken = default)
    {
        string resolvedPath = ResolvePath(projectPath, fileName);
        EnsureDirectoryExists(resolvedPath);
        string scriptContent = content ?? $@"using UnityEngine;

public class {scriptName} : MonoBehaviour
{{
    void Start()
    {{
        
    }}

    void Update()
    {{
        
    }}
}}";
        await _fs.File.WriteAllTextAsync(resolvedPath, scriptContent, cancellationToken);
        await _metaWriter.WriteScriptMetaAsync(resolvedPath, ct: cancellationToken);
        _logger.LogInformation("Script created at {Path} (with .meta)", resolvedPath);
    }

    public Task<IEnumerable<string>> ListAssetsAsync(string projectPath, string folderName, string searchPattern = "*", CancellationToken cancellationToken = default)
    {
        string resolvedPath = ResolvePath(projectPath, folderName);
        if (!_fs.Directory.Exists(resolvedPath))
        {
            _logger.LogWarning("Directory not found: {Path}", resolvedPath);
            return Task.FromResult(Enumerable.Empty<string>());
        }
        var files = _fs.Directory.EnumerateFiles(resolvedPath, searchPattern, SearchOption.AllDirectories)
            .Select(f => f.Replace('\\', '/'))
            .Where(f => !f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f)
            .ToList();
        return Task.FromResult((IEnumerable<string>)files);
    }

    public async Task BuildProjectAsync(string projectPath, string buildTarget, string outputPath, CancellationToken cancellationToken = default)
    {
        string unityExe = FindUnityExecutable();
        string resolvedProject = _fs.Path.GetFullPath(projectPath);
        string arguments = string.Join(" ",
            "-quit", "-batchmode", "-nographics",
            $"-projectPath \"{resolvedProject}\"",
            $"-buildTarget {buildTarget}",
            $"-buildOutput \"{outputPath}\""
        );

        _logger.LogInformation("Starting Unity build: {Exe} {Args}", unityExe, arguments);
        int exitCode = await _processRunner.RunAsync(unityExe, arguments, cancellationToken);

        if (exitCode != 0)
            throw new Exception($"Unity build failed with exit code {exitCode}");

        _logger.LogInformation("Build completed for target {Target}", buildTarget);
    }

    public async Task CreateAssetAsync(string projectPath, string fileName, string content, CancellationToken cancellationToken = default)
    {
        string resolvedPath = ResolvePath(projectPath, fileName);
        EnsureDirectoryExists(resolvedPath);
        await _fs.File.WriteAllTextAsync(resolvedPath, content, cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(resolvedPath, ct: cancellationToken);
        _logger.LogInformation("Asset created at {Path} (with .meta)", resolvedPath);
    }

    public async Task CreateGameObjectAsync(string projectPath, string fileName, string gameObjectName, CancellationToken cancellationToken = default)
    {
        string resolvedScenePath = ResolvePath(projectPath, fileName);
        EnsureDirectoryExists(resolvedScenePath);
        var go = new GameObjectDef { Name = gameObjectName };
        string fragment = UnityYamlWriter.WriteGameObjectFragment(go);
        await _fs.File.AppendAllTextAsync(resolvedScenePath, fragment, cancellationToken);
        _logger.LogInformation("GameObject {Name} appended to {Scene}", gameObjectName, resolvedScenePath);
    }

    // -----------------------------------------------------------------------
    // Enhanced AI-driven scene authoring tools
    // -----------------------------------------------------------------------

    public async Task CreateDetailedSceneAsync(string projectPath, string fileName, string sceneJson, CancellationToken cancellationToken = default)
    {
        string resolvedPath = ResolvePath(projectPath, fileName);
        EnsureDirectoryExists(resolvedPath);
        var defs = DeserializeGameObjects(sceneJson);
        UnityYamlWriter.ResetFileIdCounter();
        string yaml = UnityYamlWriter.WriteScene(defs);
        await _fs.File.WriteAllTextAsync(resolvedPath, yaml, cancellationToken);
        _logger.LogInformation("Detailed scene created at {Path} with {Count} GameObjects", resolvedPath, defs.Count);
    }

    public async Task AddGameObjectToSceneAsync(string projectPath, string fileName, string gameObjectJson, CancellationToken cancellationToken = default)
    {
        string resolvedScenePath = ResolvePath(projectPath, fileName);
        if (!_fs.File.Exists(resolvedScenePath))
            throw new FileNotFoundException($"Scene file not found: {resolvedScenePath}");
        var go = DeserializeGameObject(gameObjectJson);
        string fragment = UnityYamlWriter.WriteGameObjectFragment(go);
        await _fs.File.AppendAllTextAsync(resolvedScenePath, fragment, cancellationToken);
        _logger.LogInformation("GameObject {Name} added to scene {Scene}", go.Name, resolvedScenePath);
    }

    public async Task CreateMaterialAsync(string projectPath, string fileName, string materialJson, CancellationToken cancellationToken = default)
    {
        string resolvedPath = ResolvePath(projectPath, fileName);
        EnsureDirectoryExists(resolvedPath);
        var matDef = JsonSerializer.Deserialize<MaterialDef>(materialJson, JsonOpts)
            ?? new MaterialDef();
        UnityYamlWriter.ResetFileIdCounter();
        string yaml = UnityYamlWriter.WriteMaterial(matDef);
        await _fs.File.WriteAllTextAsync(resolvedPath, yaml, cancellationToken);
        _logger.LogInformation("Material created at {Path}", resolvedPath);
    }

    public async Task CreatePrefabAsync(string projectPath, string fileName, string prefabJson, CancellationToken cancellationToken = default)
    {
        string resolvedPath = ResolvePath(projectPath, fileName);
        EnsureDirectoryExists(resolvedPath);
        var go = DeserializeGameObject(prefabJson);
        UnityYamlWriter.ResetFileIdCounter();
        string yaml = UnityYamlWriter.WritePrefab(go);
        await _fs.File.WriteAllTextAsync(resolvedPath, yaml, cancellationToken);
        _logger.LogInformation("Prefab created at {Path}", resolvedPath);
    }

    public Task<string> ReadAssetAsync(string projectPath, string fileName, CancellationToken cancellationToken = default)
    {
        string resolvedPath = ResolvePath(projectPath, fileName);
        if (!_fs.File.Exists(resolvedPath))
            throw new FileNotFoundException($"File not found: {resolvedPath}");
        return _fs.File.ReadAllTextAsync(resolvedPath, cancellationToken);
    }

    public Task DeleteAssetAsync(string projectPath, string fileName, CancellationToken cancellationToken = default)
    {
        string resolvedPath = ResolvePath(projectPath, fileName);
        if (_fs.File.Exists(resolvedPath))
        {
            _fs.File.Delete(resolvedPath);
            _logger.LogInformation("Deleted asset at {Path}", resolvedPath);
            string metaPath = resolvedPath + ".meta";
            if (_fs.File.Exists(metaPath))
                _fs.File.Delete(metaPath);
        }
        else
        {
            _logger.LogWarning("File not found for deletion: {Path}", resolvedPath);
        }
        return Task.CompletedTask;
    }

    public async Task<string> ListSceneObjectsAsync(string projectPath, string fileName, CancellationToken cancellationToken = default)
    {
        string resolvedPath = ResolvePath(projectPath, fileName);
        if (!_fs.File.Exists(resolvedPath))
            return SerializeFailure<UnitySceneGraph>("SceneGraph.NotFound", $"Scene or prefab not found: {resolvedPath}", UnityMcpErrorCategory.Io);

        string content = await _fs.File.ReadAllTextAsync(resolvedPath, cancellationToken);
        var graph = BuildSceneGraph(projectPath, resolvedPath, content);
        return JsonSerializer.Serialize(new ToolResultEnvelope<UnitySceneGraph>
        {
            Success = true,
            Data = graph,
            Message = "Scene graph listed.",
        });
    }

    public async Task<string> RenameSceneObjectAsync(string projectPath, string fileName, string objectPath, string newName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectPath))
            return SerializeFailure<object>("SceneGraph.InvalidObjectPath", "objectPath is required.", UnityMcpErrorCategory.Validation);
        if (string.IsNullOrWhiteSpace(newName))
            return SerializeFailure<object>("SceneGraph.InvalidName", "newName is required.", UnityMcpErrorCategory.Validation);

        string resolvedPath = ResolvePath(projectPath, fileName);
        if (!_fs.File.Exists(resolvedPath))
            return SerializeFailure<object>("SceneGraph.NotFound", $"Scene or prefab not found: {resolvedPath}", UnityMcpErrorCategory.Io);

        string content = await _fs.File.ReadAllTextAsync(resolvedPath, cancellationToken);
        var graph = BuildSceneGraph(projectPath, resolvedPath, content);
        var target = FindSceneObject(graph, objectPath);
        if (target is null)
            return SerializeFailure<object>("SceneGraph.ObjectNotFound", $"GameObject '{objectPath}' was not found.", UnityMcpErrorCategory.Validation);

        string updated = ReplaceGameObjectName(content, target.FileId, newName.Trim());
        await _fs.File.WriteAllTextAsync(resolvedPath, updated, cancellationToken);
        return JsonSerializer.Serialize(new ToolResultEnvelope<object>
        {
            Success = true,
            Message = $"Renamed '{objectPath}' to '{newName.Trim()}'.",
            Data = new { path = MakeProjectRelativePath(projectPath, resolvedPath), objectPath, newName = newName.Trim() },
        });
    }

    public async Task<string> RemoveSceneObjectAsync(string projectPath, string fileName, string objectPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectPath))
            return SerializeFailure<object>("SceneGraph.InvalidObjectPath", "objectPath is required.", UnityMcpErrorCategory.Validation);

        string resolvedPath = ResolvePath(projectPath, fileName);
        if (!_fs.File.Exists(resolvedPath))
            return SerializeFailure<object>("SceneGraph.NotFound", $"Scene or prefab not found: {resolvedPath}", UnityMcpErrorCategory.Io);

        string content = await _fs.File.ReadAllTextAsync(resolvedPath, cancellationToken);
        var graph = BuildSceneGraph(projectPath, resolvedPath, content);
        var target = FindSceneObject(graph, objectPath);
        if (target is null)
            return SerializeFailure<object>("SceneGraph.ObjectNotFound", $"GameObject '{objectPath}' was not found.", UnityMcpErrorCategory.Validation);

        string updated = RemoveYamlDocumentsForObject(content, target.FileId);
        await _fs.File.WriteAllTextAsync(resolvedPath, updated, cancellationToken);
        return JsonSerializer.Serialize(new ToolResultEnvelope<object>
        {
            Success = true,
            Message = $"Removed GameObject '{objectPath}'.",
            Data = new { path = MakeProjectRelativePath(projectPath, resolvedPath), objectPath },
        });
    }

    public async Task<string> DiffSceneFilesAsync(string projectPath, string fileNameA, string fileNameB, CancellationToken cancellationToken = default)
    {
        if (IsEditorAvailable(projectPath))
        {
            return await _editorExecutor!.ExecuteAsync(projectPath, "scene.diff",
                new Dictionary<string, object?>
                {
                    ["fileNameA"] = ToProjectRelative(projectPath, fileNameA),
                    ["fileNameB"] = ToProjectRelative(projectPath, fileNameB),
                }, cancellationToken).ConfigureAwait(false);
        }

        string pathA = ResolvePath(projectPath, fileNameA);
        string pathB = ResolvePath(projectPath, fileNameB);
        if (!_fs.File.Exists(pathA) || !_fs.File.Exists(pathB))
            return SerializeFailure<UnitySceneDiff>("SceneDiff.NotFound", "Both scene or prefab files must exist.", UnityMcpErrorCategory.Io);

        var graphA = BuildSceneGraph(projectPath, pathA, await _fs.File.ReadAllTextAsync(pathA, cancellationToken));
        var graphB = BuildSceneGraph(projectPath, pathB, await _fs.File.ReadAllTextAsync(pathB, cancellationToken));
        var byNameA = graphA.Objects.ToDictionary(o => o.Path, StringComparer.OrdinalIgnoreCase);
        var byNameB = graphB.Objects.ToDictionary(o => o.Path, StringComparer.OrdinalIgnoreCase);

        var modified = new List<string>();
        foreach (string key in byNameA.Keys.Intersect(byNameB.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var left = byNameA[key];
            var right = byNameB[key];
            if (!left.Components.SequenceEqual(right.Components) || !DictionaryEquals(left.Properties, right.Properties))
                modified.Add(key);
        }

        var diff = new UnitySceneDiff
        {
            Added = byNameB.Keys.Except(byNameA.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray(),
            Removed = byNameA.Keys.Except(byNameB.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray(),
            Modified = modified.OrderBy(x => x).ToArray(),
        };

        return JsonSerializer.Serialize(new ToolResultEnvelope<UnitySceneDiff>
        {
            Success = true,
            Data = diff,
            Message = "Scene diff completed.",
        });
    }

    public async Task<string> AttachScriptAsync(string projectPath, string fileName, string objectPath, string scriptFileName, CancellationToken cancellationToken = default)
    {
        if (IsEditorAvailable(projectPath))
        {
            return await _editorExecutor!.ExecuteAsync(projectPath, "scene.attach_script",
                new Dictionary<string, object?>
                {
                    ["fileName"] = ToProjectRelative(projectPath, fileName),
                    ["objectPath"] = objectPath,
                    ["scriptPath"] = ToProjectRelative(projectPath, scriptFileName),
                },
                cancellationToken).ConfigureAwait(false);
        }

        string scenePath = ResolvePath(projectPath, fileName);
        string scriptPath = ResolvePath(projectPath, scriptFileName);
        if (!_fs.File.Exists(scenePath))
            return SerializeFailure<object>("AttachScript.SceneNotFound", $"Scene or prefab not found: {scenePath}", UnityMcpErrorCategory.Io);
        if (!_fs.File.Exists(scriptPath))
            return SerializeFailure<object>("AttachScript.ScriptNotFound", $"Script not found: {scriptPath}", UnityMcpErrorCategory.Io);

        string? scriptGuid = TryReadGuid(scriptPath + ".meta");
        if (string.IsNullOrWhiteSpace(scriptGuid))
            return SerializeFailure<object>("AttachScript.MissingGuid", $"Script meta GUID not found: {scriptPath}.meta", UnityMcpErrorCategory.Validation);

        string content = await _fs.File.ReadAllTextAsync(scenePath, cancellationToken);
        var graph = BuildSceneGraph(projectPath, scenePath, content);
        var target = FindSceneObject(graph, objectPath);
        if (target is null)
            return SerializeFailure<object>("AttachScript.ObjectNotFound", $"GameObject '{objectPath}' was not found.", UnityMcpErrorCategory.Validation);

        long componentFileId = NextYamlFileId(content);
        string updated = AddComponentReference(content, target.FileId, componentFileId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        updated += $$"""

--- !u!114 &{{componentFileId}}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_GameObject: {fileID: {{target.FileId}}}
  m_Enabled: 1
  m_Script: {fileID: 11500000, guid: {{scriptGuid}}, type: 3}
  m_Name: 

""";
        await _fs.File.WriteAllTextAsync(scenePath, updated, cancellationToken);

        return JsonSerializer.Serialize(new ToolResultEnvelope<object>
        {
            Success = true,
            Message = $"Attached script '{scriptFileName}' to '{objectPath}'.",
            Data = new { scene = MakeProjectRelativePath(projectPath, scenePath), script = MakeProjectRelativePath(projectPath, scriptPath), objectPath },
        });
    }

    public async Task<string> InstantiatePrefabAsync(string projectPath, string sceneFileName, string prefabFileName, string instanceName, CancellationToken cancellationToken = default)
    {
        if (IsEditorAvailable(projectPath))
        {
            return await _editorExecutor!.ExecuteAsync(projectPath, "scene.instantiate_prefab",
                new Dictionary<string, object?>
                {
                    ["fileName"] = ToProjectRelative(projectPath, sceneFileName),
                    ["prefabPath"] = ToProjectRelative(projectPath, prefabFileName),
                    ["instanceName"] = instanceName,
                },
                cancellationToken).ConfigureAwait(false);
        }

        string scenePath = ResolvePath(projectPath, sceneFileName);
        string prefabPath = ResolvePath(projectPath, prefabFileName);
        if (!_fs.File.Exists(scenePath))
            return SerializeFailure<object>("Prefab.SceneNotFound", $"Scene not found: {scenePath}", UnityMcpErrorCategory.Io);
        if (!_fs.File.Exists(prefabPath))
            return SerializeFailure<object>("Prefab.NotFound", $"Prefab not found: {prefabPath}", UnityMcpErrorCategory.Io);

        string? prefabGuid = TryReadGuid(prefabPath + ".meta");
        if (string.IsNullOrWhiteSpace(prefabGuid))
            return SerializeFailure<object>("Prefab.MissingGuid", $"Prefab meta GUID not found: {prefabPath}.meta", UnityMcpErrorCategory.Validation);

        var go = new GameObjectDef { Name = string.IsNullOrWhiteSpace(instanceName) ? _fs.Path.GetFileNameWithoutExtension(prefabPath) : instanceName.Trim() };
        string content = await _fs.File.ReadAllTextAsync(scenePath, cancellationToken);
        string fragment = UnityYamlWriter.WriteGameObjectFragment(go) + $"# Prefab source: {{guid: {prefabGuid}, path: {MakeProjectRelativePath(projectPath, prefabPath)}}}\n";
        await _fs.File.WriteAllTextAsync(scenePath, content + fragment, cancellationToken);

        return JsonSerializer.Serialize(new ToolResultEnvelope<object>
        {
            Success = true,
            Message = $"Instantiated prefab '{prefabFileName}' as '{go.Name}'.",
            Data = new { scene = MakeProjectRelativePath(projectPath, scenePath), prefab = MakeProjectRelativePath(projectPath, prefabPath), instanceName = go.Name },
            Warnings =
            [
                new UnityMcpError { Category = UnityMcpErrorCategory.Contract, Code = "Prefab.LinkedInstancePartial", Message = "File-only mode appends a GameObject plus prefab source metadata; use Editor validation for full PrefabInstance fidelity." }
            ],
        });
    }

    public async Task<string> SaveObjectAsPrefabAsync(string projectPath, string sceneFileName, string objectPath, string prefabFileName, CancellationToken cancellationToken = default)
    {
        if (IsEditorAvailable(projectPath))
        {
            return await _editorExecutor!.ExecuteAsync(projectPath, "scene.save_as_prefab",
                new Dictionary<string, object?>
                {
                    ["fileName"] = ToProjectRelative(projectPath, sceneFileName),
                    ["objectPath"] = objectPath,
                    ["prefabPath"] = ToProjectRelative(projectPath, prefabFileName),
                }, cancellationToken).ConfigureAwait(false);
        }

        string scenePath = ResolvePath(projectPath, sceneFileName);
        string prefabPath = ResolvePath(projectPath, prefabFileName);
        if (!_fs.File.Exists(scenePath))
            return SerializeFailure<object>("Prefab.SceneNotFound", $"Scene not found: {scenePath}", UnityMcpErrorCategory.Io);

        string content = await _fs.File.ReadAllTextAsync(scenePath, cancellationToken);
        var graph = BuildSceneGraph(projectPath, scenePath, content);
        var target = FindSceneObject(graph, objectPath);
        if (target is null)
            return SerializeFailure<object>("Prefab.ObjectNotFound", $"GameObject '{objectPath}' was not found.", UnityMcpErrorCategory.Validation);

        string yaml = ExtractYamlDocumentsForObject(content, target.FileId);
        if (string.IsNullOrWhiteSpace(yaml))
            yaml = UnityYamlWriter.WritePrefab(new GameObjectDef { Name = target.Name });
        EnsureDirectoryExists(prefabPath);
        await _fs.File.WriteAllTextAsync(prefabPath, UnityYamlWriter.Header() + yaml, cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(prefabPath, ct: cancellationToken);

        return JsonSerializer.Serialize(new ToolResultEnvelope<object>
        {
            Success = true,
            Message = $"Saved '{objectPath}' as prefab.",
            Data = new { prefab = MakeProjectRelativePath(projectPath, prefabPath), source = objectPath },
        });
    }

    // -----------------------------------------------------------------------
    // JSON deserialization helpers
    // -----------------------------------------------------------------------

    private static List<GameObjectDef> DeserializeGameObjects(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Accept either a JSON array of GOs or an object with a "gameObjects" array
        JsonElement array;
        if (root.ValueKind == JsonValueKind.Array)
        {
            array = root;
        }
        else if (root.TryGetProperty("gameObjects", out var gosProp))
        {
            array = gosProp;
        }
        else
        {
            // Single GO
            return [DeserializeGameObjectFromElement(root)];
        }

        var result = new List<GameObjectDef>();
        foreach (var elem in array.EnumerateArray())
        {
            result.Add(DeserializeGameObjectFromElement(elem));
        }
        return result;
    }

    private static GameObjectDef DeserializeGameObject(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return DeserializeGameObjectFromElement(doc.RootElement);
    }

    private static GameObjectDef DeserializeGameObjectFromElement(JsonElement elem)
    {
        var go = new GameObjectDef();

        if (elem.TryGetProperty("name", out var name)) go.Name = name.GetString() ?? "GameObject";
        if (elem.TryGetProperty("tag", out var tag)) go.Tag = tag.GetString() ?? "Untagged";
        if (elem.TryGetProperty("layer", out var layer)) go.Layer = layer.GetInt32();
        if (elem.TryGetProperty("isActive", out var active)) go.IsActive = active.GetBoolean();
        if (elem.TryGetProperty("position", out var pos)) go.Position = ParseVector3(pos);
        if (elem.TryGetProperty("scale", out var scale)) go.Scale = ParseVector3(scale);
        if (elem.TryGetProperty("eulerAngles", out var euler))
        {
            go.EulerAngles = ParseVector3(euler);
            go.Rotation = EulerToQuaternion(go.EulerAngles);
        }

        if (elem.TryGetProperty("components", out var comps))
        {
            foreach (var compElem in comps.EnumerateArray())
            {
                go.Components.Add(ParseComponent(compElem));
            }
        }

        return go;
    }

    private static ComponentDef ParseComponent(JsonElement elem)
    {
        string type = elem.TryGetProperty("type", out var t)
            ? t.GetString()?.ToLowerInvariant() ?? ""
            : "";

        int classId = type switch
        {
            "camera" => UnityYamlWriter.ClassId_Camera,
            "light" => UnityYamlWriter.ClassId_Light,
            "meshfilter" => UnityYamlWriter.ClassId_MeshFilter,
            "meshrenderer" => UnityYamlWriter.ClassId_MeshRenderer,
            "boxcollider" => UnityYamlWriter.ClassId_BoxCollider,
            "spherecollider" => UnityYamlWriter.ClassId_SphereCollider,
            "capsulecollider" => UnityYamlWriter.ClassId_CapsuleCollider,
            "rigidbody" => UnityYamlWriter.ClassId_Rigidbody,
            "audiosource" => UnityYamlWriter.ClassId_AudioSource,
            _ => UnityYamlWriter.ClassId_MonoBehaviour,
        };

        var comp = new ComponentDef(classId);

        // Parse all extra properties
        foreach (var prop in elem.EnumerateObject())
        {
            if (prop.Name == "type") continue;

            switch (prop.Value.ValueKind)
            {
                case JsonValueKind.Number:
                    comp.Properties[prop.Name] = prop.Value.GetDouble();
                    break;
                case JsonValueKind.True:
                    comp.Properties[prop.Name] = true;
                    break;
                case JsonValueKind.False:
                    comp.Properties[prop.Name] = false;
                    break;
                case JsonValueKind.String:
                    comp.Properties[prop.Name] = prop.Value.GetString() ?? "";
                    break;
                case JsonValueKind.Object:
                    // Could be a vector3 or color
                    if (prop.Value.TryGetProperty("r", out _))
                        comp.Properties[prop.Name] = ParseColor(prop.Value);
                    else if (prop.Value.TryGetProperty("x", out _))
                        comp.Properties[prop.Name] = ParseVector3(prop.Value);
                    break;
            }
        }

        return comp;
    }

    private static Vector3Def ParseVector3(JsonElement elem)
    {
        float x = elem.TryGetProperty("x", out var xp) ? (float)xp.GetDouble() : 0;
        float y = elem.TryGetProperty("y", out var yp) ? (float)yp.GetDouble() : 0;
        float z = elem.TryGetProperty("z", out var zp) ? (float)zp.GetDouble() : 0;
        return new Vector3Def(x, y, z);
    }

    private static ColorDef ParseColor(JsonElement elem)
    {
        float r = elem.TryGetProperty("r", out var rp) ? (float)rp.GetDouble() : 1;
        float g = elem.TryGetProperty("g", out var gp) ? (float)gp.GetDouble() : 1;
        float b = elem.TryGetProperty("b", out var bp) ? (float)bp.GetDouble() : 1;
        float a = elem.TryGetProperty("a", out var ap) ? (float)ap.GetDouble() : 1;
        return new ColorDef(r, g, b, a);
    }

    private static QuaternionDef EulerToQuaternion(Vector3Def euler)
    {
        // Convert euler angles (degrees) to quaternion
        double pitch = euler.X * Math.PI / 360.0; // half-angle in radians
        double yaw = euler.Y * Math.PI / 360.0;
        double roll = euler.Z * Math.PI / 360.0;

        double cy = Math.Cos(yaw), sy = Math.Sin(yaw);
        double cp = Math.Cos(pitch), sp = Math.Sin(pitch);
        double cr = Math.Cos(roll), sr = Math.Sin(roll);

        return new QuaternionDef(
            (float)(sr * cp * cy - cr * sp * sy),
            (float)(cr * sp * cy + sr * cp * sy),
            (float)(cr * cp * sy - sr * sp * cy),
            (float)(cr * cp * cy + sr * sp * sy)
        );
    }

    // -----------------------------------------------------------------------
    // Default scene object helpers
    // -----------------------------------------------------------------------

    private static GameObjectDef CreateDefaultCamera()
    {
        return new GameObjectDef
        {
            Name = "Main Camera",
            Tag = "MainCamera",
            Position = new Vector3Def(0, 1, -10),
            Components =
            [
                new ComponentDef(UnityYamlWriter.ClassId_Camera)
                {
                    Properties = { ["clearFlags"] = 1, ["fov"] = 60f, ["nearClip"] = 0.3f, ["farClip"] = 1000f }
                }
            ]
        };
    }

    private static GameObjectDef CreateDefaultLight()
    {
        return new GameObjectDef
        {
            Name = "Directional Light",
            EulerAngles = new Vector3Def(50, -30, 0),
            Rotation = EulerToQuaternion(new Vector3Def(50, -30, 0)),
            Components =
            [
                new ComponentDef(UnityYamlWriter.ClassId_Light)
                {
                    Properties = { ["type"] = 1, ["intensity"] = 1f }
                }
            ]
        };
    }

    private static GameObjectDef CreateDefaultGroundPlane()
    {
        return new GameObjectDef
        {
            Name = "Ground",
            Position = new Vector3Def(0, 0, 0),
            Scale = new Vector3Def(5, 1, 5),
            Components =
            [
                new ComponentDef(UnityYamlWriter.ClassId_MeshFilter) { Properties = { ["mesh"] = "Plane" } },
                new ComponentDef(UnityYamlWriter.ClassId_MeshRenderer)
            ]
        };
    }

    // -----------------------------------------------------------------------
    // Project scaffolding & management
    // -----------------------------------------------------------------------

    public async Task<string> ScaffoldProjectAsync(string projectName, string? outputRoot = null, string? unityVersion = null, CancellationToken cancellationToken = default)
    {
        string root = outputRoot ?? _fs.Path.Combine(_fs.Directory.GetCurrentDirectory(), "output");
        string safeName = System.Text.RegularExpressions.Regex.Replace(projectName, @"[^a-zA-Z0-9_\-]", "_").Trim('_');
        if (string.IsNullOrEmpty(safeName)) safeName = "UnityProject";

        string projectDir = _fs.Path.Combine(root, safeName);

        // Standard Unity folder structure
        string[] folders = {
            projectDir,
            _fs.Path.Combine(projectDir, "Assets"),
            _fs.Path.Combine(projectDir, "Assets", "Scripts"),
            _fs.Path.Combine(projectDir, "Assets", "Scenes"),
            _fs.Path.Combine(projectDir, "Assets", "Prefabs"),
            _fs.Path.Combine(projectDir, "Assets", "Materials"),
            _fs.Path.Combine(projectDir, "Assets", "Textures"),
            _fs.Path.Combine(projectDir, "Assets", "Audio"),
            _fs.Path.Combine(projectDir, "Assets", "Text"),
            _fs.Path.Combine(projectDir, "ProjectSettings"),
            _fs.Path.Combine(projectDir, "Packages"),
        };

        foreach (var folder in folders)
        {
            _fs.Directory.CreateDirectory(folder);
            string folderMeta = folder + ".meta";
            if (!_fs.File.Exists(folderMeta))
                await _metaWriter.WriteFolderMetaAsync(folder, ct: cancellationToken);
        }

        // ProjectVersion.txt
        string version = unityVersion ?? "2022.3.0f1";
        string versionFile = _fs.Path.Combine(projectDir, "ProjectSettings", "ProjectVersion.txt");
        if (!_fs.File.Exists(versionFile))
        {
            await _fs.File.WriteAllTextAsync(versionFile,
                $"m_EditorVersion: {version}\nm_EditorVersionWithRevision: {version} (placeholder)\n",
                cancellationToken);
        }

        // Packages/manifest.json
        string manifestPath = _fs.Path.Combine(projectDir, "Packages", "manifest.json");
        if (!_fs.File.Exists(manifestPath))
        {
            string manifest = "{\n  \"dependencies\": {}\n}";
            await _fs.File.WriteAllTextAsync(manifestPath, manifest, cancellationToken);
        }

        // README
        string readmePath = _fs.Path.Combine(projectDir, "README.txt");
        if (!_fs.File.Exists(readmePath))
        {
            await _fs.File.WriteAllTextAsync(readmePath,
                $"Generated by Unity MCP Server.\nProject: {projectName}\nImport this folder as a Unity project.\n",
                cancellationToken);
            await _metaWriter.WriteDefaultMetaAsync(readmePath, ct: cancellationToken);
        }

        _logger.LogInformation("Project scaffolded at {Path}", projectDir);
        return projectDir;
    }

    public Task<string> GetProjectInfoAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        string name = _fs.Path.GetFileName(projectPath);
        string versionPath = _fs.Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt");
        string unityVersion = "unknown";

        if (_fs.File.Exists(versionPath))
        {
            string content = _fs.File.ReadAllText(versionPath);
            var match = System.Text.RegularExpressions.Regex.Match(content, @"m_EditorVersion:\s*(\S+)");
            if (match.Success) unityVersion = match.Groups[1].Value;
        }

        bool hasAssets = _fs.Directory.Exists(_fs.Path.Combine(projectPath, "Assets"));

        var info = JsonSerializer.Serialize(new
        {
            projectName = name,
            projectPath = _fs.Path.GetFullPath(projectPath),
            unityVersion,
            hasAssets,
        });

        return Task.FromResult(info);
    }

    public async Task CreateFolderAsync(string projectPath, string folderName, CancellationToken cancellationToken = default)
    {
        string resolvedPath = ResolvePath(projectPath, folderName);
        _fs.Directory.CreateDirectory(resolvedPath);
        await _metaWriter.WriteFolderMetaAsync(resolvedPath, ct: cancellationToken);
        _logger.LogInformation("Folder created at {Path} (with .meta)", resolvedPath);
    }

    // -----------------------------------------------------------------------
    // Typed asset saving (with correct .meta sidecars)
    // -----------------------------------------------------------------------

    public async Task SaveScriptAsync(string projectPath, string fileName, string content, CancellationToken cancellationToken = default)
    {
        string filePath = ResolveAssetPath(projectPath, fileName, "Scripts");
        string dir = _fs.Path.GetDirectoryName(filePath)!;
        _fs.Directory.CreateDirectory(dir);
        await _fs.File.WriteAllTextAsync(filePath, content, cancellationToken);
        await _metaWriter.WriteScriptMetaAsync(filePath, ct: cancellationToken);
        _logger.LogInformation("Script saved at {Path} (with MonoImporter .meta)", filePath);
    }

    public async Task SaveTextAssetAsync(string projectPath, string fileName, string content, CancellationToken cancellationToken = default)
    {
        string filePath = ResolveAssetPath(projectPath, fileName, "Text");
        string dir = _fs.Path.GetDirectoryName(filePath)!;
        _fs.Directory.CreateDirectory(dir);
        await _fs.File.WriteAllTextAsync(filePath, content, cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(filePath, ct: cancellationToken);
        _logger.LogInformation("Text asset saved at {Path} (with .meta)", filePath);
    }

    public async Task SaveTextureAsync(string projectPath, string fileName, string base64Data, CancellationToken cancellationToken = default)
    {
        string filePath = ResolveAssetPath(projectPath, fileName, "Textures");
        string dir = _fs.Path.GetDirectoryName(filePath)!;
        _fs.Directory.CreateDirectory(dir);
        byte[] data = Convert.FromBase64String(base64Data);
        await _fs.File.WriteAllBytesAsync(filePath, data, cancellationToken);
        await _metaWriter.WriteTextureMetaAsync(filePath, ct: cancellationToken);
        _logger.LogInformation("Texture saved at {Path} (with TextureImporter .meta)", filePath);
    }

    public async Task SaveAudioAsync(string projectPath, string fileName, string base64Data, CancellationToken cancellationToken = default)
    {
        string filePath = ResolveAssetPath(projectPath, fileName, "Audio");
        string dir = _fs.Path.GetDirectoryName(filePath)!;
        _fs.Directory.CreateDirectory(dir);
        byte[] data = Convert.FromBase64String(base64Data);
        await _fs.File.WriteAllBytesAsync(filePath, data, cancellationToken);
        await _metaWriter.WriteAudioMetaAsync(filePath, ct: cancellationToken);
        _logger.LogInformation("Audio saved at {Path} (with AudioImporter .meta)", filePath);
    }

    public Task<string> GetAssetMetadataAsync(string projectPath, string fileName, CancellationToken cancellationToken = default)
    {
        string resolvedPath = ResolvePath(projectPath, fileName);
        if (!_fs.File.Exists(resolvedPath))
            return Task.FromResult(SerializeFailure<UnityAssetMetadata>("Asset.NotFound", $"Asset not found: {resolvedPath}", UnityMcpErrorCategory.Io));

        var metadata = BuildAssetMetadata(projectPath, resolvedPath);
        return Task.FromResult(JsonSerializer.Serialize(new ToolResultEnvelope<UnityAssetMetadata>
        {
            Success = true,
            Data = metadata,
            Message = "Asset metadata read.",
        }));
    }

    public Task<string> ListAssetMetadataAsync(string projectPath, string folderName = "Assets", string searchPattern = "*", CancellationToken cancellationToken = default)
    {
        string folderPath = ResolvePath(projectPath, string.IsNullOrWhiteSpace(folderName) ? "Assets" : folderName);
        if (!_fs.Directory.Exists(folderPath))
            return Task.FromResult(SerializeFailure<IReadOnlyList<UnityAssetMetadata>>("Asset.FolderNotFound", $"Folder not found: {folderPath}", UnityMcpErrorCategory.Io));

        var assets = _fs.Directory.EnumerateFiles(folderPath, string.IsNullOrWhiteSpace(searchPattern) ? "*" : searchPattern, SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => BuildAssetMetadata(projectPath, path))
            .ToArray();

        return Task.FromResult(JsonSerializer.Serialize(new ToolResultEnvelope<IReadOnlyList<UnityAssetMetadata>>
        {
            Success = true,
            Data = assets,
            Message = $"Listed {assets.Length} assets.",
        }));
    }

    public Task<string> MoveAssetAsync(string projectPath, string sourceFileName, string destinationFileName, CancellationToken cancellationToken = default)
        => PreferEditorAsync(projectPath, "asset.move",
            new Dictionary<string, object?>
            {
                ["sourceFileName"] = ToProjectRelative(projectPath, sourceFileName),
                ["destinationFileName"] = ToProjectRelative(projectPath, destinationFileName),
            },
            () => MoveAssetFileOnlyAsync(projectPath, sourceFileName, destinationFileName),
            cancellationToken);

    private Task<string> MoveAssetFileOnlyAsync(string projectPath, string sourceFileName, string destinationFileName)
    {
        string source = ResolvePath(projectPath, sourceFileName);
        string destination = ResolvePath(projectPath, destinationFileName);
        if (!_fs.File.Exists(source))
            return Task.FromResult(SerializeFailure<object>("Asset.NotFound", $"Source asset not found: {source}", UnityMcpErrorCategory.Io));
        if (_fs.File.Exists(destination))
            return Task.FromResult(SerializeFailure<object>("Asset.Exists", $"Destination already exists: {destination}", UnityMcpErrorCategory.Validation));

        EnsureDirectoryExists(destination);
        _fs.File.Move(source, destination);
        if (_fs.File.Exists(source + ".meta"))
            _fs.File.Move(source + ".meta", destination + ".meta");

        return Task.FromResult(JsonSerializer.Serialize(new ToolResultEnvelope<object>
        {
            Success = true,
            Message = "Asset moved.",
            Data = new { source = MakeProjectRelativePath(projectPath, source), destination = MakeProjectRelativePath(projectPath, destination) },
        }));
    }

    public async Task<string> UpdateMaterialPropertiesAsync(string projectPath, string fileName, string propertiesJson, CancellationToken cancellationToken = default)
    {
        string materialPath = ResolvePath(projectPath, fileName);
        if (!_fs.File.Exists(materialPath))
            return SerializeFailure<object>("Material.NotFound", $"Material not found: {materialPath}", UnityMcpErrorCategory.Io);

        Dictionary<string, JsonElement>? properties;
        try
        {
            properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(propertiesJson, JsonOpts);
        }
        catch (Exception ex)
        {
            return SerializeFailure<object>("Material.InvalidJson", $"Material properties JSON could not be parsed: {ex.Message}", UnityMcpErrorCategory.Validation);
        }

        if (properties is null || properties.Count == 0)
            return SerializeFailure<object>("Material.EmptyProperties", "At least one material property is required.", UnityMcpErrorCategory.Validation);

        string content = await _fs.File.ReadAllTextAsync(materialPath, cancellationToken);
        if (properties.TryGetValue("name", out var nameValue) && nameValue.ValueKind == JsonValueKind.String)
            content = Regex.Replace(content, @"(?m)^  m_Name: .*$", $"  m_Name: {EscapeYamlScalar(nameValue.GetString() ?? "Material")}");

        foreach (var colorProperty in properties.Where(p => p.Value.ValueKind == JsonValueKind.Object && p.Value.TryGetProperty("r", out _)))
        {
            string yamlColor = FormatJsonColor(colorProperty.Value);
            content = ReplaceYamlListEntry(content, colorProperty.Key, yamlColor);
        }

        foreach (var numericProperty in properties.Where(p => p.Value.ValueKind == JsonValueKind.Number))
        {
            string value = numericProperty.Value.GetDouble().ToString("G", System.Globalization.CultureInfo.InvariantCulture);
            content = ReplaceYamlListEntry(content, numericProperty.Key, value);
        }

        await _fs.File.WriteAllTextAsync(materialPath, content, cancellationToken);
        return JsonSerializer.Serialize(new ToolResultEnvelope<object>
        {
            Success = true,
            Message = "Material properties updated.",
            Data = new { material = MakeProjectRelativePath(projectPath, materialPath), updated = properties.Keys.ToArray() },
        });
    }

    public async Task<string> AssignMaterialTextureAsync(string projectPath, string materialFileName, string textureFileName, string propertyName, CancellationToken cancellationToken = default)
    {
        string materialPath = ResolvePath(projectPath, materialFileName);
        string texturePath = ResolvePath(projectPath, textureFileName);
        if (!_fs.File.Exists(materialPath))
            return SerializeFailure<object>("Material.NotFound", $"Material not found: {materialPath}", UnityMcpErrorCategory.Io);
        if (!_fs.File.Exists(texturePath))
            return SerializeFailure<object>("Texture.NotFound", $"Texture not found: {texturePath}", UnityMcpErrorCategory.Io);

        string? guid = TryReadGuid(texturePath + ".meta");
        if (string.IsNullOrWhiteSpace(guid))
            return SerializeFailure<object>("Texture.MissingGuid", $"Texture meta GUID not found: {texturePath}.meta", UnityMcpErrorCategory.Validation);

        string key = string.IsNullOrWhiteSpace(propertyName) ? "_MainTex" : propertyName.Trim();
        string content = await _fs.File.ReadAllTextAsync(materialPath, cancellationToken);
        string textureRef = $"{{fileID: 2800000, guid: {guid}, type: 3}}";
        if (content.Contains($"- {key}:"))
            content = Regex.Replace(content, $@"(?m)^    - {Regex.Escape(key)}: .*$", $"    - {key}: {textureRef}");
        else
            content = content.Replace("  m_TexEnvs: []", $"  m_TexEnvs:\n    - {key}: {textureRef}");

        await _fs.File.WriteAllTextAsync(materialPath, content, cancellationToken);
        return JsonSerializer.Serialize(new ToolResultEnvelope<object>
        {
            Success = true,
            Message = "Texture assigned to material.",
            Data = new { material = MakeProjectRelativePath(projectPath, materialPath), texture = MakeProjectRelativePath(projectPath, texturePath), propertyName = key },
        });
    }

    public Task<string> LintProjectAsync(string projectPath, CancellationToken cancellationToken = default)
        => PreferEditorAsync(projectPath, "asset.lint", null, () => LintProjectFileOnlyAsync(projectPath, cancellationToken), cancellationToken);

    private Task<string> LintProjectFileOnlyAsync(string projectPath, CancellationToken cancellationToken)
    {
        string projectRoot = ValidateProjectRoot(projectPath, requireExists: true);
        var errors = new List<UnityMcpError>();
        var warnings = new List<UnityMcpError>();

        string assetsPath = _fs.Path.Combine(projectRoot, "Assets");
        if (!_fs.Directory.Exists(assetsPath))
            errors.Add(new UnityMcpError { Category = UnityMcpErrorCategory.Validation, Code = "Lint.MissingAssets", Message = "Project is missing Assets/." });

        if (_fs.Directory.Exists(assetsPath))
        {
            var assetFiles = _fs.Directory.EnumerateFiles(assetsPath, "*", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            foreach (string asset in assetFiles)
            {
                if (!_fs.File.Exists(asset + ".meta"))
                    warnings.Add(new UnityMcpError { Category = UnityMcpErrorCategory.Validation, Code = "Lint.MissingMeta", Message = $"Missing .meta sidecar: {MakeProjectRelativePath(projectRoot, asset)}" });

                if (asset.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) || asset.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) || asset.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                {
                    string content = _fs.File.ReadAllText(asset);
                    foreach (Match match in Regex.Matches(content, @"guid:\s*([a-fA-F0-9]{32})"))
                    {
                        if (!GuidExists(projectRoot, match.Groups[1].Value))
                            warnings.Add(new UnityMcpError { Category = UnityMcpErrorCategory.Validation, Code = "Lint.BrokenGuidReference", Message = $"Broken GUID reference {match.Groups[1].Value} in {MakeProjectRelativePath(projectRoot, asset)}" });
                    }

                    if (content.Contains("m_Script: {fileID: 0}", StringComparison.Ordinal))
                        warnings.Add(new UnityMcpError { Category = UnityMcpErrorCategory.Validation, Code = "Lint.MissingScript", Message = $"Missing script reference in {MakeProjectRelativePath(projectRoot, asset)}" });
                }
            }
        }

        return Task.FromResult(JsonSerializer.Serialize(new ImportValidationResult
        {
            Success = errors.Count == 0,
            ErrorCount = errors.Count,
            WarningCount = warnings.Count,
            Errors = errors,
            Warnings = warnings,
            Message = errors.Count == 0 ? "Project lint completed." : "Project lint found errors.",
        }));
    }

    // -----------------------------------------------------------------------
    // Validation & package management
    // -----------------------------------------------------------------------

    public Task<string> ValidateCSharpAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Task.FromResult(JsonSerializer.Serialize(new
            {
                isValid = false,
                errors = new[] { "Code must be a non-empty string." },
                diagnostics = Array.Empty<object>(),
            }));
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(code, cancellationToken: cancellationToken);
        var diagnostics = syntaxTree.GetDiagnostics(cancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning)
            .Select(d =>
            {
                var span = d.Location.GetLineSpan();
                return new
                {
                    id = d.Id,
                    severity = d.Severity.ToString(),
                    message = d.GetMessage(),
                    line = span.StartLinePosition.Line + 1,
                    character = span.StartLinePosition.Character + 1,
                };
            })
            .ToArray();

        var errors = diagnostics
            .Where(d => string.Equals(d.severity, nameof(DiagnosticSeverity.Error), StringComparison.Ordinal))
            .Select(d => $"{d.id}: {d.message} (line {d.line}, column {d.character})")
            .ToList();

        if (!Regex.IsMatch(code, @"\b(class|struct|interface|enum|record)\b"))
            errors.Add("No type declaration found (expected class, struct, interface, enum, or record).");

        bool isValid = errors.Count == 0;
        var result = JsonSerializer.Serialize(new { isValid, errors, diagnostics });
        return Task.FromResult(result);
    }

    public async Task AddPackagesAsync(string projectPath, string packagesJson, CancellationToken cancellationToken = default)
    {
        string manifestPath = _fs.Path.Combine(projectPath, "Packages", "manifest.json");

        // Read existing manifest or create new
        JsonDocument existingDoc;
        if (_fs.File.Exists(manifestPath))
        {
            string existing = await _fs.File.ReadAllTextAsync(manifestPath, cancellationToken);
            existingDoc = JsonDocument.Parse(existing);
        }
        else
        {
            _fs.Directory.CreateDirectory(_fs.Path.GetDirectoryName(manifestPath)!);
            existingDoc = JsonDocument.Parse("{\"dependencies\":{}}");
        }

        // Parse packages to add: expects {"package.id": "version", ...}
        var newPackages = JsonDocument.Parse(packagesJson);

        // Merge
        var merged = new Dictionary<string, string>();

        if (existingDoc.RootElement.TryGetProperty("dependencies", out var deps))
        {
            foreach (var prop in deps.EnumerateObject())
                merged[prop.Name] = prop.Value.GetString() ?? "";
        }

        foreach (var prop in newPackages.RootElement.EnumerateObject())
            merged[prop.Name] = prop.Value.GetString() ?? "";

        // Write back
        var result = new Dictionary<string, object>
        {
            ["dependencies"] = merged
        };
        string output = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        await _fs.File.WriteAllTextAsync(manifestPath, output, cancellationToken);
        _logger.LogInformation("Updated manifest.json with {Count} packages at {Path}", merged.Count, manifestPath);
    }

    public async Task<string> ListPackagesAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        string manifestPath = _fs.Path.Combine(ValidateProjectRoot(projectPath, requireExists: false), "Packages", "manifest.json");
        var dependencies = await ReadPackageDependenciesAsync(manifestPath, cancellationToken);
        return JsonSerializer.Serialize(new ToolResultEnvelope<object>
        {
            Success = true,
            Message = $"Listed {dependencies.Count} package dependencies.",
            Data = new { manifest = MakeProjectRelativePath(projectPath, manifestPath), dependencies },
        });
    }

    public async Task<string> RemovePackagesAsync(string projectPath, IReadOnlyList<string> packageIds, CancellationToken cancellationToken = default)
    {
        string manifestPath = _fs.Path.Combine(ValidateProjectRoot(projectPath, requireExists: false), "Packages", "manifest.json");
        var dependencies = await ReadPackageDependenciesAsync(manifestPath, cancellationToken);
        var removed = new List<string>();
        foreach (string packageId in packageIds.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            if (dependencies.Remove(packageId.Trim()))
                removed.Add(packageId.Trim());
        }

        await WritePackageDependenciesAsync(manifestPath, dependencies, cancellationToken);
        return JsonSerializer.Serialize(new ToolResultEnvelope<object>
        {
            Success = true,
            Message = $"Removed {removed.Count} package dependencies.",
            Data = new { removed, dependencies },
        });
    }

    public async Task<string> VerifyPackageHealthAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        string projectRoot = ValidateProjectRoot(projectPath, requireExists: false);
        string manifestPath = _fs.Path.Combine(projectRoot, "Packages", "manifest.json");
        string lockPath = _fs.Path.Combine(projectRoot, "Packages", "packages-lock.json");
        var warnings = new List<UnityMcpError>();
        var errors = new List<UnityMcpError>();

        Dictionary<string, string> dependencies;
        try
        {
            dependencies = await ReadPackageDependenciesAsync(manifestPath, cancellationToken);
        }
        catch (Exception ex)
        {
            errors.Add(new UnityMcpError { Category = UnityMcpErrorCategory.Validation, Code = "Packages.ManifestInvalid", Message = ex.Message });
            dependencies = new Dictionary<string, string>();
        }

        if (!_fs.File.Exists(lockPath))
            warnings.Add(new UnityMcpError { Category = UnityMcpErrorCategory.Validation, Code = "Packages.LockMissing", Message = "Packages/packages-lock.json is missing; run Unity package resolution to create it." });

        foreach (var dependency in dependencies)
        {
            if (string.IsNullOrWhiteSpace(dependency.Value))
                warnings.Add(new UnityMcpError { Category = UnityMcpErrorCategory.Validation, Code = "Packages.EmptyVersion", Message = $"Package '{dependency.Key}' has an empty version." });
        }

        return JsonSerializer.Serialize(new ImportValidationResult
        {
            Success = errors.Count == 0,
            ErrorCount = errors.Count,
            WarningCount = warnings.Count,
            Errors = errors,
            Warnings = warnings,
            Message = errors.Count == 0 ? "Package health verification completed." : "Package health verification failed.",
        });
    }

    // -----------------------------------------------------------------------
    // MCP-Unity contract tools (return JSON)
    // -----------------------------------------------------------------------

    private static readonly IReadOnlyDictionary<string, string> DefaultPackageVersions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["com.unity.render-pipelines.universal"] = "14.0.11",
        ["com.unity.render-pipelines.core"] = "14.0.11",
        ["com.unity.textmeshpro"] = "3.0.6",
    };

    public async Task<string> InstallPackagesAsync(string projectPath, IReadOnlyList<string> packageIds, CancellationToken cancellationToken = default)
    {
        var installed = new List<string>();
        try
        {
            string manifestPath = _fs.Path.Combine(projectPath, "Packages", "manifest.json");
            string? dir = _fs.Path.GetDirectoryName(manifestPath);
            if (!string.IsNullOrEmpty(dir) && !_fs.Directory.Exists(dir))
                _fs.Directory.CreateDirectory(dir);

            JsonDocument existingDoc;
            if (_fs.File.Exists(manifestPath))
            {
                string existing = await _fs.File.ReadAllTextAsync(manifestPath, cancellationToken);
                existingDoc = JsonDocument.Parse(existing);
            }
            else
            {
                existingDoc = JsonDocument.Parse("{\"dependencies\":{}}");
            }

            var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (existingDoc.RootElement.TryGetProperty("dependencies", out var deps))
            {
                foreach (var prop in deps.EnumerateObject())
                    merged[prop.Name] = prop.Value.GetString() ?? "";
            }

            foreach (string id in packageIds)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                string version = DefaultPackageVersions.TryGetValue(id, out var v) ? v : "1.0.0";
                merged[id] = version;
                installed.Add(id);
            }

            var manifestObject = new Dictionary<string, object> { ["dependencies"] = merged };
            string output = JsonSerializer.Serialize(manifestObject, new JsonSerializerOptions { WriteIndented = true });
            await _fs.File.WriteAllTextAsync(manifestPath, output, cancellationToken);
            _logger.LogInformation("Installed {Count} packages at {Path}", installed.Count, manifestPath);

            var result = new InstallPackagesResult
            {
                Success = true,
                Installed = installed,
                Message = null,
            };

            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "InstallPackages failed for {Path}", projectPath);
            var error = new UnityMcpError
            {
                Category = UnityMcpErrorCategory.Internal,
                Code = "InstallPackages.Failure",
                Message = ex.Message,
            };

            var result = new InstallPackagesResult
            {
                Success = false,
                Installed = Array.Empty<string>(),
                Message = ex.Message,
                Errors = new[] { error },
            };

            return JsonSerializer.Serialize(result);
        }
    }

    public async Task<string> CreateDefaultSceneAsync(string projectPath, string sceneName, CancellationToken cancellationToken = default)
    {
        try
        {
            string scenesDir = _fs.Path.Combine(projectPath, "Assets", "Scenes");
            string prefabsDir = _fs.Path.Combine(projectPath, "Assets", "Prefabs");
            _fs.Directory.CreateDirectory(scenesDir);
            _fs.Directory.CreateDirectory(prefabsDir);
            await _metaWriter.WriteFolderMetaAsync(scenesDir, ct: cancellationToken);
            await _metaWriter.WriteFolderMetaAsync(prefabsDir, ct: cancellationToken);

            string scenePath = _fs.Path.Combine(scenesDir, sceneName + ".unity");
            string prefabPath = _fs.Path.Combine(prefabsDir, "Ground.prefab");

            var camera = CreateDefaultCamera();
            var light = CreateDefaultLight();
            var ground = CreateDefaultGroundPlane();

            UnityYamlWriter.ResetFileIdCounter();
            string sceneYaml = UnityYamlWriter.WriteScene(new[] { camera, light, ground });
            await _fs.File.WriteAllTextAsync(scenePath, sceneYaml, cancellationToken);
            await _metaWriter.WriteDefaultMetaAsync(scenePath, ct: cancellationToken);

            UnityYamlWriter.ResetFileIdCounter();
            string prefabYaml = UnityYamlWriter.WritePrefab(ground);
            await _fs.File.WriteAllTextAsync(prefabPath, prefabYaml, cancellationToken);
            await _metaWriter.WriteDefaultMetaAsync(prefabPath, ct: cancellationToken);

            string sceneRelative = "Assets/Scenes/" + sceneName + ".unity";
            string prefabRelative = "Assets/Prefabs/Ground.prefab";

            var result = new DefaultSceneResult
            {
                Success = true,
                ScenePath = sceneRelative,
                PrefabPath = prefabRelative,
                Message = null,
            };

            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CreateDefaultScene failed for {Path}", projectPath);
            var error = new UnityMcpError
            {
                Category = UnityMcpErrorCategory.Internal,
                Code = "CreateDefaultScene.Failure",
                Message = ex.Message,
            };

            var result = new DefaultSceneResult
            {
                Success = false,
                ScenePath = null,
                PrefabPath = null,
                Message = ex.Message,
                Errors = new[] { error },
            };

            return JsonSerializer.Serialize(result);
        }
    }

    public async Task<string> ConfigureUrpAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        try
        {
            string projectSettingsPath = _fs.Path.Combine(projectPath, "ProjectSettings", "ProjectSettings.asset");
            string tagManagerPath = _fs.Path.Combine(projectPath, "ProjectSettings", "TagManager.asset");
            string graphicsPath = _fs.Path.Combine(projectPath, "ProjectSettings", "GraphicsSettings.asset");

            if (_fs.File.Exists(projectSettingsPath))
            {
                string content = await _fs.File.ReadAllTextAsync(projectSettingsPath, cancellationToken);
                content = System.Text.RegularExpressions.Regex.Replace(content, @"m_ActiveColorSpace:\s*\d+", "m_ActiveColorSpace: 1");
                await _fs.File.WriteAllTextAsync(projectSettingsPath, content, cancellationToken);
            }

            if (_fs.File.Exists(tagManagerPath))
            {
                string content = await _fs.File.ReadAllTextAsync(tagManagerPath, cancellationToken);
                if (!content.Contains("Generated"))
                    content = System.Text.RegularExpressions.Regex.Replace(content, @"(  m_Tags:\s*\n)", "$1  - Generated\n  - AutoSetup\n");
                if (!content.Contains("CustomLayer1"))
                    content = System.Text.RegularExpressions.Regex.Replace(content, @"(  m_Layers:\s*\n(?:  - .*\n){8})", "$1  - CustomLayer1\n  - CustomLayer2\n");
                await _fs.File.WriteAllTextAsync(tagManagerPath, content, cancellationToken);
            }

            string? rpGuid = FindFirstRenderPipelineAssetGuid(projectPath);
            if (!string.IsNullOrEmpty(rpGuid) && _fs.File.Exists(graphicsPath))
            {
                string content = await _fs.File.ReadAllTextAsync(graphicsPath, cancellationToken);
                if (content.Contains("m_DefaultRenderPipeline"))
                    content = System.Text.RegularExpressions.Regex.Replace(content, @"(m_DefaultRenderPipeline:\s*\{[^}]*})", $"m_DefaultRenderPipeline: {{fileID: 11400000, guid: {rpGuid}, type: 2}}");
                else
                    content = content.Replace("SerializedShader:\n", "SerializedShader:\nm_DefaultRenderPipeline: {fileID: 11400000, guid: " + rpGuid + ", type: 2}\n");
                await _fs.File.WriteAllTextAsync(graphicsPath, content, cancellationToken);
            }
            else if (string.IsNullOrEmpty(rpGuid))
            {
                var error = new UnityMcpError
                {
                    Category = UnityMcpErrorCategory.Validation,
                    Code = "ConfigureUrp.MissingRenderPipelineAsset",
                    Message = "No RenderPipelineAsset found in project. Add URP package and create or import a pipeline asset.",
                };

                var noAssetResult = new UrpConfigurationResult
                {
                    Success = false,
                    Message = error.Message,
                    Errors = new[] { error },
                };

                return JsonSerializer.Serialize(noAssetResult);
            }

            var result = new UrpConfigurationResult
            {
                Success = true,
                Message = null,
            };

            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ConfigureUrp failed for {Path}", projectPath);
            var error = new UnityMcpError
            {
                Category = UnityMcpErrorCategory.Internal,
                Code = "ConfigureUrp.Failure",
                Message = ex.Message,
            };

            var result = new UrpConfigurationResult
            {
                Success = false,
                Message = ex.Message,
                Errors = new[] { error },
            };

            return JsonSerializer.Serialize(result);
        }
    }

    public async Task<string> ConfigureProjectSettingsAsync(string projectPath, string settingsJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
            return SerializeFailure<object>("ProjectSettings.Empty", "settingsJson is required.", UnityMcpErrorCategory.Validation);

        string projectRoot = ValidateProjectRoot(projectPath, requireExists: false);
        string settingsDir = _fs.Path.Combine(projectRoot, "ProjectSettings");
        _fs.Directory.CreateDirectory(settingsDir);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(settingsJson);
        }
        catch (Exception ex)
        {
            return SerializeFailure<object>("ProjectSettings.InvalidJson", $"Project settings JSON could not be parsed: {ex.Message}", UnityMcpErrorCategory.Validation);
        }

        string sidecarPath = _fs.Path.Combine(settingsDir, "McpProjectSettings.json");
        await _fs.File.WriteAllTextAsync(sidecarPath, JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);

        bool hasTags = document.RootElement.TryGetProperty("tags", out var tags);
        bool hasLayers = document.RootElement.TryGetProperty("layers", out var layers);
        if (hasTags || hasLayers)
        {
            string tagManagerPath = _fs.Path.Combine(settingsDir, "TagManager.asset");
            string tagManager = _fs.File.Exists(tagManagerPath) ? await _fs.File.ReadAllTextAsync(tagManagerPath, cancellationToken) : "TagManager:\n  m_Tags:\n  m_Layers:\n";
            if (hasTags && tags.ValueKind == JsonValueKind.Array)
            {
                var renderedTags = tags.EnumerateArray().Where(t => t.ValueKind == JsonValueKind.String).Select(t => $"  - {EscapeYamlScalar(t.GetString() ?? string.Empty)}");
                tagManager = Regex.Replace(tagManager, @"(?s)  m_Tags:\s*(?:\n  - .*)*", "  m_Tags:\n" + string.Join("\n", renderedTags));
            }
            if (hasLayers && layers.ValueKind == JsonValueKind.Array)
            {
                var renderedLayers = layers.EnumerateArray().Where(t => t.ValueKind == JsonValueKind.String).Select(t => $"  - {EscapeYamlScalar(t.GetString() ?? string.Empty)}");
                tagManager = Regex.Replace(tagManager, @"(?s)  m_Layers:\s*(?:\n  - .*)*", "  m_Layers:\n" + string.Join("\n", renderedLayers));
            }
            await _fs.File.WriteAllTextAsync(tagManagerPath, tagManager, cancellationToken);
        }

        return JsonSerializer.Serialize(new ToolResultEnvelope<object>
        {
            Success = true,
            Message = "Project settings sidecar updated.",
            Data = new { path = MakeProjectRelativePath(projectRoot, sidecarPath) },
        });
    }

    public async Task<string> ConfigureBuildProfileAsync(string projectPath, string profileJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileJson))
            return SerializeFailure<object>("BuildProfile.Empty", "profileJson is required.", UnityMcpErrorCategory.Validation);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(profileJson);
        }
        catch (Exception ex)
        {
            return SerializeFailure<object>("BuildProfile.InvalidJson", $"Build profile JSON could not be parsed: {ex.Message}", UnityMcpErrorCategory.Validation);
        }

        string name = document.RootElement.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
            ? SanitizeAssetName(nameElement.GetString() ?? "BuildProfile")
            : "BuildProfile";
        string profilePath = ResolvePath(projectPath, $"Assets/Settings/BuildProfiles/{name}.buildprofile.json");
        EnsureDirectoryExists(profilePath);
        await _fs.File.WriteAllTextAsync(profilePath, JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(profilePath, ct: cancellationToken);

        return JsonSerializer.Serialize(new ToolResultEnvelope<object>
        {
            Success = true,
            Message = "Build profile written.",
            Data = new { path = MakeProjectRelativePath(projectPath, profilePath) },
        });
    }

    public Task<string> QueryDocumentationAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult(SerializeFailure<object>("Documentation.EmptyQuery", "query is required.", UnityMcpErrorCategory.Validation));

        string repoRoot = FindRepositoryRoot();
        string[] roots =
        [
            repoRoot,
            _fs.Path.Combine(repoRoot, "Docs"),
            _fs.Path.Combine(repoRoot, "Skills"),
        ];
        string[] tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var results = new List<object>();
        foreach (string root in roots.Where(_fs.Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (string file in _fs.Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                if (results.Count >= Math.Clamp(maxResults, 1, 25))
                    break;
                string text = _fs.File.ReadAllText(file);
                int score = tokens.Count(token => text.Contains(token, StringComparison.OrdinalIgnoreCase) || _fs.Path.GetFileName(file).Contains(token, StringComparison.OrdinalIgnoreCase));
                if (score == 0)
                    continue;
                int firstIndex = tokens.Select(token => text.IndexOf(token, StringComparison.OrdinalIgnoreCase)).Where(index => index >= 0).DefaultIfEmpty(0).Min();
                int start = Math.Max(0, firstIndex - 160);
                int length = Math.Min(360, text.Length - start);
                results.Add(new
                {
                    path = MakeProjectRelativePath(repoRoot, file),
                    score,
                    snippet = text.Substring(start, length).Replace("\r", string.Empty).Replace("\n", " "),
                });
            }
        }

        return Task.FromResult(JsonSerializer.Serialize(new ToolResultEnvelope<object>
        {
            Success = true,
            Message = $"Documentation search completed with {results.Count} results.",
            Data = new { query, results },
        }));
    }

    private string? FindFirstRenderPipelineAssetGuid(string projectPath)
    {
        string assetsPath = _fs.Path.Combine(projectPath, "Assets");
        string packagesPath = _fs.Path.Combine(projectPath, "Packages");
        foreach (string root in new[] { assetsPath, packagesPath })
        {
            if (!_fs.Directory.Exists(root)) continue;
            foreach (string file in _fs.Directory.EnumerateFiles(root, "*.asset", SearchOption.AllDirectories))
            {
                try
                {
                    string content = _fs.File.ReadAllText(file);
                    if (content.IndexOf("RenderPipelineAsset", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        string metaPath = file + ".meta";
                        if (_fs.File.Exists(metaPath))
                        {
                            string meta = _fs.File.ReadAllText(metaPath);
                            var m = System.Text.RegularExpressions.Regex.Match(meta, @"guid:\s*([a-fA-F0-9]{32})");
                            if (m.Success) return m.Groups[1].Value;
                        }
                    }
                }
                catch { /* skip */ }
            }
        }
        return null;
    }

    public async Task<string> ValidateImportAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        _projectPath = projectPath;
        if (_editorExecutor is null)
        {
            var unavailable = new ImportValidationResult
            {
                Success = false,
                ErrorCount = 1,
                WarningCount = 0,
                Errors =
                [
                    new UnityMcpError
                    {
                        Category = UnityMcpErrorCategory.ExternalTool,
                        Code = "ValidateImport.EditorUnavailable",
                        Message = "Unity Editor executor is not registered.",
                    }
                ],
                Warnings = Array.Empty<UnityMcpError>(),
                Message = "Unity Editor is required for authoritative import and compilation validation. Set UNITY_EDITOR_PATH.",
            };
            return JsonSerializer.Serialize(unavailable);
        }

        if (_editorExecutor.FindUnityExecutable() is null
            && !_editorExecutor.TryGetLiveBridgeStatus(projectPath, out bool connected, out _)
            && !connected)
        {
            var unavailable = new ImportValidationResult
            {
                Success = false,
                ErrorCount = 1,
                WarningCount = 0,
                Errors =
                [
                    new UnityMcpError
                    {
                        Category = UnityMcpErrorCategory.ExternalTool,
                        Code = "ValidateImport.EditorUnavailable",
                        Message = "Unity Editor executable not found. Set UNITY_EDITOR_PATH environment variable.",
                    }
                ],
                Warnings = Array.Empty<UnityMcpError>(),
                Message = "Unity Editor is required for authoritative import and compilation validation. Set UNITY_EDITOR_PATH.",
            };
            return JsonSerializer.Serialize(unavailable);
        }

        return await _editorExecutor.ExecuteAsync(projectPath, "validate_import", null, cancellationToken).ConfigureAwait(false);
    }

    // -----------------------------------------------------------------------
    // UI authoring (Phase 1 foundations)
    // -----------------------------------------------------------------------

    public async Task<string> CreateUiCanvasAsync(string projectPath, string fileName, CancellationToken cancellationToken = default)
    {
        if (IsEditorAvailable(projectPath))
        {
            return await _editorExecutor!.ExecuteAsync(projectPath, "native.create_ui",
                new Dictionary<string, object?>
                {
                    ["fileName"] = ToProjectRelative(projectPath, fileName),
                }, cancellationToken).ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path is required.", nameof(projectPath));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        string resolvedPath = ResolvePath(projectPath, fileName);
        EnsureDirectoryExists(resolvedPath);

        bool isScene = resolvedPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
        bool exists = _fs.File.Exists(resolvedPath);

        // If this is a new scene, start from a basic camera + light scene.
        if (isScene && !exists)
        {
            var gameObjects = new List<GameObjectDef>
            {
                CreateDefaultCamera(),
                CreateDefaultLight(),
            };
            UnityYamlWriter.ResetFileIdCounter();
            string yaml = UnityYamlWriter.WriteScene(gameObjects);
            await _fs.File.WriteAllTextAsync(resolvedPath, yaml, cancellationToken);
        }

        // Build Canvas and EventSystem GameObjects.
        var canvas = new GameObjectDef
        {
            Name = "Canvas",
            Tag = "Untagged",
            Layer = 5, // UI layer
            Position = new Vector3Def(0, 0, 0),
            Scale = new Vector3Def(1920, 1080, 1),
            Components =
            [
                new ComponentDef(UnityYamlWriter.ClassId_Canvas),
                new ComponentDef(UnityYamlWriter.ClassId_CanvasRenderer),
            ],
        };

        var eventSystem = new GameObjectDef
        {
            Name = "EventSystem",
            Tag = "Untagged",
            Layer = 5,
            Position = new Vector3Def(0, 0, 0),
            Components =
            [
                new ComponentDef(UnityYamlWriter.ClassId_MonoBehaviour)
                {
                    Properties = { ["mcpComponentHint"] = "EventSystem" }
                },
            ],
        };

        UnityYamlWriter.ResetFileIdCounter();

        if (isScene)
        {
            // Append Canvas and EventSystem fragments to the existing scene file.
            string canvasFragment = UnityYamlWriter.WriteGameObjectFragment(canvas);
            string esFragment = UnityYamlWriter.WriteGameObjectFragment(eventSystem);
            await _fs.File.AppendAllTextAsync(resolvedPath, canvasFragment + esFragment, cancellationToken);
        }
        else
        {
            // Treat as prefab: create a prefab containing only the Canvas GameObject.
            string prefabYaml = UnityYamlWriter.WritePrefab(canvas);
            await _fs.File.WriteAllTextAsync(resolvedPath, prefabYaml, cancellationToken);
        }

        await _metaWriter.WriteDefaultMetaAsync(resolvedPath, ct: cancellationToken);

        string relativePath = MakeProjectRelativePath(projectPath, resolvedPath);
        var result = new
        {
            success = true,
            path = relativePath,
            message = (string?)null,
        };

        return JsonSerializer.Serialize(result);
    }

    public async Task<string> CreateUiLayoutAsync(string projectPath, string fileName, string layoutJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path is required.", nameof(projectPath));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(layoutJson))
            throw new ArgumentException("Layout JSON is required.", nameof(layoutJson));

        string resolvedPath = ResolvePath(projectPath, fileName);
        if (!_fs.File.Exists(resolvedPath))
            throw new FileNotFoundException($"Target scene or prefab not found: {resolvedPath}");

        UiLayout layout;
        try
        {
            layout = JsonSerializer.Deserialize<UiLayout>(layoutJson, JsonOpts)
                     ?? throw new InvalidOperationException("Deserialized UiLayout was null.");
        }
        catch (Exception ex)
        {
            var error = new UnityMcpError
            {
                Category = UnityMcpErrorCategory.Validation,
                Code = "UiLayout.InvalidJson",
                Message = $"Failed to parse UiLayout JSON: {ex.Message}",
            };

            var errorResult = new ImportValidationResult
            {
                Success = false,
                ErrorCount = 1,
                WarningCount = 0,
                Errors = new[] { error },
                Warnings = Array.Empty<UnityMcpError>(),
                Message = "UI layout JSON could not be parsed.",
            };

            return JsonSerializer.Serialize(errorResult);
        }

        var uiObjects = new List<GameObjectDef>();
        foreach (var panel in layout.Panels)
        {
            BuildPanelHierarchy(panel, uiObjects);
        }

        if (uiObjects.Count == 0)
        {
            var warning = new UnityMcpError
            {
                Category = UnityMcpErrorCategory.Validation,
                Code = "UiLayout.Empty",
                Message = "UiLayout contains no panels or controls.",
            };

            var warnResult = new ImportValidationResult
            {
                Success = true,
                ErrorCount = 0,
                WarningCount = 1,
                Errors = Array.Empty<UnityMcpError>(),
                Warnings = new[] { warning },
                Message = "UiLayout was empty; no GameObjects were added.",
            };

            return JsonSerializer.Serialize(warnResult);
        }

        UnityYamlWriter.ResetFileIdCounter();
        var sb = new System.Text.StringBuilder();
        foreach (var go in uiObjects)
        {
            sb.Append(UnityYamlWriter.WriteGameObjectFragment(go));
        }

        await _fs.File.AppendAllTextAsync(resolvedPath, sb.ToString(), cancellationToken);

        var successResult = new ImportValidationResult
        {
            Success = true,
            ErrorCount = 0,
            WarningCount = 0,
            Errors = Array.Empty<UnityMcpError>(),
            Warnings = Array.Empty<UnityMcpError>(),
            Message = "UI layout applied successfully.",
        };

        return JsonSerializer.Serialize(successResult);
    }

    // -----------------------------------------------------------------------
    // Navigation (Phase 2)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Writes NavMesh configuration to Assets/Settings/NavMeshConfig.json (or project-relative path).
    /// configJson must deserialize to NavMeshConfig; returns JSON: success, path, message, errors.
    /// </summary>
    public async Task<string> ConfigureNavmeshAsync(string projectPath, string configJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path is required.", nameof(projectPath));
        if (string.IsNullOrWhiteSpace(configJson))
            throw new ArgumentException("Config JSON is required.", nameof(configJson));

        NavMeshConfig config;
        try
        {
            config = JsonSerializer.Deserialize<NavMeshConfig>(configJson, JsonOpts)
                     ?? throw new InvalidOperationException("Deserialized NavMeshConfig was null.");
        }
        catch (Exception ex)
        {
            var error = new UnityMcpError
            {
                Category = UnityMcpErrorCategory.Validation,
                Code = "NavMeshConfig.InvalidJson",
                Message = $"Failed to parse NavMesh config: {ex.Message}",
            };
            var result = new { success = false, path = (string?)null, message = error.Message, errors = new[] { error } };
            return JsonSerializer.Serialize(result);
        }

        string fileName = "Assets/Settings/NavMeshConfig.json";
        string resolvedPath = ResolvePath(projectPath, fileName);
        EnsureDirectoryExists(resolvedPath);

        string jsonToWrite = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        await _fs.File.WriteAllTextAsync(resolvedPath, jsonToWrite, cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(resolvedPath, ct: cancellationToken);
        string nativePath = _fs.Path.ChangeExtension(resolvedPath, ".asset");
        await _fs.File.WriteAllTextAsync(nativePath, "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_Name: NavMeshConfig\n  # mcp_native_hint: NavMesh settings ScriptableObject companion\n", cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(nativePath, ct: cancellationToken);

        string relativePath = MakeProjectRelativePath(projectPath, resolvedPath);
        var successResult = new { success = true, path = relativePath, native_path = MakeProjectRelativePath(projectPath, nativePath), message = (string?)null, errors = Array.Empty<UnityMcpError>() };
        return JsonSerializer.Serialize(successResult);
    }

    /// <summary>
    /// Creates a waypoint graph asset from graphJson (WaypointGraph). Writes JSON to fileName (e.g. Assets/Data/PatrolRoute.waypoints.json).
    /// Validates that every edge references existing node ids; returns ImportValidationResult-style JSON on failure.
    /// </summary>
    public async Task<string> CreateWaypointGraphAsync(string projectPath, string fileName, string graphJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path is required.", nameof(projectPath));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(graphJson))
            throw new ArgumentException("Graph JSON is required.", nameof(graphJson));

        WaypointGraph graph;
        try
        {
            graph = JsonSerializer.Deserialize<WaypointGraph>(graphJson, JsonOpts)
                    ?? throw new InvalidOperationException("Deserialized WaypointGraph was null.");
        }
        catch (Exception ex)
        {
            var error = new UnityMcpError
            {
                Category = UnityMcpErrorCategory.Validation,
                Code = "WaypointGraph.InvalidJson",
                Message = $"Failed to parse waypoint graph: {ex.Message}",
            };
            var validationResult = new ImportValidationResult
            {
                Success = false,
                ErrorCount = 1,
                WarningCount = 0,
                Errors = new[] { error },
                Warnings = Array.Empty<UnityMcpError>(),
                Message = "Waypoint graph JSON could not be parsed.",
            };
            return JsonSerializer.Serialize(validationResult);
        }

        var nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in graph.Nodes)
        {
            if (string.IsNullOrWhiteSpace(n.Id))
                continue;
            nodeIds.Add(n.Id.Trim());
        }

        var errors = new List<UnityMcpError>();
        foreach (var e in graph.Edges)
        {
            if (string.IsNullOrWhiteSpace(e.From) || string.IsNullOrWhiteSpace(e.To))
            {
                errors.Add(new UnityMcpError
                {
                    Category = UnityMcpErrorCategory.Validation,
                    Code = "WaypointGraph.InvalidEdge",
                    Message = "Edge has null or empty from/to.",
                });
                continue;
            }
            if (!nodeIds.Contains(e.From.Trim()))
                errors.Add(new UnityMcpError
                {
                    Category = UnityMcpErrorCategory.Validation,
                    Code = "WaypointGraph.InvalidEdge",
                    Message = $"Edge references missing node id: {e.From}",
                });
            if (!nodeIds.Contains(e.To.Trim()))
                errors.Add(new UnityMcpError
                {
                    Category = UnityMcpErrorCategory.Validation,
                    Code = "WaypointGraph.InvalidEdge",
                    Message = $"Edge references missing node id: {e.To}",
                });
        }

        if (errors.Count > 0)
        {
            var failResult = new ImportValidationResult
            {
                Success = false,
                ErrorCount = errors.Count,
                WarningCount = 0,
                Errors = errors,
                Warnings = Array.Empty<UnityMcpError>(),
                Message = "Waypoint graph validation failed.",
            };
            return JsonSerializer.Serialize(failResult);
        }

        string resolvedPath = ResolvePath(projectPath, fileName);
        EnsureDirectoryExists(resolvedPath);

        string jsonToWrite = JsonSerializer.Serialize(graph, new JsonSerializerOptions { WriteIndented = true });
        await _fs.File.WriteAllTextAsync(resolvedPath, jsonToWrite, cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(resolvedPath, ct: cancellationToken);

        string relativePath = MakeProjectRelativePath(projectPath, resolvedPath);
        return JsonSerializer.Serialize(new { success = true, path = relativePath, message = "Waypoint graph created successfully.", errors = Array.Empty<UnityMcpError>() });
    }

    // -----------------------------------------------------------------------
    // Modern Input System (Phase 2)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates an Input Actions asset (.inputactions) from actionsJson. Writes JSON to fileName.
    /// </summary>
    public async Task<string> CreateInputActionsAsync(string projectPath, string fileName, string actionsJson, CancellationToken cancellationToken = default)
    {
        if (IsEditorAvailable(projectPath))
        {
            return await _editorExecutor!.ExecuteAsync(projectPath, "native.create_input_actions",
                new Dictionary<string, object?>
                {
                    ["fileName"] = ToProjectRelative(projectPath, fileName),
                    ["content"] = actionsJson,
                }, cancellationToken).ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path is required.", nameof(projectPath));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(actionsJson))
            throw new ArgumentException("Actions JSON is required.", nameof(actionsJson));

        InputActionsAsset asset;
        try
        {
            asset = JsonSerializer.Deserialize<InputActionsAsset>(actionsJson, JsonOpts)
                    ?? throw new InvalidOperationException("Deserialized InputActionsAsset was null.");
        }
        catch (Exception ex)
        {
            var error = new UnityMcpError
            {
                Category = UnityMcpErrorCategory.Validation,
                Code = "InputActions.InvalidJson",
                Message = $"Failed to parse input actions: {ex.Message}",
            };
            var result = new ImportValidationResult
            {
                Success = false,
                ErrorCount = 1,
                WarningCount = 0,
                Errors = new[] { error },
                Warnings = Array.Empty<UnityMcpError>(),
                Message = "Input actions JSON could not be parsed.",
            };
            return JsonSerializer.Serialize(result);
        }

        string resolvedPath = ResolvePath(projectPath, fileName);
        EnsureDirectoryExists(resolvedPath);

        string jsonToWrite = JsonSerializer.Serialize(asset, new JsonSerializerOptions { WriteIndented = true });
        await _fs.File.WriteAllTextAsync(resolvedPath, jsonToWrite, cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(resolvedPath, ct: cancellationToken);

        string relativePath = MakeProjectRelativePath(projectPath, resolvedPath);
        return JsonSerializer.Serialize(new { success = true, path = relativePath, message = "Input actions asset created successfully.", errors = Array.Empty<UnityMcpError>() });
    }

    // -----------------------------------------------------------------------
    // Basic Animation (Phase 2)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a basic Animator definition asset (JSON surrogate). Validates referenced clip paths exist under project.
    /// </summary>
    public async Task<string> CreateBasicAnimatorAsync(string projectPath, string fileName, string animatorJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path is required.", nameof(projectPath));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(animatorJson))
            throw new ArgumentException("Animator JSON is required.", nameof(animatorJson));

        if (IsEditorAvailable(projectPath))
        {
            var args = MergeJsonArgs(animatorJson, new Dictionary<string, object?>
            {
                ["fileName"] = ToProjectRelative(projectPath, fileName),
            });
            return await _editorExecutor!.ExecuteAsync(projectPath, "native.create_animator", args, cancellationToken).ConfigureAwait(false);
        }

        BasicAnimatorDefinition def;
        try
        {
            def = JsonSerializer.Deserialize<BasicAnimatorDefinition>(animatorJson, JsonOpts)
                  ?? throw new InvalidOperationException("Deserialized BasicAnimatorDefinition was null.");
        }
        catch (Exception ex)
        {
            var error = new UnityMcpError
            {
                Category = UnityMcpErrorCategory.Validation,
                Code = "BasicAnimator.InvalidJson",
                Message = $"Failed to parse animator definition: {ex.Message}",
            };
            var result = new ImportValidationResult
            {
                Success = false,
                ErrorCount = 1,
                WarningCount = 0,
                Errors = new[] { error },
                Warnings = Array.Empty<UnityMcpError>(),
                Message = "Animator JSON could not be parsed.",
            };
            return JsonSerializer.Serialize(result);
        }

        var warnings = new List<UnityMcpError>();
        foreach (var state in def.States)
        {
            if (string.IsNullOrWhiteSpace(state.Clip))
                continue;
            string clipResolved = ResolvePath(projectPath, state.Clip.Trim());
            if (!_fs.File.Exists(clipResolved))
            {
                warnings.Add(new UnityMcpError
                {
                    Category = UnityMcpErrorCategory.Validation,
                    Code = "BasicAnimator.MissingClip",
                    Message = $"Referenced clip not found: {state.Clip}",
                });
            }
        }

        string resolvedPath = ResolvePath(projectPath, fileName);
        EnsureDirectoryExists(resolvedPath);

        string jsonToWrite = JsonSerializer.Serialize(def, new JsonSerializerOptions { WriteIndented = true });
        await _fs.File.WriteAllTextAsync(resolvedPath, jsonToWrite, cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(resolvedPath, ct: cancellationToken);
        string controllerPath = _fs.Path.ChangeExtension(resolvedPath, ".controller");
        await _fs.File.WriteAllTextAsync(controllerPath, "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!91 &9100000\nAnimatorController:\n  m_Name: " + EscapeYamlScalar(def.Name) + "\n  # mcp_native_hint: Generated from BasicAnimatorDefinition\n", cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(controllerPath, ct: cancellationToken);

        string relativePath = MakeProjectRelativePath(projectPath, resolvedPath);
        if (warnings.Count > 0)
        {
            var result = new ImportValidationResult
            {
                Success = true,
                ErrorCount = 0,
                WarningCount = warnings.Count,
                Errors = Array.Empty<UnityMcpError>(),
                Warnings = warnings,
                Message = "Animator definition created; some referenced clips are missing.",
            };
            return JsonSerializer.Serialize(new { success = true, path = relativePath, native_path = MakeProjectRelativePath(projectPath, controllerPath), message = result.Message, errors = Array.Empty<UnityMcpError>(), warnings });
        }
        return JsonSerializer.Serialize(new { success = true, path = relativePath, native_path = MakeProjectRelativePath(projectPath, controllerPath), message = "Animator definition created successfully.", errors = Array.Empty<UnityMcpError>(), warnings = Array.Empty<UnityMcpError>() });
    }

    /// <summary>
    /// Creates an advanced Animator definition asset (multi-layer, sub-state machines, blend trees).
    /// For Phase 3, this is a JSON surrogate; Unity-native controller generation can be added later.
    /// </summary>
    public async Task<string> CreateAdvancedAnimatorAsync(string projectPath, string fileName, string animatorJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path is required.", nameof(projectPath));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(animatorJson))
            throw new ArgumentException("Animator JSON is required.", nameof(animatorJson));

        AdvancedAnimatorWrapper wrapper;
        try
        {
            wrapper = JsonSerializer.Deserialize<AdvancedAnimatorWrapper>(animatorJson, JsonOpts)
                      ?? throw new InvalidOperationException("Deserialized advanced animator wrapper was null.");
        }
        catch (Exception ex)
        {
            var error = new UnityMcpError
            {
                Category = UnityMcpErrorCategory.Validation,
                Code = "AdvancedAnimator.InvalidJson",
                Message = $"Failed to parse advanced animator definition: {ex.Message}",
            };
            var result = new ImportValidationResult
            {
                Success = false,
                ErrorCount = 1,
                WarningCount = 0,
                Errors = new[] { error },
                Warnings = Array.Empty<UnityMcpError>(),
                Message = "Advanced animator JSON could not be parsed.",
            };
            return JsonSerializer.Serialize(result);
        }

        var errors = new List<UnityMcpError>();
        foreach (var layer in wrapper.Layers)
        {
            bool hasDefault = layer.States.Any(s => s.Name == layer.DefaultState);
            hasDefault |= layer.SubStateMachines.Any(sm => sm.Name == layer.DefaultState);
            if (!hasDefault)
            {
                errors.Add(new UnityMcpError
                {
                    Category = UnityMcpErrorCategory.Validation,
                    Code = "AdvancedAnimator.InvalidLayer",
                    Message = $"Layer '{layer.Name}' has defaultState '{layer.DefaultState}' which does not match any state or sub-state machine.",
                });
            }
        }

        if (errors.Count > 0)
        {
            var fail = new ImportValidationResult
            {
                Success = false,
                ErrorCount = errors.Count,
                WarningCount = 0,
                Errors = errors,
                Warnings = Array.Empty<UnityMcpError>(),
                Message = "Advanced animator validation failed.",
            };
            return JsonSerializer.Serialize(fail);
        }

        string resolvedPath = ResolvePath(projectPath, fileName);
        EnsureDirectoryExists(resolvedPath);

        // Persist original JSON as the authoritative surrogate.
        await _fs.File.WriteAllTextAsync(resolvedPath, animatorJson, cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(resolvedPath, ct: cancellationToken);
        string nativeControllerPath = _fs.Path.ChangeExtension(resolvedPath, ".controller");
        await _fs.File.WriteAllTextAsync(nativeControllerPath, "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!91 &9100000\nAnimatorController:\n  m_Name: AdvancedAnimator\n  # mcp_native_hint: Generated from advanced animator contract\n", cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(nativeControllerPath, ct: cancellationToken);

        string relativePath = MakeProjectRelativePath(projectPath, resolvedPath);
        return JsonSerializer.Serialize(new { success = true, path = relativePath, native_path = MakeProjectRelativePath(projectPath, nativeControllerPath), message = "Advanced animator definition created successfully.", errors = Array.Empty<UnityMcpError>() });
    }

    /// <summary>
    /// Creates a Timeline definition asset (JSON surrogate). Validates referenced clip/audio paths optionally.
    /// </summary>
    public async Task<string> CreateTimelineAsync(string projectPath, string fileName, string timelineJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path is required.", nameof(projectPath));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(timelineJson))
            throw new ArgumentException("Timeline JSON is required.", nameof(timelineJson));

        TimelineDefinition def;
        try
        {
            def = JsonSerializer.Deserialize<TimelineDefinition>(timelineJson, JsonOpts)
                  ?? throw new InvalidOperationException("Deserialized TimelineDefinition was null.");
        }
        catch (Exception ex)
        {
            var error = new UnityMcpError
            {
                Category = UnityMcpErrorCategory.Validation,
                Code = "Timeline.InvalidJson",
                Message = $"Failed to parse timeline definition: {ex.Message}",
            };
            var result = new ImportValidationResult
            {
                Success = false,
                ErrorCount = 1,
                WarningCount = 0,
                Errors = new[] { error },
                Warnings = Array.Empty<UnityMcpError>(),
                Message = "Timeline JSON could not be parsed.",
            };
            return JsonSerializer.Serialize(result);
        }

        var warnings = new List<UnityMcpError>();
        foreach (var track in def.Tracks)
        {
            foreach (var clip in track.Clips)
            {
                if (!string.IsNullOrWhiteSpace(clip.Clip))
                {
                    string clipPath = ResolvePath(projectPath, clip.Clip.Trim());
                    if (!_fs.File.Exists(clipPath))
                    {
                        warnings.Add(new UnityMcpError
                        {
                            Category = UnityMcpErrorCategory.Validation,
                            Code = "Timeline.MissingClip",
                            Message = $"Timeline '{def.Name}' references missing animation clip: {clip.Clip}",
                        });
                    }
                }
                if (!string.IsNullOrWhiteSpace(clip.Audio))
                {
                    string audioPath = ResolvePath(projectPath, clip.Audio.Trim());
                    if (!_fs.File.Exists(audioPath))
                    {
                        warnings.Add(new UnityMcpError
                        {
                            Category = UnityMcpErrorCategory.Validation,
                            Code = "Timeline.MissingAudio",
                            Message = $"Timeline '{def.Name}' references missing audio clip: {clip.Audio}",
                        });
                    }
                }
            }
        }

        string resolvedPath = ResolvePath(projectPath, fileName);
        EnsureDirectoryExists(resolvedPath);

        string jsonToWrite = JsonSerializer.Serialize(def, new JsonSerializerOptions { WriteIndented = true });
        await _fs.File.WriteAllTextAsync(resolvedPath, jsonToWrite, cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(resolvedPath, ct: cancellationToken);
        string playablePath = _fs.Path.ChangeExtension(resolvedPath, ".playable");
        await _fs.File.WriteAllTextAsync(playablePath, "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_Name: " + EscapeYamlScalar(def.Name) + "\n  # mcp_native_hint: Timeline companion asset\n", cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(playablePath, ct: cancellationToken);

        string relPath = MakeProjectRelativePath(projectPath, resolvedPath);
        if (warnings.Count > 0)
        {
            var result = new ImportValidationResult
            {
                Success = true,
                ErrorCount = 0,
                WarningCount = warnings.Count,
                Errors = Array.Empty<UnityMcpError>(),
                Warnings = warnings,
                Message = "Timeline created; some referenced clips are missing.",
            };
            return JsonSerializer.Serialize(new { success = true, path = relPath, native_path = MakeProjectRelativePath(projectPath, playablePath), message = result.Message, errors = Array.Empty<UnityMcpError>(), warnings });
        }

        return JsonSerializer.Serialize(new { success = true, path = relPath, native_path = MakeProjectRelativePath(projectPath, playablePath), message = "Timeline created successfully.", errors = Array.Empty<UnityMcpError>(), warnings = Array.Empty<UnityMcpError>() });
    }

    // -----------------------------------------------------------------------
    // Advanced Physics (Phase 3)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a physics setup (ragdoll/joints) asset (JSON surrogate). Validates that joints reference existing bones.
    /// </summary>
    public async Task<string> CreatePhysicsSetupAsync(string projectPath, string fileName, string physicsJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path is required.", nameof(projectPath));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(physicsJson))
            throw new ArgumentException("Physics JSON is required.", nameof(physicsJson));

        RagdollSetupContract setup;
        try
        {
            setup = JsonSerializer.Deserialize<RagdollSetupContract>(physicsJson, JsonOpts)
                    ?? throw new InvalidOperationException("Deserialized RagdollSetupContract was null.");
        }
        catch (Exception ex)
        {
            var error = new UnityMcpError
            {
                Category = UnityMcpErrorCategory.Validation,
                Code = "PhysicsSetup.InvalidJson",
                Message = $"Failed to parse physics setup: {ex.Message}",
            };

            var validationResult = new ImportValidationResult
            {
                Success = false,
                ErrorCount = 1,
                WarningCount = 0,
                Errors = new[] { error },
                Warnings = Array.Empty<UnityMcpError>(),
                Message = "Physics setup JSON could not be parsed.",
            };

            return JsonSerializer.Serialize(validationResult);
        }

        var boneNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var bone in setup.Bones)
        {
            if (string.IsNullOrWhiteSpace(bone.Name))
                continue;
            boneNames.Add(bone.Name.Trim());
        }

        var errors = new List<UnityMcpError>();
        foreach (var joint in setup.Joints)
        {
            if (!string.IsNullOrWhiteSpace(joint.Bone) && !boneNames.Contains(joint.Bone.Trim()))
            {
                errors.Add(new UnityMcpError
                {
                    Category = UnityMcpErrorCategory.Validation,
                    Code = "PhysicsSetup.InvalidReference",
                    Message = $"Joint '{joint.Name}' references missing bone '{joint.Bone}'.",
                });
            }

            if (!string.IsNullOrWhiteSpace(joint.ConnectedBodyName) && !boneNames.Contains(joint.ConnectedBodyName.Trim()))
            {
                errors.Add(new UnityMcpError
                {
                    Category = UnityMcpErrorCategory.Validation,
                    Code = "PhysicsSetup.InvalidReference",
                    Message = $"Joint '{joint.Name}' references missing connected body '{joint.ConnectedBodyName}'.",
                });
            }
        }

        if (errors.Count > 0)
        {
            var fail = new ImportValidationResult
            {
                Success = false,
                ErrorCount = errors.Count,
                WarningCount = 0,
                Errors = errors,
                Warnings = Array.Empty<UnityMcpError>(),
                Message = "Physics setup validation failed.",
            };

            return JsonSerializer.Serialize(fail);
        }

        string resolvedPath = ResolvePath(projectPath, fileName);
        EnsureDirectoryExists(resolvedPath);

        string jsonToWrite = JsonSerializer.Serialize(setup, new JsonSerializerOptions { WriteIndented = true });
        await _fs.File.WriteAllTextAsync(resolvedPath, jsonToWrite, cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(resolvedPath, ct: cancellationToken);
        string prefabPath = _fs.Path.ChangeExtension(resolvedPath, ".prefab");
        UnityYamlWriter.ResetFileIdCounter();
        await _fs.File.WriteAllTextAsync(prefabPath, UnityYamlWriter.WritePrefab(new GameObjectDef
        {
            Name = string.IsNullOrWhiteSpace(setup.Name) ? "PhysicsSetup" : setup.Name,
            Components = [new ComponentDef(UnityYamlWriter.ClassId_Rigidbody)]
        }), cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(prefabPath, ct: cancellationToken);

        string relativePath = MakeProjectRelativePath(projectPath, resolvedPath);
        return JsonSerializer.Serialize(new { success = true, path = relativePath, native_path = MakeProjectRelativePath(projectPath, prefabPath), message = "Physics setup created successfully.", errors = Array.Empty<UnityMcpError>() });
    }

    // -----------------------------------------------------------------------
    // VFX / particles (Phase 3)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a VFX particle asset from a ParticleEffectContract JSON definition.
    /// Performs basic semantic validation and writes a JSON surrogate asset plus .meta.
    /// </summary>
    public async Task<string> CreateVfxAssetAsync(string projectPath, string fileName, string vfxJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path is required.", nameof(projectPath));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(vfxJson))
            throw new ArgumentException("VFX JSON is required.", nameof(vfxJson));

        ParticleEffectContract effect;
        try
        {
            effect = JsonSerializer.Deserialize<ParticleEffectContract>(vfxJson, JsonOpts)
                     ?? throw new InvalidOperationException("Deserialized ParticleEffectContract was null.");
        }
        catch (Exception ex)
        {
            var error = new UnityMcpError
            {
                Category = UnityMcpErrorCategory.Validation,
                Code = "Vfx.InvalidJson",
                Message = $"Failed to parse VFX definition: {ex.Message}",
            };
            var result = new ImportValidationResult
            {
                Success = false,
                ErrorCount = 1,
                WarningCount = 0,
                Errors = new[] { error },
                Warnings = Array.Empty<UnityMcpError>(),
                Message = "VFX JSON could not be parsed.",
            };
            return JsonSerializer.Serialize(result);
        }

        var errors = new List<UnityMcpError>();

        if (effect.Duration < 0)
        {
            errors.Add(new UnityMcpError
            {
                Category = UnityMcpErrorCategory.Validation,
                Code = "Vfx.InvalidParameters",
                Message = "Duration must be non-negative.",
            });
        }

        if (effect.StartLifetime < 0)
        {
            errors.Add(new UnityMcpError
            {
                Category = UnityMcpErrorCategory.Validation,
                Code = "Vfx.InvalidParameters",
                Message = "StartLifetime must be non-negative.",
            });
        }

        if (effect.StartSpeed < 0)
        {
            errors.Add(new UnityMcpError
            {
                Category = UnityMcpErrorCategory.Validation,
                Code = "Vfx.InvalidParameters",
                Message = "StartSpeed must be non-negative.",
            });
        }

        if (effect.StartSize < 0)
        {
            errors.Add(new UnityMcpError
            {
                Category = UnityMcpErrorCategory.Validation,
                Code = "Vfx.InvalidParameters",
                Message = "StartSize must be non-negative.",
            });
        }

        if (effect.Emission is not null)
        {
            if (effect.Emission.RateOverTime < 0)
            {
                errors.Add(new UnityMcpError
                {
                    Category = UnityMcpErrorCategory.Validation,
                    Code = "Vfx.InvalidParameters",
                    Message = "Emission.rateOverTime must be non-negative.",
                });
            }

            foreach (var burst in effect.Emission.Bursts)
            {
                if (burst.Time < 0)
                {
                    errors.Add(new UnityMcpError
                    {
                        Category = UnityMcpErrorCategory.Validation,
                        Code = "Vfx.InvalidParameters",
                        Message = "Burst time must be non-negative.",
                    });
                }

                if (burst.Count < 0)
                {
                    errors.Add(new UnityMcpError
                    {
                        Category = UnityMcpErrorCategory.Validation,
                        Code = "Vfx.InvalidParameters",
                        Message = "Burst count must be non-negative.",
                    });
                }
            }
        }

        if (errors.Count > 0)
        {
            var validationResult = new ImportValidationResult
            {
                Success = false,
                ErrorCount = errors.Count,
                WarningCount = 0,
                Errors = errors,
                Warnings = Array.Empty<UnityMcpError>(),
                Message = "VFX definition validation failed.",
            };
            return JsonSerializer.Serialize(validationResult);
        }

        string resolvedPath = ResolvePath(projectPath, fileName);
        EnsureDirectoryExists(resolvedPath);

        string jsonToWrite = JsonSerializer.Serialize(effect, new JsonSerializerOptions { WriteIndented = true });
        await _fs.File.WriteAllTextAsync(resolvedPath, jsonToWrite, cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(resolvedPath, ct: cancellationToken);
        string vfxPrefabPath = _fs.Path.ChangeExtension(resolvedPath, ".prefab");
        UnityYamlWriter.ResetFileIdCounter();
        await _fs.File.WriteAllTextAsync(vfxPrefabPath, UnityYamlWriter.WritePrefab(new GameObjectDef
        {
            Name = string.IsNullOrWhiteSpace(effect.Name) ? "ParticleEffect" : effect.Name,
            Components =
            [
                new ComponentDef(UnityYamlWriter.ClassId_MonoBehaviour)
                {
                    Properties = { ["mcpComponentHint"] = "ParticleSystem" }
                }
            ],
        }), cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(vfxPrefabPath, ct: cancellationToken);

        string relativePath = MakeProjectRelativePath(projectPath, resolvedPath);
        return JsonSerializer.Serialize(new
        {
            success = true,
            path = relativePath,
            native_path = MakeProjectRelativePath(projectPath, vfxPrefabPath),
            message = "VFX asset created successfully.",
            errors = Array.Empty<UnityMcpError>(),
        });
    }

    public Task<string> ListCamerasAsync(string projectPath, string folderName = "Assets/Scenes", CancellationToken cancellationToken = default)
        => Task.FromResult(ListDomainObjects(projectPath, folderName, "Camera", "Camera"));

    public Task<string> ValidateCamerasAsync(string projectPath, string folderName = "Assets/Scenes", CancellationToken cancellationToken = default)
        => PreferEditorAsync(projectPath, "domain.validate_camera",
            new Dictionary<string, object?> { ["folderName"] = folderName },
            () => Task.FromResult(ValidateDomainObjects(projectPath, folderName, "Camera", requireAtLeastOne: true)),
            cancellationToken);

    public Task<string> ListLightsAsync(string projectPath, string folderName = "Assets/Scenes", CancellationToken cancellationToken = default)
        => Task.FromResult(ListDomainObjects(projectPath, folderName, "Light", "Light"));

    public Task<string> ValidateLightsAsync(string projectPath, string folderName = "Assets/Scenes", CancellationToken cancellationToken = default)
        => PreferEditorAsync(projectPath, "domain.validate_light",
            new Dictionary<string, object?> { ["folderName"] = folderName },
            () => Task.FromResult(ValidateDomainObjects(projectPath, folderName, "Light", requireAtLeastOne: false)),
            cancellationToken);

    public Task<string> ListPhysicsObjectsAsync(string projectPath, string folderName = "Assets/Scenes", CancellationToken cancellationToken = default)
        => Task.FromResult(ListDomainObjects(projectPath, folderName, "Physics", "Rigidbody", "BoxCollider", "SphereCollider", "CapsuleCollider"));

    public Task<string> ValidatePhysicsAsync(string projectPath, string folderName = "Assets/Scenes", CancellationToken cancellationToken = default)
        => TryEditorOrFileAsync(projectPath, "domain.validate_physics",
            new Dictionary<string, object?> { ["folderName"] = folderName },
            () => Task.FromResult(ValidateDomainObjects(projectPath, folderName, "Physics", requireAtLeastOne: false, "Rigidbody", "BoxCollider", "SphereCollider", "CapsuleCollider")),
            cancellationToken);

    public Task<string> AddSceneGameObjectAsync(string projectPath, string fileName, string parentPath, string objectName, CancellationToken cancellationToken = default)
        => PreferEditorAsync(projectPath, "scene.add",
            new Dictionary<string, object?>
            {
                ["fileName"] = ToProjectRelative(projectPath, fileName),
                ["parentPath"] = parentPath,
                ["objectName"] = objectName,
            },
            async () =>
            {
                // File fallback: append root GO (parentPath ignored in file-only mode).
                await AddGameObjectToSceneAsync(projectPath, fileName,
                    JsonSerializer.Serialize(new { name = objectName }), cancellationToken).ConfigureAwait(false);
                return JsonSerializer.Serialize(new ToolResultEnvelope<object>
                {
                    Success = true,
                    Message = "GameObject added (file-only; parentPath ignored).",
                    Data = new { objectName, parentPath },
                    Warnings =
                    [
                        new UnityMcpError
                        {
                            Category = UnityMcpErrorCategory.Contract,
                            Code = "Scene.ParentIgnoredFileOnly",
                            Message = "File-only mode cannot reparent; use Editor bridge for hierarchy-accurate adds.",
                        }
                    ],
                });
            },
            cancellationToken);

    public Task<string> ReparentSceneGameObjectAsync(string projectPath, string fileName, string objectPath, string newParentPath, CancellationToken cancellationToken = default)
        => PreferEditorAsync(projectPath, "scene.reparent",
            new Dictionary<string, object?>
            {
                ["fileName"] = ToProjectRelative(projectPath, fileName),
                ["objectPath"] = objectPath,
                ["newParentPath"] = newParentPath,
            },
            () => Task.FromResult(SerializeFailure<object>("Scene.ReparentRequiresEditor",
                "Reparent requires the Unity Editor bridge.", UnityMcpErrorCategory.ExternalTool,
                "Install the bridge and set UNITY_EDITOR_PATH or open the project in Unity.")),
            cancellationToken);

    public Task<string> GetSceneObjectPropertiesAsync(string projectPath, string fileName, string objectPath, CancellationToken cancellationToken = default)
        => PreferEditorAsync(projectPath, "scene.get_properties",
            new Dictionary<string, object?>
            {
                ["fileName"] = ToProjectRelative(projectPath, fileName),
                ["objectPath"] = objectPath,
            },
            async () =>
            {
                string list = await ListSceneObjectsAsync(projectPath, fileName, cancellationToken).ConfigureAwait(false);
                return list;
            },
            cancellationToken);

    public Task<string> SetSceneObjectPropertiesAsync(string projectPath, string fileName, string objectPath, string propertiesJson, CancellationToken cancellationToken = default)
    {
        Dictionary<string, object?>? props = null;
        try { props = JsonSerializer.Deserialize<Dictionary<string, object?>>(propertiesJson, JsonOpts); } catch { /* handled below */ }
        if (props is null)
            return Task.FromResult(SerializeFailure<object>("Scene.InvalidProperties", "propertiesJson is invalid.", UnityMcpErrorCategory.Validation));

        return PreferEditorAsync(projectPath, "scene.set_properties",
            new Dictionary<string, object?>
            {
                ["fileName"] = ToProjectRelative(projectPath, fileName),
                ["objectPath"] = objectPath,
                ["properties"] = props,
            },
            () => Task.FromResult(SerializeFailure<object>("Scene.SetPropertiesRequiresEditor",
                "Setting serialized properties requires the Unity Editor bridge.", UnityMcpErrorCategory.ExternalTool)),
            cancellationToken);
    }

    public Task<string> SetSceneObjectActiveAsync(string projectPath, string fileName, string objectPath, bool active, CancellationToken cancellationToken = default)
        => PreferEditorAsync(projectPath, "scene.set_active",
            new Dictionary<string, object?>
            {
                ["fileName"] = ToProjectRelative(projectPath, fileName),
                ["objectPath"] = objectPath,
                ["active"] = active,
            },
            () => Task.FromResult(SerializeFailure<object>("Scene.SetActiveRequiresEditor",
                "Setting active state requires the Unity Editor bridge.", UnityMcpErrorCategory.ExternalTool)),
            cancellationToken);

    public Task<string> ListComponentsAsync(string projectPath, string fileName, string objectPath, CancellationToken cancellationToken = default)
        => PreferEditorAsync(projectPath, "component.list",
            new Dictionary<string, object?>
            {
                ["fileName"] = ToProjectRelative(projectPath, fileName),
                ["objectPath"] = objectPath,
            },
            async () =>
            {
                var graphJson = await ListSceneObjectsAsync(projectPath, fileName, cancellationToken).ConfigureAwait(false);
                return graphJson;
            },
            cancellationToken);

    public Task<string> AddComponentAsync(string projectPath, string fileName, string objectPath, string componentType, CancellationToken cancellationToken = default)
        => PreferEditorAsync(projectPath, "component.add",
            new Dictionary<string, object?>
            {
                ["fileName"] = ToProjectRelative(projectPath, fileName),
                ["objectPath"] = objectPath,
                ["componentType"] = componentType,
            },
            () => Task.FromResult(SerializeFailure<object>("Component.AddRequiresEditor",
                "Adding components requires the Unity Editor bridge.", UnityMcpErrorCategory.ExternalTool)),
            cancellationToken);

    public Task<string> RemoveComponentAsync(string projectPath, string fileName, string objectPath, string componentType, int componentIndex = 0, CancellationToken cancellationToken = default)
        => PreferEditorAsync(projectPath, "component.remove",
            new Dictionary<string, object?>
            {
                ["fileName"] = ToProjectRelative(projectPath, fileName),
                ["objectPath"] = objectPath,
                ["componentType"] = componentType,
                ["componentIndex"] = componentIndex,
            },
            () => Task.FromResult(SerializeFailure<object>("Component.RemoveRequiresEditor",
                "Removing components requires the Unity Editor bridge.", UnityMcpErrorCategory.ExternalTool)),
            cancellationToken);

    public Task<string> GetComponentPropertiesAsync(string projectPath, string fileName, string objectPath, string componentType, int componentIndex = 0, CancellationToken cancellationToken = default)
        => PreferEditorAsync(projectPath, "component.get",
            new Dictionary<string, object?>
            {
                ["fileName"] = ToProjectRelative(projectPath, fileName),
                ["objectPath"] = objectPath,
                ["componentType"] = componentType,
                ["componentIndex"] = componentIndex,
            },
            () => Task.FromResult(SerializeFailure<object>("Component.GetRequiresEditor",
                "Reading component properties requires the Unity Editor bridge.", UnityMcpErrorCategory.ExternalTool)),
            cancellationToken);

    public Task<string> SetComponentPropertiesAsync(string projectPath, string fileName, string objectPath, string componentType, string propertiesJson, int componentIndex = 0, CancellationToken cancellationToken = default)
    {
        Dictionary<string, object?>? props = null;
        try { props = JsonSerializer.Deserialize<Dictionary<string, object?>>(propertiesJson, JsonOpts); } catch { /* */ }
        if (props is null)
            return Task.FromResult(SerializeFailure<object>("Component.InvalidProperties", "propertiesJson is invalid.", UnityMcpErrorCategory.Validation));

        return PreferEditorAsync(projectPath, "component.set",
            new Dictionary<string, object?>
            {
                ["fileName"] = ToProjectRelative(projectPath, fileName),
                ["objectPath"] = objectPath,
                ["componentType"] = componentType,
                ["componentIndex"] = componentIndex,
                ["properties"] = props,
            },
            () => Task.FromResult(SerializeFailure<object>("Component.SetRequiresEditor",
                "Setting component properties requires the Unity Editor bridge.", UnityMcpErrorCategory.ExternalTool)),
            cancellationToken);
    }

    public Task<string> SetComponentEnabledAsync(string projectPath, string fileName, string objectPath, string componentType, bool enabled, int componentIndex = 0, CancellationToken cancellationToken = default)
        => PreferEditorAsync(projectPath, "component.set_enabled",
            new Dictionary<string, object?>
            {
                ["fileName"] = ToProjectRelative(projectPath, fileName),
                ["objectPath"] = objectPath,
                ["componentType"] = componentType,
                ["componentIndex"] = componentIndex,
                ["enabled"] = enabled,
            },
            () => Task.FromResult(SerializeFailure<object>("Component.SetEnabledRequiresEditor",
                "Enabling components requires the Unity Editor bridge.", UnityMcpErrorCategory.ExternalTool)),
            cancellationToken);

    public Task<string> CreateScriptableObjectAsync(string projectPath, string fileName, string typeName, string? propertiesJson = null, CancellationToken cancellationToken = default)
        => PreferEditorAsync(projectPath, "asset.create_scriptable_object",
            new Dictionary<string, object?>
            {
                ["fileName"] = ToProjectRelative(projectPath, fileName),
                ["typeName"] = typeName,
            },
            () => Task.FromResult(SerializeFailure<object>("ScriptableObject.RequiresEditor",
                "Creating ScriptableObjects requires the Unity Editor bridge.", UnityMcpErrorCategory.ExternalTool)),
            cancellationToken);

    public Task<string> ReadSerializedAssetAsync(string projectPath, string fileName, CancellationToken cancellationToken = default)
        => PreferEditorAsync(projectPath, "asset.read_serialized",
            new Dictionary<string, object?> { ["fileName"] = ToProjectRelative(projectPath, fileName) },
            async () =>
            {
                string content = await ReadAssetAsync(projectPath, fileName, cancellationToken).ConfigureAwait(false);
                return JsonSerializer.Serialize(new ToolResultEnvelope<object>
                {
                    Success = true,
                    Message = "Raw asset text read (file-only).",
                    Data = new { content },
                });
            },
            cancellationToken);

    public Task<string> UpdateSerializedAssetAsync(string projectPath, string fileName, string propertiesJson, CancellationToken cancellationToken = default)
    {
        Dictionary<string, object?>? props = null;
        try { props = JsonSerializer.Deserialize<Dictionary<string, object?>>(propertiesJson, JsonOpts); } catch { /* */ }
        if (props is null)
            return Task.FromResult(SerializeFailure<object>("Asset.InvalidProperties", "propertiesJson is invalid.", UnityMcpErrorCategory.Validation));

        return PreferEditorAsync(projectPath, "asset.update_serialized",
            new Dictionary<string, object?>
            {
                ["fileName"] = ToProjectRelative(projectPath, fileName),
                ["properties"] = props,
            },
            () => Task.FromResult(SerializeFailure<object>("Asset.UpdateRequiresEditor",
                "Updating serialized assets requires the Unity Editor bridge.", UnityMcpErrorCategory.ExternalTool)),
            cancellationToken);
    }

    public Task<string> CreateCameraAsync(string projectPath, string fileName, string parentPath, string cameraJson, CancellationToken cancellationToken = default)
    {
        var args = MergeJsonArgs(cameraJson, new Dictionary<string, object?>
        {
            ["fileName"] = ToProjectRelative(projectPath, fileName),
            ["parentPath"] = parentPath,
        });
        return PreferEditorAsync(projectPath, "domain.create_camera", args,
            () => Task.FromResult(SerializeFailure<object>("Camera.CreateRequiresEditor",
                "Creating cameras requires the Unity Editor bridge.", UnityMcpErrorCategory.ExternalTool)),
            cancellationToken);
    }

    public Task<string> UpdateCameraAsync(string projectPath, string fileName, string objectPath, string cameraJson, CancellationToken cancellationToken = default)
    {
        var args = MergeJsonArgs(cameraJson, new Dictionary<string, object?>
        {
            ["fileName"] = ToProjectRelative(projectPath, fileName),
            ["objectPath"] = objectPath,
        });
        return PreferEditorAsync(projectPath, "domain.update_camera", args,
            () => Task.FromResult(SerializeFailure<object>("Camera.UpdateRequiresEditor",
                "Updating cameras requires the Unity Editor bridge.", UnityMcpErrorCategory.ExternalTool)),
            cancellationToken);
    }

    public Task<string> CreateLightAsync(string projectPath, string fileName, string parentPath, string lightJson, CancellationToken cancellationToken = default)
    {
        var args = MergeJsonArgs(lightJson, new Dictionary<string, object?>
        {
            ["fileName"] = ToProjectRelative(projectPath, fileName),
            ["parentPath"] = parentPath,
        });
        return PreferEditorAsync(projectPath, "domain.create_light", args,
            () => Task.FromResult(SerializeFailure<object>("Light.CreateRequiresEditor",
                "Creating lights requires the Unity Editor bridge.", UnityMcpErrorCategory.ExternalTool)),
            cancellationToken);
    }

    public Task<string> UpdateLightAsync(string projectPath, string fileName, string objectPath, string lightJson, CancellationToken cancellationToken = default)
    {
        var args = MergeJsonArgs(lightJson, new Dictionary<string, object?>
        {
            ["fileName"] = ToProjectRelative(projectPath, fileName),
            ["objectPath"] = objectPath,
        });
        return PreferEditorAsync(projectPath, "domain.update_light", args,
            () => Task.FromResult(SerializeFailure<object>("Light.UpdateRequiresEditor",
                "Updating lights requires the Unity Editor bridge.", UnityMcpErrorCategory.ExternalTool)),
            cancellationToken);
    }

    public Task<string> CreatePhysicsBodyAsync(string projectPath, string fileName, string parentPath, string physicsJson, CancellationToken cancellationToken = default)
    {
        var args = MergeJsonArgs(physicsJson, new Dictionary<string, object?>
        {
            ["fileName"] = ToProjectRelative(projectPath, fileName),
            ["parentPath"] = parentPath,
        });
        return PreferEditorAsync(projectPath, "domain.create_physics", args,
            () => Task.FromResult(SerializeFailure<object>("Physics.CreateRequiresEditor",
                "Creating physics bodies requires the Unity Editor bridge.", UnityMcpErrorCategory.ExternalTool)),
            cancellationToken);
    }

    public Task<string> UpdatePhysicsBodyAsync(string projectPath, string fileName, string objectPath, string physicsJson, CancellationToken cancellationToken = default)
    {
        var args = MergeJsonArgs(physicsJson, new Dictionary<string, object?>
        {
            ["fileName"] = ToProjectRelative(projectPath, fileName),
            ["objectPath"] = objectPath,
        });
        return PreferEditorAsync(projectPath, "domain.update_physics", args,
            () => Task.FromResult(SerializeFailure<object>("Physics.UpdateRequiresEditor",
                "Updating physics bodies requires the Unity Editor bridge.", UnityMcpErrorCategory.ExternalTool)),
            cancellationToken);
    }

    public Task<string> SearchPackagesAsync(string projectPath, string query, CancellationToken cancellationToken = default)
        => PreferEditorAsync(projectPath, "ecosystem.search_packages",
            new Dictionary<string, object?> { ["query"] = query },
            () => Task.FromResult(SerializeFailure<object>("Packages.SearchRequiresEditor",
                "UPM search requires the Unity Editor bridge.", UnityMcpErrorCategory.ExternalTool)),
            cancellationToken);

    public Task<string> ResolvePackagesAsync(string projectPath, CancellationToken cancellationToken = default)
        => PreferEditorAsync(projectPath, "ecosystem.resolve_packages", null,
            () => Task.FromResult(SerializeFailure<object>("Packages.ResolveRequiresEditor",
                "UPM resolve requires the Unity Editor bridge.", UnityMcpErrorCategory.ExternalTool)),
            cancellationToken);

    public Task<string> QueryUnityEngineDocumentationAsync(string query, string? unityVersion = null, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        // Lightweight Unity-version-aware Scripting API search via public docs URL patterns + local fallback.
        maxResults = Math.Clamp(maxResults, 1, 25);
        string versionSegment = string.IsNullOrWhiteSpace(unityVersion) ? "6000.0" : unityVersion.Trim();
        var results = new List<object>
        {
            new
            {
                title = $"Unity Scripting API search: {query}",
                url = $"https://docs.unity3d.com/ScriptReference/30_search.html?q={Uri.EscapeDataString(query)}",
                unityVersion = versionSegment,
                snippet = $"Search Unity {versionSegment} Scripting API for '{query}'.",
            },
            new
            {
                title = $"Unity Manual search: {query}",
                url = $"https://docs.unity3d.com/Manual/30_search.html?q={Uri.EscapeDataString(query)}",
                unityVersion = versionSegment,
                snippet = $"Search Unity {versionSegment} Manual for '{query}'.",
            },
        };
        return Task.FromResult(JsonSerializer.Serialize(new ToolResultEnvelope<object>
        {
            Success = true,
            Message = "Unity engine documentation links generated.",
            Data = new { query, unityVersion = versionSegment, results = results.Take(maxResults).ToArray() },
        }));
    }

    private async Task<string> PreferEditorAsync(
        string projectPath,
        string operation,
        IReadOnlyDictionary<string, object?>? args,
        Func<Task<string>> fileFallback,
        CancellationToken cancellationToken)
    {
        _projectPath = projectPath;
        if (IsEditorAvailable(projectPath))
        {
            return await _editorExecutor!.ExecuteAsync(projectPath, operation, args, cancellationToken).ConfigureAwait(false);
        }

        return await fileFallback().ConfigureAwait(false);
    }

    private bool IsEditorAvailable(string projectPath)
        => _editorExecutor is not null
           && (_editorExecutor.FindUnityExecutable() is not null
               || (_editorExecutor.TryGetLiveBridgeStatus(projectPath, out bool connected, out _) && connected));

    private Task<string> TryEditorOrFileAsync(
        string projectPath,
        string operation,
        IReadOnlyDictionary<string, object?>? args,
        Func<Task<string>> fileFallback,
        CancellationToken cancellationToken)
        => PreferEditorAsync(projectPath, operation, args, fileFallback, cancellationToken);

    private string ToProjectRelative(string projectPath, string fileName)
    {
        try
        {
            string resolved = ResolvePath(projectPath, fileName);
            return MakeProjectRelativePath(projectPath, resolved);
        }
        catch
        {
            return fileName.Replace('\\', '/');
        }
    }

    private static Dictionary<string, object?> MergeJsonArgs(string json, Dictionary<string, object?> baseArgs)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOpts);
            if (parsed is not null)
            {
                foreach (var kv in parsed)
                    baseArgs[kv.Key] = kv.Value;
            }
        }
        catch
        {
            // Ignore invalid JSON; editor handler will validate.
        }
        return baseArgs;
    }

    /// <summary>
    /// Resolves path under project using PathResolver containment rules.
    /// </summary>
    private string ResolvePath(string projectPath, string nameOrPath)
    {
        string projectRoot = ValidateProjectRoot(projectPath, requireExists: false);
        return PathResolver.ForProject(projectRoot).ResolveUnderProject(projectRoot, nameOrPath);
    }

    /// <summary>
    /// For Save*: fileName can be a bare name (e.g. Player.cs) or path. If bare name, use projectPath/Assets/defaultSubfolder/fileName;
    /// if path already has segments, combine with projectPath without duplicating (Assets/Scripts appear only once).
    /// </summary>
    private string ResolveAssetPath(string projectPath, string fileName, string defaultSubfolder)
    {
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "unnamed";
        fileName = fileName.Trim().Replace('/', _fs.Path.DirectorySeparatorChar);
        string projectRoot = ValidateProjectRoot(projectPath, requireExists: false);

        bool hasDirSep = fileName.Contains(_fs.Path.DirectorySeparatorChar);
        if (hasDirSep)
            return ResolvePath(projectPath ?? "", fileName);

        // Plain filename: projectRoot/Assets/defaultSubfolder/fileName, but don't duplicate if projectRoot already ends with Assets or Assets/defaultSubfolder
        string defaultPrefix = _fs.Path.Combine("Assets", defaultSubfolder);
        string projectRootNorm = projectRoot.Replace('/', _fs.Path.DirectorySeparatorChar);
        string defaultPrefixNorm = defaultPrefix.Replace('/', _fs.Path.DirectorySeparatorChar);
        if (projectRootNorm.EndsWith(defaultPrefixNorm, StringComparison.OrdinalIgnoreCase))
            return EnsureResolvedAssetInside(projectRoot, _fs.Path.GetFullPath(_fs.Path.Combine(projectRoot, fileName)));
        if (projectRootNorm.EndsWith("Assets", StringComparison.OrdinalIgnoreCase))
            return EnsureResolvedAssetInside(projectRoot, _fs.Path.GetFullPath(_fs.Path.Combine(projectRoot, defaultSubfolder, fileName)));
        return EnsureResolvedAssetInside(projectRoot, _fs.Path.GetFullPath(_fs.Path.Combine(projectRoot, "Assets", defaultSubfolder, fileName)));
    }

    // -----------------------------------------------------------------------
    // Utility
    // -----------------------------------------------------------------------

    private static string SerializeFailure<TData>(string code, string message, UnityMcpErrorCategory category, string? remediation = null)
        => JsonSerializer.Serialize(new ToolResultEnvelope<TData>
        {
            Success = false,
            Message = message,
            SuggestedRemediation = remediation,
            Errors =
            [
                new UnityMcpError
                {
                    Category = category,
                    Code = code,
                    Message = message,
                }
            ],
        });

    private UnitySceneGraph BuildSceneGraph(string projectPath, string resolvedPath, string content)
    {
        var objects = new List<UnitySceneObject>();
        foreach (Match match in Regex.Matches(content, @"(?ms)^--- !u!1 &(?<id>\d+)\s*\nGameObject:\s*(?<body>.*?)(?=^--- !u!|\z)"))
        {
            string id = match.Groups["id"].Value;
            string body = match.Groups["body"].Value;
            string name = Regex.Match(body, @"(?m)^  m_Name:\s*(?<name>.*)$").Groups["name"].Value.Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = $"GameObject_{id}";

            var components = Regex.Matches(body, @"component:\s*\{fileID:\s*(?<id>\d+)\}")
                .Select(componentMatch => ResolveComponentType(content, componentMatch.Groups["id"].Value))
                .Where(type => !string.IsNullOrWhiteSpace(type))
                .ToArray();

            var transformMatch = Regex.Match(content, $@"(?ms)^--- !u!4 &(?<transformId>\d+)\s*\nTransform:\s*.*?m_GameObject:\s*\{{fileID:\s*{Regex.Escape(id)}\}}(?<body>.*?)(?=^--- !u!|\z)");
            string? parentFileId = null;
            var properties = new Dictionary<string, object?>();
            if (transformMatch.Success)
            {
                string transformBody = transformMatch.Groups["body"].Value;
                var parentMatch = Regex.Match(transformBody, @"m_Father:\s*\{fileID:\s*(?<id>\d+)\}");
                if (parentMatch.Success && parentMatch.Groups["id"].Value != "0")
                    parentFileId = parentMatch.Groups["id"].Value;
                properties["position"] = Regex.Match(transformBody, @"m_LocalPosition:\s*(?<value>\{.*\})").Groups["value"].Value;
                properties["rotation"] = Regex.Match(transformBody, @"m_LocalRotation:\s*(?<value>\{.*\})").Groups["value"].Value;
                properties["scale"] = Regex.Match(transformBody, @"m_LocalScale:\s*(?<value>\{.*\})").Groups["value"].Value;
            }

            objects.Add(new UnitySceneObject
            {
                Name = name,
                Path = name,
                FileId = id,
                ParentFileId = parentFileId,
                Components = components,
                Properties = properties,
            });
        }

        return new UnitySceneGraph
        {
            ScenePath = MakeProjectRelativePath(projectPath, resolvedPath),
            Objects = objects,
        };
    }

    private static UnitySceneObject? FindSceneObject(UnitySceneGraph graph, string objectPath)
    {
        string normalized = objectPath.Trim();
        return graph.Objects.FirstOrDefault(o =>
            string.Equals(o.Path, normalized, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(o.Name, normalized, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(o.FileId, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveComponentType(string content, string componentFileId)
    {
        var match = Regex.Match(content, $@"(?ms)^--- !u!(?<classId>\d+) &{Regex.Escape(componentFileId)}\s*\n(?<type>\w+):");
        if (!match.Success)
            return string.Empty;
        return match.Groups["type"].Value;
    }

    private static string ReplaceGameObjectName(string content, string gameObjectFileId, string newName)
    {
        return Regex.Replace(content,
            $@"(?ms)(^--- !u!1 &{Regex.Escape(gameObjectFileId)}\s*\nGameObject:\s*.*?^  m_Name:\s*).*$",
            "$1" + EscapeYamlScalar(newName),
            RegexOptions.Multiline);
    }

    private static string RemoveYamlDocumentsForObject(string content, string gameObjectFileId)
    {
        var componentIds = new HashSet<string> { gameObjectFileId };
        var goMatch = Regex.Match(content, $@"(?ms)^--- !u!1 &{Regex.Escape(gameObjectFileId)}\s*\nGameObject:\s*(?<body>.*?)(?=^--- !u!|\z)");
        if (goMatch.Success)
        {
            foreach (Match componentMatch in Regex.Matches(goMatch.Groups["body"].Value, @"component:\s*\{fileID:\s*(?<id>\d+)\}"))
                componentIds.Add(componentMatch.Groups["id"].Value);
        }

        foreach (string id in componentIds)
            content = Regex.Replace(content, $@"(?ms)^--- !u!\d+ &{Regex.Escape(id)}\s*\n.*?(?=^--- !u!|\z)", string.Empty);
        return content;
    }

    private static string ExtractYamlDocumentsForObject(string content, string gameObjectFileId)
    {
        var documents = new List<string>();
        var componentIds = new HashSet<string> { gameObjectFileId };
        var goMatch = Regex.Match(content, $@"(?ms)^--- !u!1 &{Regex.Escape(gameObjectFileId)}\s*\nGameObject:\s*(?<body>.*?)(?=^--- !u!|\z)");
        if (goMatch.Success)
        {
            documents.Add(goMatch.Value);
            foreach (Match componentMatch in Regex.Matches(goMatch.Groups["body"].Value, @"component:\s*\{fileID:\s*(?<id>\d+)\}"))
                componentIds.Add(componentMatch.Groups["id"].Value);
        }

        foreach (string id in componentIds.Where(id => id != gameObjectFileId))
        {
            var match = Regex.Match(content, $@"(?ms)^--- !u!\d+ &{Regex.Escape(id)}\s*\n.*?(?=^--- !u!|\z)");
            if (match.Success)
                documents.Add(match.Value);
        }
        return string.Join("\n", documents);
    }

    private static long NextYamlFileId(string content)
    {
        var ids = Regex.Matches(content, @"^--- !u!\d+ &(?<id>\d+)", RegexOptions.Multiline)
            .Select(match => long.TryParse(match.Groups["id"].Value, out long id) ? id : 0)
            .DefaultIfEmpty(100);
        return ids.Max() + 1;
    }

    private static string AddComponentReference(string content, string gameObjectFileId, string componentFileId)
    {
        return Regex.Replace(content,
            $@"(?ms)(^--- !u!1 &{Regex.Escape(gameObjectFileId)}\s*\nGameObject:\s*.*?^  m_Component:\s*\n)",
            "$1  - component: {fileID: " + componentFileId + "}\n",
            RegexOptions.Multiline);
    }

    private UnityAssetMetadata BuildAssetMetadata(string projectPath, string resolvedPath)
    {
        string metaPath = resolvedPath + ".meta";
        string? guid = TryReadGuid(metaPath);
        string content = IsTextLikeAsset(resolvedPath) ? _fs.File.ReadAllText(resolvedPath) : string.Empty;
        var dependencies = Regex.Matches(content, @"guid:\s*([a-fA-F0-9]{32})")
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new UnityAssetMetadata
        {
            Path = MakeProjectRelativePath(projectPath, resolvedPath),
            Guid = guid,
            Type = _fs.Path.GetExtension(resolvedPath).TrimStart('.').ToLowerInvariant(),
            HasMeta = _fs.File.Exists(metaPath),
            Dependencies = dependencies,
        };
    }

    private string? TryReadGuid(string metaPath)
    {
        if (!_fs.File.Exists(metaPath))
            return null;
        var match = Regex.Match(_fs.File.ReadAllText(metaPath), @"guid:\s*([a-fA-F0-9]{32})");
        return match.Success ? match.Groups[1].Value : null;
    }

    private bool GuidExists(string projectPath, string guid)
    {
        string assetsPath = _fs.Path.Combine(projectPath, "Assets");
        if (!_fs.Directory.Exists(assetsPath))
            return false;
        return _fs.Directory.EnumerateFiles(assetsPath, "*.meta", SearchOption.AllDirectories)
            .Any(meta => string.Equals(TryReadGuid(meta), guid, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsTextLikeAsset(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        return extension is ".unity" or ".prefab" or ".mat" or ".asset" or ".controller" or ".anim" or ".json" or ".inputactions" or ".cs" or ".txt";
    }

    private string ListDomainObjects(string projectPath, string folderName, string domainName, params string[] componentTypes)
    {
        string folderPath = ResolvePath(projectPath, string.IsNullOrWhiteSpace(folderName) ? "Assets/Scenes" : folderName);
        if (!_fs.Directory.Exists(folderPath))
            return SerializeFailure<IReadOnlyList<UnitySceneObject>>($"{domainName}.FolderNotFound", $"Folder not found: {folderPath}", UnityMcpErrorCategory.Io);

        var objects = new List<UnitySceneObject>();
        foreach (string scenePath in _fs.Directory.EnumerateFiles(folderPath, "*.unity", SearchOption.AllDirectories))
        {
            var graph = BuildSceneGraph(projectPath, scenePath, _fs.File.ReadAllText(scenePath));
            objects.AddRange(graph.Objects.Where(o => o.Components.Any(c => componentTypes.Contains(c, StringComparer.OrdinalIgnoreCase))));
        }

        return JsonSerializer.Serialize(new ToolResultEnvelope<IReadOnlyList<UnitySceneObject>>
        {
            Success = true,
            Data = objects,
            Message = $"Listed {objects.Count} {domainName} objects.",
        });
    }

    private string ValidateDomainObjects(string projectPath, string folderName, string domainName, bool requireAtLeastOne, params string[] componentTypes)
    {
        string json = ListDomainObjects(projectPath, folderName, domainName, componentTypes.Length == 0 ? new[] { domainName } : componentTypes);
        var envelope = JsonSerializer.Deserialize<ToolResultEnvelope<IReadOnlyList<UnitySceneObject>>>(json, JsonOpts);
        var warnings = new List<UnityMcpError>();
        var objects = envelope?.Data ?? Array.Empty<UnitySceneObject>();
        if (objects.Count == 0 && requireAtLeastOne)
        {
            warnings.Add(new UnityMcpError
            {
                Category = UnityMcpErrorCategory.Validation,
                Code = $"{domainName}.Missing",
                Message = $"No {domainName} objects were found.",
                Details = new { suggestedRemediation = $"Create at least one {domainName} in a scene under {folderName}." },
            });
        }

        if (string.Equals(domainName, "Physics", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var obj in objects)
            {
                bool hasBody = obj.Components.Any(c => c.Contains("Rigidbody", StringComparison.OrdinalIgnoreCase));
                bool hasCollider = obj.Components.Any(c => c.Contains("Collider", StringComparison.OrdinalIgnoreCase));
                if (hasBody && !hasCollider)
                {
                    warnings.Add(new UnityMcpError
                    {
                        Category = UnityMcpErrorCategory.Validation,
                        Code = "Physics.RigidbodyWithoutCollider",
                        Message = $"{obj.Path} has a Rigidbody but no Collider.",
                    });
                }
            }
        }

        if (string.Equals(domainName, "Light", StringComparison.OrdinalIgnoreCase) && objects.Count == 0)
        {
            warnings.Add(new UnityMcpError
            {
                Category = UnityMcpErrorCategory.Validation,
                Code = "Light.NoneFound",
                Message = "No lights found in scanned scenes.",
            });
        }

        return JsonSerializer.Serialize(new ImportValidationResult
        {
            Success = true,
            ErrorCount = 0,
            WarningCount = warnings.Count,
            Errors = Array.Empty<UnityMcpError>(),
            Warnings = warnings,
            Message = $"{domainName} validation completed.",
        });
    }

    private static bool DictionaryEquals(IReadOnlyDictionary<string, object?> left, IReadOnlyDictionary<string, object?> right)
    {
        if (left.Count != right.Count)
            return false;
        foreach (var entry in left)
        {
            if (!right.TryGetValue(entry.Key, out object? value))
                return false;
            if (!string.Equals(Convert.ToString(entry.Value), Convert.ToString(value), StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    private static string EscapeYamlScalar(string value)
        => value.Replace("\r", string.Empty).Replace("\n", " ").Trim();

    private static string FormatJsonColor(JsonElement value)
    {
        double r = value.TryGetProperty("r", out var rValue) ? rValue.GetDouble() : 1;
        double g = value.TryGetProperty("g", out var gValue) ? gValue.GetDouble() : 1;
        double b = value.TryGetProperty("b", out var bValue) ? bValue.GetDouble() : 1;
        double a = value.TryGetProperty("a", out var aValue) ? aValue.GetDouble() : 1;
        return $"{{r: {r:G}, g: {g:G}, b: {b:G}, a: {a:G}}}";
    }

    private static string ReplaceYamlListEntry(string content, string key, string value)
    {
        if (content.Contains($"- {key}:", StringComparison.Ordinal))
            return Regex.Replace(content, $@"(?m)^    - {Regex.Escape(key)}: .*$", $"    - {key}: {value}");
        return content;
    }

    private static string SanitizeAssetName(string value)
    {
        string sanitized = Regex.Replace(value, @"[^a-zA-Z0-9_\-]", "_").Trim('_');
        return string.IsNullOrWhiteSpace(sanitized) ? "Asset" : sanitized;
    }

    private async Task<Dictionary<string, string>> ReadPackageDependenciesAsync(string manifestPath, CancellationToken cancellationToken)
    {
        if (!_fs.File.Exists(manifestPath))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string existing = await _fs.File.ReadAllTextAsync(manifestPath, cancellationToken);
        using JsonDocument existingDoc = JsonDocument.Parse(existing);
        var dependencies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (existingDoc.RootElement.TryGetProperty("dependencies", out var deps))
        {
            foreach (var prop in deps.EnumerateObject())
                dependencies[prop.Name] = prop.Value.GetString() ?? string.Empty;
        }
        return dependencies;
    }

    private async Task WritePackageDependenciesAsync(string manifestPath, Dictionary<string, string> dependencies, CancellationToken cancellationToken)
    {
        string? dir = _fs.Path.GetDirectoryName(manifestPath);
        if (!string.IsNullOrWhiteSpace(dir))
            _fs.Directory.CreateDirectory(dir);
        var manifestObject = new Dictionary<string, object> { ["dependencies"] = dependencies.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value) };
        await _fs.File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifestObject, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
    }

    private string? TryFindUnityExecutable()
    {
        try
        {
            return FindUnityExecutable();
        }
        catch
        {
            return null;
        }
    }

    private string FindRepositoryRoot()
    {
        string current = _fs.Directory.GetCurrentDirectory();
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (_fs.File.Exists(_fs.Path.Combine(current, "README.md")) &&
                (_fs.Directory.Exists(_fs.Path.Combine(current, "Docs")) || _fs.Directory.Exists(_fs.Path.Combine(current, "Skills"))))
            {
                return current;
            }

            string? parent = _fs.Directory.GetParent(current)?.FullName;
            if (string.IsNullOrWhiteSpace(parent) || string.Equals(parent, current, StringComparison.OrdinalIgnoreCase))
                break;
            current = parent;
        }

        return _fs.Directory.GetCurrentDirectory();
    }

    private string ValidateProjectRoot(string projectPath, bool requireExists)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path is required.", nameof(projectPath));
        if (projectPath.Contains("://", StringComparison.Ordinal))
            throw new ArgumentException("URI-style project paths are not supported.", nameof(projectPath));
        string root = _fs.Path.GetFullPath(projectPath.Trim()).TrimEnd(_fs.Path.DirectorySeparatorChar, _fs.Path.AltDirectorySeparatorChar);
        if (requireExists && !_fs.Directory.Exists(root))
            throw new DirectoryNotFoundException($"Project path does not exist: {root}");
        return root;
    }

    private void EnsureInsideProject(string projectRoot, string candidatePath)
    {
        string root = _fs.Path.GetFullPath(projectRoot).TrimEnd(_fs.Path.DirectorySeparatorChar, _fs.Path.AltDirectorySeparatorChar);
        string candidate = _fs.Path.GetFullPath(candidatePath).TrimEnd(_fs.Path.DirectorySeparatorChar, _fs.Path.AltDirectorySeparatorChar);
        string rootWithSeparator = root + _fs.Path.DirectorySeparatorChar;
        if (!candidate.Equals(root, StringComparison.OrdinalIgnoreCase) &&
            !candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Path must resolve inside project root. Path: {candidate}");
        }
    }

    private string EnsureResolvedAssetInside(string projectRoot, string resolvedPath)
    {
        EnsureInsideProject(projectRoot, resolvedPath);
        return resolvedPath;
    }

    private void EnsureDirectoryExists(string filePath)
    {
        var dir = _fs.Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !_fs.Directory.Exists(dir))
            _fs.Directory.CreateDirectory(dir);
    }

    private static string MakeProjectRelativePath(string projectPath, string absolutePath)
    {
        try
        {
            var projectFull = Path.GetFullPath(projectPath);
            var fileFull = Path.GetFullPath(absolutePath);
            if (fileFull.StartsWith(projectFull, StringComparison.OrdinalIgnoreCase))
            {
                string trimmed = fileFull.Substring(projectFull.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return trimmed.Replace('\\', '/');
            }
        }
        catch
        {
            // Fall back to absolute path if anything goes wrong.
        }

        return absolutePath.Replace('\\', '/');
    }

    private static void BuildPanelHierarchy(UiPanel panel, List<GameObjectDef> output)
    {
        var panelGo = new GameObjectDef
        {
            Name = string.IsNullOrWhiteSpace(panel.Name) ? "Panel" : panel.Name,
            Tag = "Untagged",
            Layer = 5,
            Position = RectToPosition(panel.Rect),
            Scale = new Vector3Def(panel.Rect.SizeDelta.X, panel.Rect.SizeDelta.Y, 1),
            Components =
            [
                new ComponentDef(UnityYamlWriter.ClassId_CanvasRenderer),
                new ComponentDef(UnityYamlWriter.ClassId_MonoBehaviour)
                {
                    Properties = { ["mcpComponentHint"] = "Image" }
                },
            ],
        };
        output.Add(panelGo);

        foreach (var control in panel.Controls)
        {
            var controlGo = new GameObjectDef
            {
                Name = string.IsNullOrWhiteSpace(control.Name) ? control.Type.ToString() : control.Name,
                Tag = "Untagged",
                Layer = 5,
                Position = RectToPosition(control.Rect),
                Scale = new Vector3Def(control.Rect.SizeDelta.X, control.Rect.SizeDelta.Y, 1),
                Components =
                [
                    new ComponentDef(UnityYamlWriter.ClassId_CanvasRenderer),
                    new ComponentDef(UnityYamlWriter.ClassId_MonoBehaviour)
                    {
                        Properties = { ["mcpComponentHint"] = control.Type.ToString(), ["text"] = control.Text ?? string.Empty }
                    },
                ],
            };
            output.Add(controlGo);
        }

        foreach (var child in panel.Children)
        {
            BuildPanelHierarchy(child, output);
        }
    }

    private static Vector3Def RectToPosition(UiRectTransform rect)
    {
        // Map anchoredPosition (x, y) into world-space X/Y; keep Z at 0.
        return new Vector3Def(rect.AnchoredPosition.X, rect.AnchoredPosition.Y, 0);
    }

    private string FindUnityExecutable()
    {
        var envPath = Environment.GetEnvironmentVariable("UNITY_EDITOR_PATH");
        if (!string.IsNullOrEmpty(envPath) && _fs.File.Exists(envPath))
            return envPath;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var programFiles = Environment.GetEnvironmentVariable("ProgramFiles")
                ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrEmpty(programFiles))
            {
                var hubEditor = _fs.Path.Combine(programFiles, "Unity", "Hub", "Editor");
                if (_fs.Directory.Exists(hubEditor))
                {
                    foreach (var versionDir in _fs.Directory.EnumerateDirectories(hubEditor).OrderByDescending(d => d))
                    {
                        var unityExe = _fs.Path.Combine(versionDir, "Editor", "Unity.exe");
                        if (_fs.File.Exists(unityExe))
                            return unityExe;
                    }
                }
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Standard Unity Hub location on macOS
            var hubEditor = _fs.Path.Combine("/", "Applications", "Unity", "Hub", "Editor");
            if (_fs.Directory.Exists(hubEditor))
            {
                foreach (var versionDir in _fs.Directory.EnumerateDirectories(hubEditor).OrderByDescending(d => d))
                {
                    var unityApp = _fs.Path.Combine(versionDir, "Unity.app", "Contents", "MacOS", "Unity");
                    if (_fs.File.Exists(unityApp))
                        return unityApp;
                }
            }
        }
        else
        {
            // Linux and other Unix-like: system path or Unity Hub under user home
            var linuxUnity = _fs.Path.Combine("/", "usr", "bin", "unity");
            if (_fs.File.Exists(linuxUnity))
                return linuxUnity;
            var home = Environment.GetEnvironmentVariable("HOME")
                ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(home))
            {
                var hubEditor = _fs.Path.Combine(home, "Unity", "Hub", "Editor");
                if (_fs.Directory.Exists(hubEditor))
                {
                    foreach (var versionDir in _fs.Directory.EnumerateDirectories(hubEditor).OrderByDescending(d => d))
                    {
                        var unityExe = _fs.Path.Combine(versionDir, "Editor", "Unity");
                        if (_fs.File.Exists(unityExe))
                            return unityExe;
                    }
                }
            }
        }

        throw new FileNotFoundException(
            "Unity Editor executable not found. Set UNITY_EDITOR_PATH environment variable.");
    }
}
