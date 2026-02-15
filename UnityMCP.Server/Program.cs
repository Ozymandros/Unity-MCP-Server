using StandaloneMCP;

class Program
{
	static async Task Main(string[] args)
	{
		var server = new StandaloneMcpServer(8765);
		server.Start();
		Console.WriteLine("Standalone MCP Server running on localhost:8765");
		await Task.Delay(-1);
	}
}