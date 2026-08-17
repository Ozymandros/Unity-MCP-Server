using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Bridge
{
    /// <summary>
    /// Live Editor bridge. Writes Temp/UnityMcp/bridge.json and accepts localhost commands.
    /// </summary>
    [InitializeOnLoad]
    public static class LiveBridge
    {
        private static CancellationTokenSource _cts;
        private static string _token;
        private static string _endpoint;
        private static string _transport;

        public static string Token => _token;
        public static bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

        static LiveBridge()
        {
            try
            {
                Start();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[UnityMcp.Bridge] Live bridge failed to start: " + ex.Message);
            }
        }

        private static void Start()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            string tempDir = Path.Combine(projectRoot, "Temp", "UnityMcp");
            Directory.CreateDirectory(tempDir);

            _token = Guid.NewGuid().ToString("N");
            _cts = new CancellationTokenSource();

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                string pipeName = "UnityMcp-" + Math.Abs(projectRoot.GetHashCode()).ToString("X");
                _transport = "namedpipe";
                _endpoint = pipeName;
                WriteBridgeFile(tempDir);
                Task.Run(() => RunNamedPipeLoop(pipeName, _cts.Token));
            }
            else
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                _transport = "tcp";
                _endpoint = "127.0.0.1:" + port;
                WriteBridgeFile(tempDir);
                Task.Run(() => RunTcpLoop(listener, _cts.Token));
            }
        }

        private static void WriteBridgeFile(string tempDir)
        {
            string path = Path.Combine(tempDir, "bridge.json");
            string json =
                "{"
                + "\"transport\":\"" + Escape(_transport) + "\","
                + "\"endpoint\":\"" + Escape(_endpoint) + "\","
                + "\"token\":\"" + Escape(_token) + "\","
                + "\"unityVersion\":\"" + Escape(Application.unityVersion) + "\","
                + "\"pid\":" + System.Diagnostics.Process.GetCurrentProcess().Id
                + "}";
            File.WriteAllText(path, json);
        }

        private static async Task RunNamedPipeLoop(string pipeName, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await server.WaitForConnectionAsync(ct).ConfigureAwait(false);
                    await HandleStreamAsync(server, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[UnityMcp.Bridge] Named pipe error: " + ex.Message);
                    await Task.Delay(250, ct).ConfigureAwait(false);
                }
            }
        }

        private static async Task RunTcpLoop(TcpListener listener, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    using (client)
                    using (var stream = client.GetStream())
                    {
                        await HandleStreamAsync(stream, ct).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[UnityMcp.Bridge] TCP error: " + ex.Message);
                    await Task.Delay(250, ct).ConfigureAwait(false);
                }
            }
        }

        private static async Task HandleStreamAsync(Stream stream, CancellationToken ct)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, leaveOpen: true);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true) { AutoFlush = true };
            string request = await reader.ReadLineAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(request))
                return;

            string result = null;
            var reset = new ManualResetEventSlim(false);
            EditorApplication.delayCall += () =>
            {
                try
                {
                    result = CommandDispatcher.Dispatch(request, requireToken: true, expectedToken: _token);
                }
                catch (Exception ex)
                {
                    result = CommandDispatcher.Fail("Live.Exception", ex.Message);
                }
                finally
                {
                    reset.Set();
                }
            };

            while (!reset.IsSet && !ct.IsCancellationRequested)
                await Task.Delay(15, ct).ConfigureAwait(false);

            await writer.WriteLineAsync(result ?? CommandDispatcher.Fail("Live.Timeout", "Editor main thread did not respond.")).ConfigureAwait(false);
        }

        private static string Escape(string value) =>
            (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
