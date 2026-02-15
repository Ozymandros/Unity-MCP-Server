using NUnit.Framework;
using StandaloneMCP;
using System.Net.Sockets;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace UnityMCP.Tests
{
    [TestFixture]
    public class StandaloneMcpServerTests
    {
        [Test]
        public async Task StartAndStopServer_Works()
        {
            var server = new StandaloneMcpServer(9876);
            server.Start();
            await Task.Delay(100); // Give time to start
            server.Stop();
            Assert.Pass();
        }

        [Test]
        public async Task PingMethod_ReturnsPong()
        {
            var server = new StandaloneMcpServer(9877);
            server.Start();
            await Task.Delay(100); // Give time to start

            using (var client = new TcpClient())
            {
                await client.ConnectAsync("127.0.0.1", 9877);
                using (var stream = client.GetStream())
                {
                    var req = new
                    {
                        jsonrpc = "2.0",
                        id = 1,
                        method = "ping"
                    };
                    string json = JsonSerializer.Serialize(req) + "\n";
                    byte[] data = Encoding.UTF8.GetBytes(json);
                    await stream.WriteAsync(data, 0, data.Length);
                    var buffer = new byte[1024];
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string resp = Encoding.UTF8.GetString(buffer, 0, read).Trim();
                    Assert.That(resp, Does.Contain("pong"));
                }
            }
            server.Stop();
        }
    }
}