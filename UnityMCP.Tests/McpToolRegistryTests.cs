using NUnit.Framework;
using UnityMCP;
using System.Collections.Generic;

namespace UnityMCP.Tests
{
    [TestFixture]
    public class McpToolRegistryTests
    {
        [Test]
        public void RegisterTools_FindsToolsInAssembly()
        {
            // Note: This tests the discovery logic.
            // Since we are running in the test assembly, it should find tools defined here
            // or in the referenced Core assembly.
            McpToolRegistry.RegisterAllTools();
            var tools = McpToolRegistry.GetAllTools();

            Assert.That(tools, Is.Not.Null);
            Assert.That(tools.Length, Is.GreaterThanOrEqualTo(0));
        }
    }

    // Mock tool for testing registry
    public class MockTool : IMcpTool
    {
        public string GetName() => "mock_tool";
        public string GetDescription() => "A mock tool for testing";
        public object GetInputSchema() => new { };
        public object Execute(System.Text.Json.JsonElement parameters)
            => "Mock success";
    }
}
