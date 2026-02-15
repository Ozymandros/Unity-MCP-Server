using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace StandaloneMCP
{
    /// <summary>
    /// Standalone MCP server implementing a JSON-RPC 2.0 TCP server for Model Context Protocol.
    /// </summary>
    public class StandaloneMcpServer
    {
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private bool _isRunning;
        private readonly int _port;

        /// <summary>
        /// Initializes a new instance of the <see cref="StandaloneMcpServer"/> class.
        /// </summary>
        /// <param name="port">The TCP port to listen on. Defaults to 8765.</param>
        public StandaloneMcpServer(int port = 8765)
        {
            _port = port;
        }

        /// <summary>
        /// Starts the MCP server and begins listening for client connections.
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();
            _isRunning = true;
            Console.WriteLine($"[StandaloneMcpServer] Started on port {_port}");
            Task.Run(() => AcceptClientsAsync(_cts.Token));
        }

        /// <summary>
        /// Stops the MCP server and disconnects all clients.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;
            _cts.Cancel();
            _listener.Stop();
            _isRunning = false;
            Console.WriteLine("[StandaloneMcpServer] Stopped");
        }

        /// <summary>
        /// Accepts incoming TCP clients asynchronously.
        /// </summary>
        /// <param name="token">Cancellation token to stop accepting clients.</param>
        private async Task AcceptClientsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine($"[StandaloneMcpServer] Client connected: {client.Client.RemoteEndPoint}");
                    _ = Task.Run(() => HandleClientAsync(client, token), token);
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex) { Console.WriteLine($"[StandaloneMcpServer] Accept error: {ex.Message}"); }
            }
        }

        /// <summary>
        /// Handles communication with a connected client.
        /// </summary>
        /// <param name="client">The connected TCP client.</param>
        /// <param name="token">Cancellation token to stop handling the client.</param>
        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                var buffer = new byte[8192];
                var sb = new StringBuilder();
                while (!token.IsCancellationRequested && client.Connected)
                {
                    int bytesRead = 0;
                    try { bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token); }
                    catch { break; }
                    if (bytesRead == 0) break;
                    string received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    sb.Append(received);
                    string full = sb.ToString();
                    int nl = full.IndexOf('\n');
                    while (nl >= 0)
                    {
                        string json = full.Substring(0, nl).Trim();
                        full = full.Substring(nl + 1);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            Console.WriteLine($"[StandaloneMcpServer] Received: {json}");
                            string response = HandleJsonRpc(json);
                            await stream.WriteAsync(Encoding.UTF8.GetBytes(response + "\n"), token);
                        }
                        nl = full.IndexOf('\n');
                    }
                    sb.Clear();
                    sb.Append(full);
                }
            }
            Console.WriteLine("[StandaloneMcpServer] Client disconnected");
        }

        /// <summary>
        /// Handles a single JSON-RPC request and returns the response JSON.
        /// </summary>
        /// <param name="json">The JSON-RPC request string.</param>
        /// <returns>JSON-RPC response string.</returns>
        private string HandleJsonRpc(string json)
        {
            try
            {
                var doc = JsonNode.Parse(json);
                string method = doc?["method"]?.ToString() ?? "";
                var id = doc?["id"];
                if (method == "ping")
                {
                    return JsonSerializer.Serialize(new { jsonrpc = "2.0", id, result = new { message = "pong" } });
                }
                return JsonSerializer.Serialize(new { jsonrpc = "2.0", id, error = new { code = -32601, message = "Method not found" } });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { jsonrpc = "2.0", id = (string?)null, error = new { code = -32700, message = "Parse error", data = ex.Message } });
            }
        }
    }
}
