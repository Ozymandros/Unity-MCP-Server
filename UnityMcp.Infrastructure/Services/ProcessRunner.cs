using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Infrastructure.Services;

/// <summary>
/// Service responsible for executing external processes (like Unity CLI).
/// </summary>
public class ProcessRunner : IProcessRunner
{
    private readonly ILogger<ProcessRunner> _logger;

    public ProcessRunner(ILogger<ProcessRunner> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// asynchronously runs an executable.
    /// </summary>
    /// <param name="fileName">Full path to the executable.</param>
    /// <param name="arguments">Command line arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code of the process.</returns>
    public Task<int> RunAsync(string fileName, string arguments, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<int>();
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (sender, args) => 
        {
            if (args.Data != null) _logger.LogInformation($"[Unity CLI] {args.Data}");
        };
        
        process.ErrorDataReceived += (sender, args) => 
        {
            if (args.Data != null) _logger.LogError($"[Unity CLI Error] {args.Data}");
        };

        process.Exited += (sender, args) =>
        {
            tcs.SetResult(process.ExitCode);
            process.Dispose();
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, $"Failed to start process: {fileName} {arguments}");
            tcs.SetException(ex);
        }

        return tcs.Task;
    }
}
