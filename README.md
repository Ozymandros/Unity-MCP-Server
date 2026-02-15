# Unity MCP Server

A standalone [Model Context Protocol (MCP)](https://modelcontextprotocol.io) server for Unity, built with **.NET 10.0**.

This server empowers AI assistants (like Claude Desktop) to interact with Unity projects directly. It bypasses the need for the Unity Editor to be running for many file-based operations, allowing for rapid creation and modification of Scripts, Scenes, and Assets. When necessary, it can also bridge to the Unity CLI (via `UnityEditor.dll` integration) for specialized operations.

## 🚀 Features

* **Standalone Operation**: Runs as a .NET console application, independent of the Unity Editor process.
* **Direct File Manipulation**: Fast and efficient creation of C# Scripts, Scenes (`.unity`), and other assets.
* **Typed Protocol Handler**: Generic, strictly-typed handler system using `McpHandlerBase<TParams, TResult>` for robust request processing and validation.
* **Data Validation**: Built-in validation using `System.ComponentModel.DataAnnotations` for all incoming tool parameters.
* **Clean Architecture**: Designed with strictly decoupled layers (Core, Infrastructure, Application, Server) for maximum maintainability and testability.
* **Cross-Platform**: Compatible with Windows, macOS, and Linux.
* **Container Support**: Includes Dev Container configuration for a consistent development environment.

## 🛠️ Available Tools

The server exposes the following tools to the MCP client:

| Tool | Namespace | Description |
| :--- | :--- | :--- |
| `create_script` | `unity` | Creates a new `MonoBehaviour` script at a specified path with a valid class name. |
| `create_scene` | `unity` | Creates a new, empty Unity Scene file (`.unity`) with standard header. |
| `create_asset` | `unity` | Creates a generic asset with provided text content at the specified path. |
| `create_gameobject`| `unity` | Creates a new GameObject entry in a specified Scene (Standalone simulation). |
| `list_assets` | `unity` | Lists files in the project's `Assets` directory (supports glob patterns). |
| `build_project` | `unity` | Triggers a Unity project build via the command line interface. |
| `ping` | `global` | A simple health check to verify the server is running. |

## 🏗️ Architecture

This project strictly adheres to **Clean Architecture** principles:

1. **UnityMcp.Core**: The Domain Layer. Contains Domain Models, Value Objects, and core interfaces (`IUnityService`, `IMcpTransport`).
2. **UnityMcp.Infrastructure**: Implementation Layer. Handles I/O, JSON-RPC transport, and the `FileUnityService` which implements Unity logic without Editor process dependency.
3. **UnityMcp.Application**: Application Layer. Defines the `McpRouter` and strictly-typed bridge handlers that map JSON-RPC requests to domain logic.
4. **UnityMcp.Server**: Presentation/Hosting Layer. The Composition Root using Microsoft.Extensions.Hosting.

## 📦 Getting Started

### Prerequisites

* [**.NET 10.0 SDK**](https://dotnet.microsoft.com/download/dotnet/10.0)
* (Optional) **Unity Editor** (2022.3+ Recommended) for verifying generated assets.
* (Optional) **Docker Desktop** if using the Dev Container.

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/your-repo/Unity-MCP-Server.git
   cd Unity-MCP-Server
   ```
2. **Build the Solution:**
   ```bash
   dotnet build
   ```

### Configuration with Claude Desktop

1. Locate your config file:
   * **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
   * **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`

2. Add the server configuration:
   ```json
   {
     "mcpServers": {
       "unity": {
         "command": "dotnet",
         "args": [
           "run",
           "--project",
           "C:/Path/To/Unity-MCP-Server/UnityMcp.Server/UnityMcp.Server.csproj"
         ]
       }
     }
   }
   ```

## 🧪 Testing

The project includes a robust and realistic unit test suite in `UnityMcp.Tests`.

* **Strict Mocking**: All tests use `NSubstitute` with strict verifications (`Received(1)`) to ensure idempotent behavior and correct service invocation.
* **High Coverage**: Achieves approximately **~80% test coverage** across the Application and Core layers.
* **Negative Testing**: Includes test cases for invalid parameters, service failures, and "Not Supported" scenarios.

To run the tests:
```bash
dotnet test
```

## 🤝 Contributing

Contributions are welcome! Please follow the Clean Architecture patterns established in the codebase.

## 📄 License

[MIT License](LICENSE)
