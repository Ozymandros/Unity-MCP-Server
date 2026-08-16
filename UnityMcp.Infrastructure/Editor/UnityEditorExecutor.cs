using System.IO.Pipes;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using UnityMcp.Core.Contracts;
using UnityMcp.Core.Interfaces;
using UnityMcp.Infrastructure.Services;

namespace UnityMcp.Infrastructure.Editor;

/// <summary>
/// Hybrid Unity Editor executor: live localhost bridge when available, otherwise batch mode.
/// </summary>
public sealed class UnityEditorExecutor : IUnityEditorExecutor
{
    private readonly ILogger<UnityEditorExecutor> _logger;
    private readonly IProcessRunner _processRunner;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public UnityEditorExecutor(ILogger<UnityEditorExecutor> logger, IProcessRunner processRunner)
    {
        _logger = logger;
        _processRunner = processRunner;
    }

    public string? FindUnityExecutable()
    {
        var envPath = Environment.GetEnvironmentVariable("UNITY_EDITOR_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
            return envPath;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var programFiles = Environment.GetEnvironmentVariable("ProgramFiles")
                ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrEmpty(programFiles))
            {
                var hubEditor = Path.Combine(programFiles, "Unity", "Hub", "Editor");
                if (Directory.Exists(hubEditor))
                {
                    foreach (var versionDir in Directory.EnumerateDirectories(hubEditor).OrderByDescending(d => d))
                    {
                        var unityExe = Path.Combine(versionDir, "Editor", "Unity.exe");
                        if (File.Exists(unityExe))
                            return unityExe;
                    }
                }
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var hubEditor = Path.Combine("/", "Applications", "Unity", "Hub", "Editor");
            if (Directory.Exists(hubEditor))
            {
                foreach (var versionDir in Directory.EnumerateDirectories(hubEditor).OrderByDescending(d => d))
                {
                    var unityApp = Path.Combine(versionDir, "Unity.app", "Contents", "MacOS", "Unity");
                    if (File.Exists(unityApp))
                        return unityApp;
                }
            }
        }
        else
        {
            var linuxUnity = Path.Combine("/", "usr", "bin", "unity");
            if (File.Exists(linuxUnity))
                return linuxUnity;
            var home = Environment.GetEnvironmentVariable("HOME")
                ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(home))
            {
                var hubEditor = Path.Combine(home, "Unity", "Hub", "Editor");
                if (Directory.Exists(hubEditor))
                {
                    foreach (var versionDir in Directory.EnumerateDirectories(hubEditor).OrderByDescending(d => d))
                    {
                        var unityExe = Path.Combine(versionDir, "Editor", "Unity");
                        if (File.Exists(unityExe))
                            return unityExe;
                    }
                }
            }
        }

        return null;
    }

    public bool TryGetLiveBridgeStatus(string projectPath, out bool connected, out string? unityVersion)
    {
        connected = false;
        unityVersion = null;
        try
        {
            var bridge = TryReadBridge(projectPath);
            if (bridge is null)
                return false;
            unityVersion = bridge.UnityVersion;
            connected = !string.IsNullOrWhiteSpace(bridge.Endpoint) && !string.IsNullOrWhiteSpace(bridge.Token);
            return connected;
        }
        catch
        {
            return false;
        }
    }

    public async Task EnsureBridgeInstalledAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        string projectRoot = Path.GetFullPath(projectPath);
        string destination = Path.Combine(projectRoot, "Packages", "com.unitymcp.bridge");
        string source = LocateBridgeSource();
        if (string.IsNullOrEmpty(source) || !Directory.Exists(source))
            throw new DirectoryNotFoundException(
                "UnityEditorBridge package source not found. Set UNITY_MCP_BRIDGE_PATH or keep UnityEditorBridge next to the server repository.");

        Directory.CreateDirectory(Path.Combine(projectRoot, "Packages"));
        CopyDirectory(source, destination);
        await EnsureManifestPackageReferenceAsync(projectRoot, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Installed Unity MCP bridge into {Destination}", destination);
    }

    public async Task<string> ExecuteAsync(
        string projectPath,
        string operation,
        IReadOnlyDictionary<string, object?>? args = null,
        CancellationToken cancellationToken = default)
    {
        string projectRoot = Path.GetFullPath(projectPath);
        await EnsureBridgeInstalledAsync(projectRoot, cancellationToken).ConfigureAwait(false);

        var request = new Dictionary<string, object?>
        {
            ["op"] = operation,
            ["requestId"] = Guid.NewGuid().ToString("N"),
            ["args"] = args ?? new Dictionary<string, object?>(),
        };

        if (TryGetLiveBridgeStatus(projectRoot, out bool connected, out _) && connected)
        {
            try
            {
                var bridge = TryReadBridge(projectRoot)!;
                request["token"] = bridge.Token;
                return await ExecuteLiveAsync(bridge, request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Live bridge failed; falling back to batch mode.");
            }
        }

        string? editor = FindUnityExecutable();
        if (editor is null)
        {
            return JsonSerializer.Serialize(new ToolResultEnvelope<object>
            {
                Success = false,
                Message = "Unity Editor is required. Set UNITY_EDITOR_PATH or open the project in Unity with the bridge installed.",
                Errors =
                [
                    new UnityMcpError
                    {
                        Category = UnityMcpErrorCategory.ExternalTool,
                        Code = "Editor.Unavailable",
                        Message = "Unity Editor executable not found and no live bridge is connected.",
                    }
                ],
                SuggestedRemediation = "Install Unity, set UNITY_EDITOR_PATH, or open the project so the live bridge can start.",
            });
        }

        return await ExecuteBatchAsync(projectRoot, editor, request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> ExecuteBatchAsync(
        string projectRoot,
        string editorPath,
        Dictionary<string, object?> request,
        CancellationToken cancellationToken)
    {
        string tempDir = Path.Combine(projectRoot, "Temp", "UnityMcp");
        Directory.CreateDirectory(tempDir);
        string requestPath = Path.Combine(tempDir, "request.json");
        string resultPath = Path.Combine(tempDir, "result.json");
        string logPath = Path.Combine(tempDir, "batch.log");
        if (File.Exists(resultPath))
            File.Delete(resultPath);

        await File.WriteAllTextAsync(requestPath, JsonSerializer.Serialize(request), cancellationToken).ConfigureAwait(false);

        string arguments = string.Join(" ",
            "-quit", "-batchmode", "-nographics",
            $"-projectPath \"{projectRoot}\"",
            "-executeMethod UnityMcp.Bridge.BatchRunner.Run",
            $"-logFile \"{logPath}\"");

        int exitCode = await _processRunner.RunAsync(editorPath, arguments, cancellationToken).ConfigureAwait(false);
        if (File.Exists(resultPath))
            return await File.ReadAllTextAsync(resultPath, cancellationToken).ConfigureAwait(false);

        return JsonSerializer.Serialize(new ToolResultEnvelope<object>
        {
            Success = false,
            Message = "Unity batch execution finished without a result file.",
            Errors =
            [
                new UnityMcpError
                {
                    Category = UnityMcpErrorCategory.ExternalTool,
                    Code = "Batch.NoResult",
                    Message = $"Unity exited with code {exitCode} before writing Temp/UnityMcp/result.json.",
                }
            ],
            SuggestedRemediation = "Inspect Temp/UnityMcp/batch.log and ensure the com.unitymcp.bridge package is installed.",
        });
    }

    private static async Task<string> ExecuteLiveAsync(
        BridgeInfo bridge,
        Dictionary<string, object?> request,
        CancellationToken cancellationToken)
    {
        string payload = JsonSerializer.Serialize(request) + "\n";
        if (string.Equals(bridge.Transport, "namedpipe", StringComparison.OrdinalIgnoreCase))
        {
            await using var client = new NamedPipeClientStream(".", bridge.Endpoint!, PipeDirection.InOut, PipeOptions.Asynchronous);
            await client.ConnectAsync(5000, cancellationToken).ConfigureAwait(false);
            await using var writer = new StreamWriter(client, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(client, Encoding.UTF8, leaveOpen: true);
            await writer.WriteAsync(payload.AsMemory(), cancellationToken).ConfigureAwait(false);
            string? response = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            return response ?? throw new InvalidOperationException("Empty live bridge response.");
        }

        string[] parts = bridge.Endpoint!.Split(':');
        string host = parts[0];
        int port = int.Parse(parts[1]);
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
        await using var stream = tcp.GetStream();
        await using var tcpWriter = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        using var tcpReader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        await tcpWriter.WriteAsync(payload.AsMemory(), cancellationToken).ConfigureAwait(false);
        string? tcpResponse = await tcpReader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        return tcpResponse ?? throw new InvalidOperationException("Empty live bridge response.");
    }

    private static BridgeInfo? TryReadBridge(string projectPath)
    {
        string path = Path.Combine(Path.GetFullPath(projectPath), "Temp", "UnityMcp", "bridge.json");
        if (!File.Exists(path))
            return null;
        // Consider stale if older than 24h — Editor may have crashed without cleanup.
        if (DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > TimeSpan.FromHours(24))
            return null;
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<BridgeInfo>(json, JsonOpts);
    }

    private static string LocateBridgeSource()
    {
        var env = Environment.GetEnvironmentVariable("UNITY_MCP_BRIDGE_PATH");
        if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env))
            return env;

        // Walk up from the executing assembly to find the repo's UnityEditorBridge folder.
        string? dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
        {
            string candidate = Path.Combine(dir, "UnityEditorBridge");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "package.json")))
                return candidate;
            dir = Directory.GetParent(dir)?.FullName;
        }

        // Common repo layout when running from bin/
        string cwd = Directory.GetCurrentDirectory();
        string cwdCandidate = Path.Combine(cwd, "UnityEditorBridge");
        if (Directory.Exists(cwdCandidate))
            return cwdCandidate;

        return string.Empty;
    }

    private static async Task EnsureManifestPackageReferenceAsync(string projectRoot, CancellationToken cancellationToken)
    {
        string manifestPath = Path.Combine(projectRoot, "Packages", "manifest.json");
        if (!File.Exists(manifestPath))
        {
            Directory.CreateDirectory(Path.Combine(projectRoot, "Packages"));
            await File.WriteAllTextAsync(manifestPath, """
{
  "dependencies": {
    "com.unitymcp.bridge": "file:com.unitymcp.bridge"
  }
}
""", cancellationToken).ConfigureAwait(false);
            return;
        }

        string content = await File.ReadAllTextAsync(manifestPath, cancellationToken).ConfigureAwait(false);
        if (content.Contains("com.unitymcp.bridge", StringComparison.OrdinalIgnoreCase))
            return;

        // Prefer file: reference to the embedded package folder.
        if (content.Contains("\"dependencies\"", StringComparison.Ordinal))
        {
            int insertAt = content.IndexOf('{', content.IndexOf("\"dependencies\"", StringComparison.Ordinal));
            if (insertAt >= 0)
            {
                content = content.Insert(insertAt + 1, "\n    \"com.unitymcp.bridge\": \"file:com.unitymcp.bridge\",");
                await File.WriteAllTextAsync(manifestPath, content, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(source, file);
            string target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private sealed class BridgeInfo
{
    public string? Transport { get; init; }
    public string? Endpoint { get; init; }
    public string? Token { get; init; }
    public string? UnityVersion { get; init; }
}
}
