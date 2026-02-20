using System;
using System.Collections.Generic;
using System.IO;
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
    private string? _projectPath;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public FileUnityService(ILogger<FileUnityService> logger, IProcessRunner processRunner)
    {
        _logger = logger;
        _processRunner = processRunner;
    }

    public Task<bool> IsValidProjectAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        _projectPath = projectPath;
        bool isValid = Directory.Exists(Path.Combine(projectPath, "Assets"));
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
        await File.WriteAllTextAsync(relativePath, yaml, cancellationToken);
        await MetaFileWriter.WriteDefaultMetaAsync(relativePath, ct: cancellationToken);
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
        await File.WriteAllTextAsync(relativePath, scriptContent, cancellationToken);
        await MetaFileWriter.WriteScriptMetaAsync(relativePath, ct: cancellationToken);
        _logger.LogInformation("Script created at {Path} (with .meta)", relativePath);
    }

    public Task<IEnumerable<string>> ListAssetsAsync(string relativePath, string searchPattern = "*", CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(relativePath))
        {
            _logger.LogWarning("Directory not found: {Path}", relativePath);
            return Task.FromResult(Enumerable.Empty<string>());
        }

        var files = Directory.EnumerateFiles(relativePath, searchPattern, SearchOption.AllDirectories)
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
        await File.WriteAllTextAsync(relativePath, content, cancellationToken);
        await MetaFileWriter.WriteDefaultMetaAsync(relativePath, ct: cancellationToken);
        _logger.LogInformation("Asset created at {Path} (with .meta)", relativePath);
    }

    public async Task CreateGameObjectAsync(string scenePath, string gameObjectName, CancellationToken cancellationToken = default)
    {
        // Append a simple GameObject to an existing scene file
        var go = new GameObjectDef { Name = gameObjectName };
        string fragment = UnityYamlWriter.WriteGameObjectFragment(go);
        await File.AppendAllTextAsync(scenePath, fragment, cancellationToken);
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
        await File.WriteAllTextAsync(relativePath, yaml, cancellationToken);
        _logger.LogInformation("Detailed scene created at {Path} with {Count} GameObjects", relativePath, defs.Count);
    }

    public async Task AddGameObjectToSceneAsync(string scenePath, string gameObjectJson, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(scenePath))
            throw new FileNotFoundException($"Scene file not found: {scenePath}");

        var go = DeserializeGameObject(gameObjectJson);
        string fragment = UnityYamlWriter.WriteGameObjectFragment(go);
        await File.AppendAllTextAsync(scenePath, fragment, cancellationToken);
        _logger.LogInformation("GameObject {Name} added to scene {Scene}", go.Name, scenePath);
    }

    public async Task CreateMaterialAsync(string relativePath, string materialJson, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists(relativePath);

        var matDef = JsonSerializer.Deserialize<MaterialDef>(materialJson, JsonOpts)
            ?? new MaterialDef();

        UnityYamlWriter.ResetFileIdCounter();
        string yaml = UnityYamlWriter.WriteMaterial(matDef);
        await File.WriteAllTextAsync(relativePath, yaml, cancellationToken);
        _logger.LogInformation("Material created at {Path}", relativePath);
    }

    public async Task CreatePrefabAsync(string relativePath, string prefabJson, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists(relativePath);

        var go = DeserializeGameObject(prefabJson);
        UnityYamlWriter.ResetFileIdCounter();
        string yaml = UnityYamlWriter.WritePrefab(go);
        await File.WriteAllTextAsync(relativePath, yaml, cancellationToken);
        _logger.LogInformation("Prefab created at {Path}", relativePath);
    }

    public Task<string> ReadAssetAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(relativePath))
            throw new FileNotFoundException($"File not found: {relativePath}");

        return File.ReadAllTextAsync(relativePath, cancellationToken);
    }

    public Task DeleteAssetAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (File.Exists(relativePath))
        {
            File.Delete(relativePath);
            _logger.LogInformation("Deleted asset at {Path}", relativePath);

            // Also delete .meta sidecar if it exists
            string metaPath = relativePath + ".meta";
            if (File.Exists(metaPath))
                File.Delete(metaPath);
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

    // -----------------------------------------------------------------------
    // Project scaffolding & management
    // -----------------------------------------------------------------------

    public async Task<string> ScaffoldProjectAsync(string projectName, string? outputRoot = null, string? unityVersion = null, CancellationToken cancellationToken = default)
    {
        string root = outputRoot ?? Path.Combine(Directory.GetCurrentDirectory(), "output");
        string safeName = System.Text.RegularExpressions.Regex.Replace(projectName, @"[^a-zA-Z0-9_\-]", "_").Trim('_');
        if (string.IsNullOrEmpty(safeName)) safeName = "UnityProject";

        string projectDir = Path.Combine(root, safeName);

        // Standard Unity folder structure
        string[] folders = {
            projectDir,
            Path.Combine(projectDir, "Assets"),
            Path.Combine(projectDir, "Assets", "Scripts"),
            Path.Combine(projectDir, "Assets", "Scenes"),
            Path.Combine(projectDir, "Assets", "Prefabs"),
            Path.Combine(projectDir, "Assets", "Materials"),
            Path.Combine(projectDir, "Assets", "Textures"),
            Path.Combine(projectDir, "Assets", "Audio"),
            Path.Combine(projectDir, "Assets", "Text"),
            Path.Combine(projectDir, "ProjectSettings"),
            Path.Combine(projectDir, "Packages"),
        };

        foreach (var folder in folders)
        {
            Directory.CreateDirectory(folder);
            await MetaFileWriter.WriteFolderMetaAsync(folder, ct: cancellationToken);
        }

        // ProjectVersion.txt
        string version = unityVersion ?? "2022.3.0f1";
        string versionFile = Path.Combine(projectDir, "ProjectSettings", "ProjectVersion.txt");
        if (!File.Exists(versionFile))
        {
            await File.WriteAllTextAsync(versionFile,
                $"m_EditorVersion: {version}\nm_EditorVersionWithRevision: {version} (placeholder)\n",
                cancellationToken);
        }

        // Packages/manifest.json
        string manifestPath = Path.Combine(projectDir, "Packages", "manifest.json");
        if (!File.Exists(manifestPath))
        {
            string manifest = "{\n  \"dependencies\": {}\n}";
            await File.WriteAllTextAsync(manifestPath, manifest, cancellationToken);
        }

        // README
        string readmePath = Path.Combine(projectDir, "README.txt");
        if (!File.Exists(readmePath))
        {
            await File.WriteAllTextAsync(readmePath,
                $"Generated by Unity MCP Server.\nProject: {projectName}\nImport this folder as a Unity project.\n",
                cancellationToken);
            await MetaFileWriter.WriteDefaultMetaAsync(readmePath, ct: cancellationToken);
        }

        _logger.LogInformation("Project scaffolded at {Path}", projectDir);
        return projectDir;
    }

    public Task<string> GetProjectInfoAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        string name = Path.GetFileName(projectPath);
        string versionPath = Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt");
        string unityVersion = "unknown";

        if (File.Exists(versionPath))
        {
            string content = File.ReadAllText(versionPath);
            var match = System.Text.RegularExpressions.Regex.Match(content, @"m_EditorVersion:\s*(\S+)");
            if (match.Success) unityVersion = match.Groups[1].Value;
        }

        bool hasAssets = Directory.Exists(Path.Combine(projectPath, "Assets"));

        var info = JsonSerializer.Serialize(new
        {
            projectName = name,
            projectPath = Path.GetFullPath(projectPath),
            unityVersion,
            hasAssets,
        });

        return Task.FromResult(info);
    }

    public async Task CreateFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(folderPath);
        await MetaFileWriter.WriteFolderMetaAsync(folderPath, ct: cancellationToken);
        _logger.LogInformation("Folder created at {Path} (with .meta)", folderPath);
    }

    // -----------------------------------------------------------------------
    // Typed asset saving (with correct .meta sidecars)
    // -----------------------------------------------------------------------

    public async Task SaveScriptAsync(string projectPath, string fileName, string content, CancellationToken cancellationToken = default)
    {
        string dir = Path.Combine(projectPath, "Assets", "Scripts");
        Directory.CreateDirectory(dir);
        string filePath = Path.Combine(dir, fileName);
        await File.WriteAllTextAsync(filePath, content, cancellationToken);
        await MetaFileWriter.WriteScriptMetaAsync(filePath, ct: cancellationToken);
        _logger.LogInformation("Script saved at {Path} (with MonoImporter .meta)", filePath);
    }

    public async Task SaveTextAssetAsync(string projectPath, string fileName, string content, CancellationToken cancellationToken = default)
    {
        string dir = Path.Combine(projectPath, "Assets", "Text");
        Directory.CreateDirectory(dir);
        string filePath = Path.Combine(dir, fileName);
        await File.WriteAllTextAsync(filePath, content, cancellationToken);
        await MetaFileWriter.WriteDefaultMetaAsync(filePath, ct: cancellationToken);
        _logger.LogInformation("Text asset saved at {Path} (with .meta)", filePath);
    }

    public async Task SaveTextureAsync(string projectPath, string fileName, string base64Data, CancellationToken cancellationToken = default)
    {
        string dir = Path.Combine(projectPath, "Assets", "Textures");
        Directory.CreateDirectory(dir);
        string filePath = Path.Combine(dir, fileName);
        byte[] data = Convert.FromBase64String(base64Data);
        await File.WriteAllBytesAsync(filePath, data, cancellationToken);
        await MetaFileWriter.WriteTextureMetaAsync(filePath, ct: cancellationToken);
        _logger.LogInformation("Texture saved at {Path} (with TextureImporter .meta)", filePath);
    }

    public async Task SaveAudioAsync(string projectPath, string fileName, string base64Data, CancellationToken cancellationToken = default)
    {
        string dir = Path.Combine(projectPath, "Assets", "Audio");
        Directory.CreateDirectory(dir);
        string filePath = Path.Combine(dir, fileName);
        byte[] data = Convert.FromBase64String(base64Data);
        await File.WriteAllBytesAsync(filePath, data, cancellationToken);
        await MetaFileWriter.WriteAudioMetaAsync(filePath, ct: cancellationToken);
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
        string manifestPath = Path.Combine(projectPath, "Packages", "manifest.json");

        // Read existing manifest or create new
        JsonDocument existingDoc;
        if (File.Exists(manifestPath))
        {
            string existing = await File.ReadAllTextAsync(manifestPath, cancellationToken);
            existingDoc = JsonDocument.Parse(existing);
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
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
        await File.WriteAllTextAsync(manifestPath, output, cancellationToken);
        _logger.LogInformation("Updated manifest.json with {Count} packages at {Path}", merged.Count, manifestPath);
    }

    // -----------------------------------------------------------------------
    // Utility
    // -----------------------------------------------------------------------

    private static void EnsureDirectoryExists(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    private static string FindUnityExecutable()
    {
        var envPath = Environment.GetEnvironmentVariable("UNITY_EDITOR_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
            return envPath;

        string[] candidates =
        [
            @"C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe",
            @"/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity",
            @"/usr/bin/unity"
        ];

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate)) return candidate;
        }

        throw new FileNotFoundException(
            "Unity Editor executable not found. Set UNITY_EDITOR_PATH environment variable.");
    }
}
