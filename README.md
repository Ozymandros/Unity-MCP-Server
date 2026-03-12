# Unity MCP Server (.NET) v3.0.0

[![CI](https://img.shields.io/github/actions/workflow/status/Ozymandros/Unity-MCP-Server/ci.yml?branch=main&label=CI&logo=dotnet&logoColor=white&style=flat-square)](https://github.com/Ozymandros/Unity-MCP-Server/actions/workflows/ci.yml)
[![CodeQL](https://img.shields.io/github/actions/workflow/status/Ozymandros/Unity-MCP-Server/codeql.yml?branch=main&label=CodeQL&logo=github&style=flat-square)](https://github.com/Ozymandros/Unity-MCP-Server/actions/workflows/codeql.yml)
[![MCP Protocol](https://img.shields.io/badge/MCP-2025--11--25-orange?style=flat-square&logo=json)](https://modelcontextprotocol.io)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square&logo=opensourceinitiative&logoColor=white)](https://opensource.org/licenses/MIT)

A pure .NET Model Context Protocol (MCP) server for Unity Editor automation. Enables AI assistants and LLMs to scaffold projects, create scenes, scripts, prefabs, and manage assets with proper .meta sidecars — no Unity or UPM dependencies at build or runtime.

---

## Features
- **Extended MCP tool set**: Project scaffolding, scene authoring, typed asset saving (with .meta sidecars), C# validation, UPM packages, UI/navigation/input/animation/VFX/physics tools, and orchestration recipes.
- **Complete .meta generation**: MonoImporter, TextureImporter, AudioImporter, DefaultImporter, and folder .meta — Unity recognises all assets on import.
- **Extensible tool/skill architecture**: Tools organised in partial classes by concern; add new automation easily.
- **JSON-RPC 2.0 over stdio**: MCP over stdio for compatibility with Claude Desktop, Cursor, and other MCP hosts.
- **Pure .NET**: No Unity/UPM dependencies. Runs on Windows, Linux, and macOS where .NET 10.0+ is supported.
- **CI/CD**: GitHub Actions for build, test (multi-OS), CodeQL, Dependabot, and NuGet release packaging.
- **Comprehensive test suite**: 117 tests (unit + integration), split by fixture and concern.

---

## Quick Start

Get up and running in minutes!

### Prerequisites
- .NET 10.0 SDK or later
- Python 3.7+ or Node.js 14+ (for client examples)

### Installation
1. Clone this repo and build:
   ```sh
   dotnet build Unity-MCP-Server.sln
   ```
2. Run the server:
   ```sh
   dotnet run --project UnityMCP.Server/UnityMCP.Server.csproj
   ```
   Or install as a global tool and run `unity-mcp` from anywhere (see [QUICKSTART.md](QUICKSTART.md)).

### Test Connection
- **Python:**
  ```python
  import socket, json
  sock = socket.socket(); sock.connect(('localhost', 8765))
  req = {"jsonrpc": "2.0", "id": "1", "method": "ping", "params": {}}
  sock.send((json.dumps(req) + "\n").encode()); print(sock.recv(1024).decode()); sock.close()
  ```
- **Command Line:**
  ```powershell
  echo '{"jsonrpc":"2.0","id":"1","method":"ping","params":{}}' | ncat localhost 8765
  ```

---

## VS Code Dev Container & Docker

You can develop and run the MCP Server in a fully containerized environment using VS Code Dev Containers and Docker.

### Dev Container (VS Code)
- Open the project in VS Code and run **“Reopen in Container”** (requires Docker)
- The `.devcontainer/devcontainer.json` uses the .NET 10 dev container image and runs `dotnet restore Unity-MCP-Server.sln` after create
- MCP uses **stdio** (no port). Use the integrated terminal or your MCP host’s config to run `dotnet run --project UnityMCP.Server/UnityMCP.Server.csproj` or `unity-mcp` if installed

### Docker (Standalone)
- Build the image:
  ```sh
  docker build -t unity-mcp-server .
  ```
- Run the server (stdio; MCP hosts start this with stdin/stdout connected):
  ```sh
  docker run -i --rm unity-mcp-server
  ```
- **Configure as MCP server:** See **[Docs/docker-mcp-setup.md](Docs/docker-mcp-setup.md)** for Claude Desktop, Cursor, and integration examples. Optional: mount a Unity folder and set `UNITY_PATH` (see [DOCKER.md](DOCKER.md)).

---

## Project Structure
- **UnityMcp.Server/**: .NET server entry point (stdio MCP transport); packable as `unity-mcp` global tool (v3.0.0).
- **UnityMcp.Application/**: MCP tool definitions in partial classes (UnityTools.cs + UnityTools.*.cs by concern).
- **UnityMcp.Core/**: Interfaces and abstractions.
- **UnityMcp.Infrastructure/**: File-based Unity service, YAML writer, MetaFileWriter.
- **UnityMcp.Tests/**: 117 tests in fixture-split files (unit + integration).
- **Docs/**: Validation pipeline, tool contracts, UI/nav/recipes, and [Docker MCP setup](Docs/docker-mcp-setup.md) (install/run/configure + integration example).
- **Skills/SKILL.md**: AI agent skill reference.

---

## Protocol & API Reference

See [Docs/](Docs/) for tool contracts, validation pipeline, and scenario docs. Tool list and usage are in [Skills/SKILL.md](Skills/SKILL.md).

---

## Troubleshooting
- **Port already in use:** Change port in config or close other app.
- **No tools registered:** Check file locations and namespaces.
- **Connection timeout:** Check firewall and allow port 8765.

---

## Version History
- **3.0.0** (2026-03)
  - Version set to 3.0.0; NuGet package metadata and multi-platform (Windows, Linux, macOS).
  - Refactor: UnityTools split into partial classes by concern (ScenesAndAssets, Project, PackagesAndValidation, Ui, Navigation, InputAndAnimation, VfxAndPhysics, Recipes).
  - Tests refactored into separate files by fixture (UnityToolsTests, UnityYamlWriterTests, UnityToolsNewToolsTests, UnityToolsRecipeTests, MetaFileWriterTests, FileUnityServiceNewToolsTests, AdvancedSystemsGoldenFixtureTests).
  - GitHub Actions: CI (multi-OS build + test), Release (NuGet pack on tag), CodeQL, Dependabot.
  - 117 tests (unit + integration).
  **2.0.0** (2026-03-12)
- **1.5.0** / **1.4.0** (2026-03-01)
  - Extended MCP tools (scaffold, folder, save script/text/texture/audio, validate C#, add packages, project info, UI, nav, input, animation, VFX, physics, recipes).
  - Full .meta sidecar generation; 55+ tests.
- **1.2.0** (2026-02-15)
  - Pure .NET structure, Unity/UPM code archived; Docs and Docker support.
- **1.0.0** (2025-02-14)
  - Initial release with core MCP protocol and tools.

---

## Legacy Unity/UPM Support

All Unity/UPM-specific code and documentation has been archived in the `archive/` folder. This repo is now a pure .NET MCP server. For Unity integration, see the archived files.
