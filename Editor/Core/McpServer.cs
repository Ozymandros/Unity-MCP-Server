using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityMCP
{
    /// <summary>
    /// Main MCP server that listens for TCP connections and processes JSON-RPC messages.
    /// Runs only in Unity Editor mode.
    /// </summary>
    [InitializeOnLoad]
    public class McpServer
    {
        private static TcpListener _listener;
        private static CancellationTokenSource _cancellationTokenSource;
        private static bool _isRunning;
        private const int DEFAULT_PORT = 8765;
        private const string SERVER_NAME = "Unity MCP Server";
        private const string SERVER_VERSION = "1.0.0";

        static McpServer()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.quitting += OnEditorQuitting;
            StartServer();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Keep server running during play mode transitions
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    Debug.Log("[McpServer] Entering play mode - server continues running");
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    Debug.Log("[McpServer] In play mode - server active");
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    Debug.Log("[McpServer] Exiting play mode - server continues running");
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    Debug.Log("[McpServer] In edit mode - server active");
                    break;
            }
        }

        private static void OnEditorQuitting()
        {
            StopServer();
        }

        public static void StartServer(int port = DEFAULT_PORT)
        {
            if (_isRunning)
            {
                Debug.LogWarning("[McpServer] Server already running");
                return;
            }

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.Loopback, port);
                _listener.Start();
                _isRunning = true;

                Debug.Log($"[McpServer] {SERVER_NAME} v{SERVER_VERSION} started on port {port}");
                Debug.Log($"[McpServer] Registered {McpToolRegistry.GetAllToolNames().Length} tools");

                // Start accepting connections asynchronously
                Task.Run(() => AcceptClientsAsync(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpServer] Failed to start: {ex.Message}");
                _isRunning = false;
            }
        }

        public static void StopServer()
        {
            if (!_isRunning) return;

            try
            {
                _cancellationTokenSource?.Cancel();
                _listener?.Stop();
                _isRunning = false;

                Debug.Log("[McpServer] Stopped");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpServer] Error stopping server: {ex.Message}");
            }
        }

        public static bool IsRunning()
        {
            return _isRunning;
        }

        private static async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    Debug.Log($"[McpServer] Client connected from {client.Client.RemoteEndPoint}");

                    // Handle client in separate task to allow multiple concurrent connections
                    _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
                }
                catch (ObjectDisposedException)
                {
                    // Server was stopped
                    break;
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Debug.LogError($"[McpServer] Error accepting client: {ex.Message}");
                    }
                }
            }

            Debug.Log("[McpServer] Stopped accepting connections");
        }

        private static async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[8192];
                StringBuilder messageBuffer = new StringBuilder();

                while (!cancellationToken.IsCancellationRequested && client.Connected)
                {
                    try
                    {
                        // Read incoming message
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        if (bytesRead == 0)
                        {
                            Debug.Log("[McpServer] Client disconnected");
                            break;
                        }

                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        messageBuffer.Append(receivedData);

                        // Process complete messages (separated by newline)
                        string fullMessage = messageBuffer.ToString();
                        int newlineIndex = fullMessage.IndexOf('\n');

                        while (newlineIndex >= 0)
                        {
                            string jsonRequest = fullMessage.Substring(0, newlineIndex).Trim();
                            fullMessage = fullMessage.Substring(newlineIndex + 1);

                            if (!string.IsNullOrWhiteSpace(jsonRequest))
                            {
                                Debug.Log($"[McpServer] Received: {jsonRequest}");

                                // Parse and dispatch request
                                McpResponse response = await ProcessRequestAsync(jsonRequest);

                                // Send response back to client
                                string jsonResponse = McpMessage.SerializeResponse(response);
                                byte[] responseBytes = Encoding.UTF8.GetBytes(jsonResponse + "\n");
                                await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken);

                                Debug.Log($"[McpServer] Sent: {jsonResponse}");
                            }

                            newlineIndex = fullMessage.IndexOf('\n');
                        }

                        messageBuffer.Clear();
                        messageBuffer.Append(fullMessage);
                    }
                    catch (Exception ex)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            Debug.LogError($"[McpServer] Error handling client message: {ex.Message}");
                        }
                        break;
                    }
                }
            }

            Debug.Log("[McpServer] Client handler terminated");
        }

        private static async Task<McpResponse> ProcessRequestAsync(string jsonRequest)
        {
            try
            {
                McpRequest request = McpMessage.ParseRequest(jsonRequest);
                
                // Handle special MCP protocol methods
                if (request.Method == "initialize")
                {
                    return HandleInitialize(request);
                }
                else if (request.Method == "tools/list")
                {
                    return HandleToolsList(request);
                }
                else
                {
                    // Dispatch to registered tools
                    return await McpDispatcher.DispatchAsync(request);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpServer] Error processing request: {ex.Message}");
                return McpMessage.CreateErrorResponse(null, -32603, "Internal error", ex.Message);
            }
        }

        private static McpResponse HandleInitialize(McpRequest request)
        {
            var result = new
            {
                protocolVersion = "2025-11-25",
                serverInfo = new
                {
                    name = SERVER_NAME,
                    version = SERVER_VERSION
                },
                capabilities = new
                {
                    tools = new { }
                }
            };

            return McpMessage.CreateSuccessResponse(request.Id, result);
        }

        private static McpResponse HandleToolsList(McpRequest request)
        {
            var tools = McpToolRegistry.GetAllToolDescriptions();
            var result = new { tools };
            return McpMessage.CreateSuccessResponse(request.Id, result);
        }
    }
}
