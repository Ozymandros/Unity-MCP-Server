using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

    public async Task CreateSceneAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists(relativePath);

        // Create a default scene with camera and directional light
        var gameObjects = new List<GameObjectDef>
        {
            CreateDefaultCamera(),
            CreateDefaultLight(),
        };

        UnityYamlWriter.ResetFileIdCounter();
        string yaml = UnityYamlWriter.WriteScene(gameObjects);
        await _fs.File.WriteAllTextAsync(relativePath, yaml, cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(relativePath, ct: cancellationToken);
        _logger.LogInformation("Scene created at {Path}", relativePath);
    }

    public async Task CreateScriptAsync(string relativePath, string scriptName, string? content = null, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists(relativePath);

        // If the AI provided full script content, use it directly.
        // Otherwise fall back to a default MonoBehaviour template.
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
        await _fs.File.WriteAllTextAsync(relativePath, scriptContent, cancellationToken);
        await _metaWriter.WriteScriptMetaAsync(relativePath, ct: cancellationToken);
        _logger.LogInformation("Script created at {Path} (with .meta)", relativePath);
    }

    public Task<IEnumerable<string>> ListAssetsAsync(string relativePath, string searchPattern = "*", CancellationToken cancellationToken = default)
    {
        if (!_fs.Directory.Exists(relativePath))
        {
            _logger.LogWarning("Directory not found: {Path}", relativePath);
            return Task.FromResult(Enumerable.Empty<string>());
        }

        var files = _fs.Directory.EnumerateFiles(relativePath, searchPattern, SearchOption.AllDirectories)
            .Select(f => f.Replace('\\', '/'))
            .Where(f => !f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f)
            .ToList();

        return Task.FromResult((IEnumerable<string>)files);
    }

    public async Task BuildProjectAsync(string buildTarget, string outputPath, CancellationToken cancellationToken = default)
    {
        string unityExe = FindUnityExecutable();

        string arguments = string.Join(" ",
            "-quit", "-batchmode", "-nographics",
            $"-projectPath \"{_projectPath ?? "."}\"",
            $"-buildTarget {buildTarget}",
            $"-buildOutput \"{outputPath}\""
        );

        _logger.LogInformation("Starting Unity build: {Exe} {Args}", unityExe, arguments);
        int exitCode = await _processRunner.RunAsync(unityExe, arguments, cancellationToken);

        if (exitCode != 0)
            throw new Exception($"Unity build failed with exit code {exitCode}");

        _logger.LogInformation("Build completed for target {Target}", buildTarget);
    }

    public async Task CreateAssetAsync(string relativePath, string content, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists(relativePath);
        await _fs.File.WriteAllTextAsync(relativePath, content, cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(relativePath, ct: cancellationToken);
        _logger.LogInformation("Asset created at {Path} (with .meta)", relativePath);
    }

    public async Task CreateGameObjectAsync(string scenePath, string gameObjectName, CancellationToken cancellationToken = default)
    {
        // Append a simple GameObject to an existing scene file
        var go = new GameObjectDef { Name = gameObjectName };
        string fragment = UnityYamlWriter.WriteGameObjectFragment(go);
        await _fs.File.AppendAllTextAsync(scenePath, fragment, cancellationToken);
        _logger.LogInformation("GameObject {Name} appended to {Scene}", gameObjectName, scenePath);
    }

    // -----------------------------------------------------------------------
    // Enhanced AI-driven scene authoring tools
    // -----------------------------------------------------------------------

    public async Task CreateDetailedSceneAsync(string relativePath, string sceneJson, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists(relativePath);

        var defs = DeserializeGameObjects(sceneJson);
        UnityYamlWriter.ResetFileIdCounter();
        string yaml = UnityYamlWriter.WriteScene(defs);
        await _fs.File.WriteAllTextAsync(relativePath, yaml, cancellationToken);
        _logger.LogInformation("Detailed scene created at {Path} with {Count} GameObjects", relativePath, defs.Count);
    }

    public async Task AddGameObjectToSceneAsync(string scenePath, string gameObjectJson, CancellationToken cancellationToken = default)
    {
        if (!_fs.File.Exists(scenePath))
            throw new FileNotFoundException($"Scene file not found: {scenePath}");

        var go = DeserializeGameObject(gameObjectJson);
        string fragment = UnityYamlWriter.WriteGameObjectFragment(go);
        await _fs.File.AppendAllTextAsync(scenePath, fragment, cancellationToken);
        _logger.LogInformation("GameObject {Name} added to scene {Scene}", go.Name, scenePath);
    }

    public async Task CreateMaterialAsync(string relativePath, string materialJson, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists(relativePath);

        var matDef = JsonSerializer.Deserialize<MaterialDef>(materialJson, JsonOpts)
            ?? new MaterialDef();

        UnityYamlWriter.ResetFileIdCounter();
        string yaml = UnityYamlWriter.WriteMaterial(matDef);
        await _fs.File.WriteAllTextAsync(relativePath, yaml, cancellationToken);
        _logger.LogInformation("Material created at {Path}", relativePath);
    }

    public async Task CreatePrefabAsync(string relativePath, string prefabJson, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists(relativePath);

        var go = DeserializeGameObject(prefabJson);
        UnityYamlWriter.ResetFileIdCounter();
        string yaml = UnityYamlWriter.WritePrefab(go);
        await _fs.File.WriteAllTextAsync(relativePath, yaml, cancellationToken);
        _logger.LogInformation("Prefab created at {Path}", relativePath);
    }

    public Task<string> ReadAssetAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (!_fs.File.Exists(relativePath))
            throw new FileNotFoundException($"File not found: {relativePath}");

        return _fs.File.ReadAllTextAsync(relativePath, cancellationToken);
    }

    public Task DeleteAssetAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (_fs.File.Exists(relativePath))
        {
            _fs.File.Delete(relativePath);
            _logger.LogInformation("Deleted asset at {Path}", relativePath);

            // Also delete .meta sidecar if it exists
            string metaPath = relativePath + ".meta";
            if (_fs.File.Exists(metaPath))
                _fs.File.Delete(metaPath);
        }
        else
        {
            _logger.LogWarning("File not found for deletion: {Path}", relativePath);
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

    public async Task CreateFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        _fs.Directory.CreateDirectory(folderPath);
        await _metaWriter.WriteFolderMetaAsync(folderPath, ct: cancellationToken);
        _logger.LogInformation("Folder created at {Path} (with .meta)", folderPath);
    }

    // -----------------------------------------------------------------------
    // Typed asset saving (with correct .meta sidecars)
    // -----------------------------------------------------------------------

    public async Task SaveScriptAsync(string projectPath, string fileName, string content, CancellationToken cancellationToken = default)
    {
        string dir = _fs.Path.Combine(projectPath, "Assets", "Scripts");
        _fs.Directory.CreateDirectory(dir);
        string filePath = _fs.Path.Combine(dir, fileName);
        await _fs.File.WriteAllTextAsync(filePath, content, cancellationToken);
        await _metaWriter.WriteScriptMetaAsync(filePath, ct: cancellationToken);
        _logger.LogInformation("Script saved at {Path} (with MonoImporter .meta)", filePath);
    }

    public async Task SaveTextAssetAsync(string projectPath, string fileName, string content, CancellationToken cancellationToken = default)
    {
        string dir = _fs.Path.Combine(projectPath, "Assets", "Text");
        _fs.Directory.CreateDirectory(dir);
        string filePath = _fs.Path.Combine(dir, fileName);
        await _fs.File.WriteAllTextAsync(filePath, content, cancellationToken);
        await _metaWriter.WriteDefaultMetaAsync(filePath, ct: cancellationToken);
        _logger.LogInformation("Text asset saved at {Path} (with .meta)", filePath);
    }

    public async Task SaveTextureAsync(string projectPath, string fileName, string base64Data, CancellationToken cancellationToken = default)
    {
        string dir = _fs.Path.Combine(projectPath, "Assets", "Textures");
        _fs.Directory.CreateDirectory(dir);
        string filePath = _fs.Path.Combine(dir, fileName);
        byte[] data = Convert.FromBase64String(base64Data);
        await _fs.File.WriteAllBytesAsync(filePath, data, cancellationToken);
        await _metaWriter.WriteTextureMetaAsync(filePath, ct: cancellationToken);
        _logger.LogInformation("Texture saved at {Path} (with TextureImporter .meta)", filePath);
    }

    public async Task SaveAudioAsync(string projectPath, string fileName, string base64Data, CancellationToken cancellationToken = default)
    {
        string dir = _fs.Path.Combine(projectPath, "Assets", "Audio");
        _fs.Directory.CreateDirectory(dir);
        string filePath = _fs.Path.Combine(dir, fileName);
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

            var result = new Dictionary<string, object> { ["dependencies"] = merged };
            string output = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            await _fs.File.WriteAllTextAsync(manifestPath, output, cancellationToken);
            _logger.LogInformation("Installed {Count} packages at {Path}", installed.Count, manifestPath);

            return JsonSerializer.Serialize(new { success = true, installed, message = (string?)null });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "InstallPackages failed for {Path}", projectPath);
            return JsonSerializer.Serialize(new { success = false, installed = Array.Empty<string>(), message = ex.Message });
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
            return JsonSerializer.Serialize(new { success = true, scene_path = sceneRelative, prefab_path = prefabRelative, message = (string?)null });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CreateDefaultScene failed for {Path}", projectPath);
            return JsonSerializer.Serialize(new { success = false, scene_path = (string?)null, prefab_path = (string?)null, message = ex.Message });
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
                return JsonSerializer.Serialize(new { success = false, message = "No RenderPipelineAsset found in project. Add URP package and create or import a pipeline asset." });
            }

            return JsonSerializer.Serialize(new { success = true, message = (string?)null });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ConfigureUrp failed for {Path}", projectPath);
            return JsonSerializer.Serialize(new { success = false, message = ex.Message });
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

    /// <summary>
    /// Validates import (asset refresh + script compilation). File-only stub: returns success with zero counts.
    /// TODO: Run Unity in batch mode with an Editor script that performs AssetDatabase.Refresh and compilation,
    /// writes mcp_validate_result.json, then read and return its contents.
    /// </summary>
    public Task<string> ValidateImportAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(JsonSerializer.Serialize(new
        {
            success = true,
            error_count = 0,
            warning_count = 0,
            errors = Array.Empty<string>(),
            warnings = Array.Empty<string>(),
            message = (string?)"Stub: file-only server cannot run Unity compilation. Implement batch-mode validation when Unity is available."
        }));
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

    private string FindUnityExecutable()
    {
        var envPath = Environment.GetEnvironmentVariable("UNITY_EDITOR_PATH");
        if (!string.IsNullOrEmpty(envPath) && _fs.File.Exists(envPath))
            return envPath;

        string[] candidates =
        [
            @"C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe",
            @"/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity",
            @"/usr/bin/unity"
        ];

        foreach (var candidate in candidates)
        {
            if (_fs.File.Exists(candidate)) return candidate;
        }

        throw new FileNotFoundException(
            "Unity Editor executable not found. Set UNITY_EDITOR_PATH environment variable.");
    }
}
