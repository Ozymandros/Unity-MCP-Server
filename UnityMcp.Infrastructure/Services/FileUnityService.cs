using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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

    public FileUnityService(ILogger<FileUnityService> logger, IProcessRunner processRunner, IFileSystem? fileSystem = null)
    {
        _logger = logger;
        _processRunner = processRunner;
        _fs = fileSystem ?? new FileSystem();
        _metaWriter = new MetaFileWriter(_fs);
    }

    public Task<bool> IsValidProjectAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        _projectPath = projectPath;
        bool isValid = _fs.Directory.Exists(_fs.Path.Combine(projectPath, "Assets"));
        return Task.FromResult(isValid);
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

    public async Task<string> ScaffoldProjectAsync(string projectName, string? outputRoot = null, string? unityVersion = null, string? template = null, CancellationToken cancellationToken = default)
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

        // Packages/manifest.json (write template-appropriate dependencies)
        await WritePackagesManifestAsync(projectDir, template, cancellationToken);

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

    // -----------------------------------------------------------------------
    // Validation & package management
    // -----------------------------------------------------------------------

    public Task<string> ValidateCSharpAsync(string code, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // Check balanced braces
        int braceCount = 0;
        foreach (char c in code)
        {
            if (c == '{') braceCount++;
            else if (c == '}') braceCount--;
            if (braceCount < 0) { errors.Add("Unmatched closing brace '}'"); break; }
        }
        if (braceCount > 0) errors.Add($"Missing {braceCount} closing brace(s) '}}'");

        // Check balanced parentheses
        int parenCount = 0;
        foreach (char c in code)
        {
            if (c == '(') parenCount++;
            else if (c == ')') parenCount--;
            if (parenCount < 0) { errors.Add("Unmatched closing parenthesis ')'"); break; }
        }
        if (parenCount > 0) errors.Add($"Missing {parenCount} closing parenthesis/es ')'");

        // Check for class/struct/interface keyword
        if (!System.Text.RegularExpressions.Regex.IsMatch(code, @"\b(class|struct|interface|enum)\b"))
            errors.Add("No class, struct, interface, or enum keyword found");

        // Check for using directive
        if (!code.Contains("using "))
            errors.Add("No 'using' directive found (expected at least UnityEngine)");

        bool isValid = errors.Count == 0;
        var result = JsonSerializer.Serialize(new { isValid, errors });
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

    private async Task WritePackagesManifestAsync(string projectDir, string? unityTemplate, CancellationToken cancellationToken = default)
    {
        string packagesDir = _fs.Path.Combine(projectDir, "Packages");
        if (!_fs.Directory.Exists(packagesDir))
            _fs.Directory.CreateDirectory(packagesDir);

        // Template -> package dependencies mapping
        var templatePackages = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["urp"] = new Dictionary<string, string>
            {
                ["com.unity.render-pipelines.universal"] = DefaultPackageVersions.TryGetValue("com.unity.render-pipelines.universal", out var v) ? v : ""
            },
            ["hdrp"] = new Dictionary<string, string>
            {
                ["com.unity.render-pipelines.high-definition"] = DefaultPackageVersions.TryGetValue("com.unity.render-pipelines.core", out var hv) ? hv : ""
            },
            ["vr"] = new Dictionary<string, string>
            {
                ["com.unity.xr.management"] = "4.0.1"
            },
            ["2d"] = new Dictionary<string, string>
            {
                ["com.unity.2d.sprite"] = "2.0.0"
            }
            // 3d and mobile intentionally map to empty dependencies
        };

        Dictionary<string, string> deps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(unityTemplate) && templatePackages.TryGetValue(unityTemplate.Trim().ToLowerInvariant(), out var mapped))
        {
            foreach (var kv in mapped)
                deps[kv.Key] = kv.Value;
        }

        var manifestObject = new Dictionary<string, object> { ["dependencies"] = deps };
        string manifestPath = _fs.Path.Combine(packagesDir, "manifest.json");
        string output = JsonSerializer.Serialize(manifestObject, new JsonSerializerOptions { WriteIndented = true });
        await _fs.File.WriteAllTextAsync(manifestPath, output, cancellationToken);
        _logger.LogInformation("Wrote manifest.json for template {Template} at {Path}", unityTemplate ?? "", manifestPath);
    }

    /// <summary>
    /// Validates import (asset refresh + script compilation). File-only stub: returns success with zero counts.
    /// TODO: Run Unity in batch mode with an Editor script that performs AssetDatabase.Refresh and compilation,
    /// writes mcp_validate_result.json, then read and return its contents.
    /// </summary>
    public Task<string> ValidateImportAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        var result = new ImportValidationResult
        {
            Success = true,
            ErrorCount = 0,
            WarningCount = 0,
            Errors = Array.Empty<UnityMcpError>(),
            Warnings = Array.Empty<UnityMcpError>(),
            Message = "Stub: file-only server cannot run Unity compilation. Implement batch-mode validation when Unity is available.",
        };

        return Task.FromResult(JsonSerializer.Serialize(result));
    }

    // -----------------------------------------------------------------------
    // UI authoring (Phase 1 foundations)
    // -----------------------------------------------------------------------

    public async Task<string> CreateUiCanvasAsync(string projectPath, string fileName, CancellationToken cancellationToken = default)
    {
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
        };

        var eventSystem = new GameObjectDef
        {
            Name = "EventSystem",
            Tag = "Untagged",
            Layer = 5,
            Position = new Vector3Def(0, 0, 0),
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

        string relativePath = MakeProjectRelativePath(projectPath, resolvedPath);
        var successResult = new { success = true, path = relativePath, message = (string?)null, errors = Array.Empty<UnityMcpError>() };
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
            return JsonSerializer.Serialize(new { success = true, path = relativePath, message = result.Message, errors = Array.Empty<UnityMcpError>(), warnings });
        }
        return JsonSerializer.Serialize(new { success = true, path = relativePath, message = "Animator definition created successfully.", errors = Array.Empty<UnityMcpError>(), warnings = Array.Empty<UnityMcpError>() });
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

        string relativePath = MakeProjectRelativePath(projectPath, resolvedPath);
        return JsonSerializer.Serialize(new { success = true, path = relativePath, message = "Advanced animator definition created successfully.", errors = Array.Empty<UnityMcpError>() });
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
            return JsonSerializer.Serialize(new { success = true, path = relPath, message = result.Message, errors = Array.Empty<UnityMcpError>(), warnings });
        }

        return JsonSerializer.Serialize(new { success = true, path = relPath, message = "Timeline created successfully.", errors = Array.Empty<UnityMcpError>(), warnings = Array.Empty<UnityMcpError>() });
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

        string relativePath = MakeProjectRelativePath(projectPath, resolvedPath);
        return JsonSerializer.Serialize(new { success = true, path = relativePath, message = "Physics setup created successfully.", errors = Array.Empty<UnityMcpError>() });
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

        string relativePath = MakeProjectRelativePath(projectPath, resolvedPath);
        return JsonSerializer.Serialize(new
        {
            success = true,
            path = relativePath,
            message = "VFX asset created successfully.",
            errors = Array.Empty<UnityMcpError>(),
        });
    }

    /// <summary>
    /// Resolves path under project. Validates both projectPath and nameOrPath; builds final path
    /// without duplicating segments (e.g. if projectPath ends with Assets and nameOrPath starts with Assets/Scripts, use as-is).
    /// </summary>
    private string ResolvePath(string projectPath, string nameOrPath)
    {
        if (string.IsNullOrWhiteSpace(nameOrPath))
            throw new ArgumentException("Path or file name cannot be empty.", nameof(nameOrPath));
        nameOrPath = nameOrPath.Trim().Replace('/', _fs.Path.DirectorySeparatorChar);
        string projectRoot = _fs.Path.GetFullPath(projectPath ?? "").TrimEnd(_fs.Path.DirectorySeparatorChar, '/');

        if (!nameOrPath.Contains(_fs.Path.DirectorySeparatorChar))
            return _fs.Path.GetFullPath(_fs.Path.Combine(projectRoot, nameOrPath));

        // Strip leading segments from nameOrPath that duplicate trailing segments of projectRoot
        string[] projectSegments = projectRoot.Split(new[] { _fs.Path.DirectorySeparatorChar, '/' }, StringSplitOptions.RemoveEmptyEntries);
        string[] nameSegments = nameOrPath.Split(new[] { _fs.Path.DirectorySeparatorChar, '/' }, StringSplitOptions.RemoveEmptyEntries);
        int strip = 0;
        int projIdx = projectSegments.Length - 1;
        while (projIdx >= 0 && strip < nameSegments.Length &&
               string.Equals(projectSegments[projIdx], nameSegments[strip], StringComparison.OrdinalIgnoreCase))
        {
            strip++;
            projIdx--;
        }
        // Never strip all segments: we must keep at least one (file/folder name) so the result is a valid path
        if (strip >= nameSegments.Length)
            strip = 0;
        string combined = _fs.Path.Combine(projectRoot, string.Join(_fs.Path.DirectorySeparatorChar.ToString(), nameSegments.Skip(strip)));
        return _fs.Path.GetFullPath(combined);
    }

    /// <summary>
    /// For Save*: fileName can be a bare name (e.g. Player.cs) or path. If bare name, use projectPath/Assets/defaultSubfolder/fileName;
    /// if path already has segments, combine with projectPath without duplicating (Assets/Scripts appear only once).
    /// </summary>
    private string ResolveAssetPath(string projectPath, string fileName, string defaultSubfolder)
    {
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "unnamed";
        fileName = fileName.Trim().Replace('/', _fs.Path.DirectorySeparatorChar);
        string projectRoot = _fs.Path.GetFullPath(projectPath ?? "").TrimEnd(_fs.Path.DirectorySeparatorChar, '/');

        bool hasDirSep = fileName.Contains(_fs.Path.DirectorySeparatorChar);
        if (hasDirSep)
            return ResolvePath(projectPath ?? "", fileName);

        // Plain filename: projectRoot/Assets/defaultSubfolder/fileName, but don't duplicate if projectRoot already ends with Assets or Assets/defaultSubfolder
        string defaultPrefix = _fs.Path.Combine("Assets", defaultSubfolder);
        string projectRootNorm = projectRoot.Replace('/', _fs.Path.DirectorySeparatorChar);
        string defaultPrefixNorm = defaultPrefix.Replace('/', _fs.Path.DirectorySeparatorChar);
        if (projectRootNorm.EndsWith(defaultPrefixNorm, StringComparison.OrdinalIgnoreCase))
            return _fs.Path.GetFullPath(_fs.Path.Combine(projectRoot, fileName));
        if (projectRootNorm.EndsWith("Assets", StringComparison.OrdinalIgnoreCase))
            return _fs.Path.GetFullPath(_fs.Path.Combine(projectRoot, defaultSubfolder, fileName));
        return _fs.Path.GetFullPath(_fs.Path.Combine(projectRoot, "Assets", defaultSubfolder, fileName));
    }

    // -----------------------------------------------------------------------
    // Utility
    // -----------------------------------------------------------------------

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
