using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Infrastructure.Services;

/// <summary>
/// Implementation of IUnityService that uses native UnityEditor APIs.
/// This replaces the previous file-system-only implementation with a strict Unity SDK wrapper.
/// </summary>
public class FileUnityService : IUnityService
{
    private readonly ILogger<FileUnityService> _logger;
    private string? _projectPath;

    public FileUnityService(ILogger<FileUnityService> logger)
    {
        _logger = logger;
    }

    public Task<bool> IsValidProjectAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        _projectPath = projectPath;
        // In a real Unity environment, Application.dataPath would be relevant, 
        // but here we check existence on disk to confirm we are in a valid project context.
        bool isValid = Directory.Exists(Path.Combine(projectPath, "Assets"));
        return Task.FromResult(isValid);
    }

    public Task CreateSceneAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        // STRICT UNITY WRAPPER: Matches user's "MinimalCreateScene" example exactly
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // Save to disk
        bool ok = EditorSceneManager.SaveScene(scene, relativePath);
        
        if (!ok)
        {
            throw new Exception($"No s'ha pogut guardar la escena a: {relativePath}");
        }
        
        _logger.LogInformation("Escena creada correctament a {Path}", relativePath);
        return Task.CompletedTask;
    }

    public async Task CreateScriptAsync(string relativePath, string scriptName, CancellationToken cancellationToken = default)
    {
        // STRICT UNITY WRAPPER: No path expansion or directory creation helpers
        string scriptContent = $@"using UnityEngine;

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
        
        // Trigger import in Unity
        AssetDatabase.ImportAsset(relativePath);
        AssetDatabase.Refresh();
        
        _logger.LogInformation("Created script at {Path} and triggered AssetDatabase.Refresh", relativePath);
    }

    public Task<IEnumerable<string>> ListAssetsAsync(string relativePath, string searchPattern = "*", CancellationToken cancellationToken = default)
    {
        // STRICT UNITY IMPLEMENTATION: Use AssetDatabase.FindAssets with folder scope
        // This matches the user's specific request for a "Wrapper" style
        // "t:Object" ensures we get all assets (Types: Object) within the path
        string[] guids = AssetDatabase.FindAssets("t:Object", new[] { relativePath });
        
        var results = new List<string>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            
            // Allow simplified wildcard matching or simple string containment
            // If pattern is "*", add everything. Otherwise check containment.
            bool match = searchPattern == "*" || 
                         path.Contains(searchPattern, StringComparison.OrdinalIgnoreCase);

            if (match)
            {
                results.Add(path);
            }
        }

        return Task.FromResult((IEnumerable<string>)results);
    }

    public Task BuildProjectAsync(string buildTarget, string outputPath, CancellationToken cancellationToken = default)
    {
        var buildOptions = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = outputPath,
            target = ParseBuildTarget(buildTarget),
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(buildOptions);
        
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new Exception($"Build failed: {report.summary.result}");
        }

        _logger.LogInformation("Build completed successfully for {Target}", buildTarget);
        return Task.CompletedTask;
    }

    public async Task CreateAssetAsync(string relativePath, string content, CancellationToken cancellationToken = default)
    {
        // STRICT UNITY WRAPPER: Direct file write then import
        await File.WriteAllTextAsync(relativePath, content, cancellationToken);
        
        AssetDatabase.ImportAsset(relativePath);
        
        _logger.LogInformation("Created asset at {Path}", relativePath);
    }

    public Task CreateGameObjectAsync(string scenePath, string gameObjectName, CancellationToken cancellationToken = default)
    {
        // STRICT UNITY WRAPPER: Open scene, new GameObject, Save
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var go = new GameObject(gameObjectName);
        EditorSceneManager.SaveScene(scene);
        
        _logger.LogInformation("Created GameObject {Name} in {Scene}", gameObjectName, scenePath);
        return Task.CompletedTask;
    }

    private BuildTarget ParseBuildTarget(string target)
    {
        return target.ToLowerInvariant() switch
        {
            "win64" => BuildTarget.StandaloneWindows64,
            "osx" => BuildTarget.StandaloneOSX,
            "linux" => BuildTarget.StandaloneLinux64,
            "android" => BuildTarget.Android,
            "ios" => BuildTarget.iOS,
            _ => BuildTarget.StandaloneWindows64
        };
    }

    // Removed EnsureAssetDirectoryExists and GetFullPath as per strict wrapper requirement
}
