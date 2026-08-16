# Unity MCP Server (.NET) v3.1.0

<!-- Build & Quality -->
[![CI](https://img.shields.io/github/actions/workflow/status/Ozymandros/Unity-MCP-Server/ci.yml?branch=main&label=CI&logo=dotnet&logoColor=white&style=flat-square)](https://github.com/Ozymandros/Unity-MCP-Server/actions/workflows/ci.yml)
[![CodeQL](https://img.shields.io/github/actions/workflow/status/Ozymandros/Unity-MCP-Server/codeql.yml?branch=main&label=CodeQL&logo=github&style=flat-square)](https://github.com/Ozymandros/Unity-MCP-Server/actions/workflows/codeql.yml)
[![Release](https://img.shields.io/github/actions/workflow/status/Ozymandros/Unity-MCP-Server/release.yml?branch=main&label=Release&logo=githubactions&logoColor=white&style=flat-square)](https://github.com/Ozymandros/Unity-MCP-Server/actions/workflows/release.yml)

<!-- Distribution -->
[![NuGet](https://img.shields.io/nuget/v/UnityMCP.Server?style=flat-square&logo=nuget&logoColor=white)](https://www.nuget.org/packages/UnityMCP.Server)
[![Docker (GHCR)](https://img.shields.io/badge/docker-GHCR-blue?style=flat-square&logo=docker&logoColor=white)](https://github.com/Ozymandros/Unity-MCP-Server/pkgs/container/unity-mcp-server)
[![GitHub Release](https://img.shields.io/github/v/release/Ozymandros/Unity-MCP-Server?display_name=tag&style=flat-square&logo=github)](https://github.com/Ozymandros/Unity-MCP-Server/releases)

<!-- Ecosystem -->
[![MCP Protocol](https://img.shields.io/badge/MCP-2025--11--25-orange?style=flat-square&logo=json)](https://modelcontextprotocol.io)
[![MCP Inspector](https://img.shields.io/badge/MCP%20Inspector-supported-blue?style=flat-square&logo=visualstudiocode)](https://marketplace.visualstudio.com/items?itemName=modelcontextprotocol.mcp-inspector)
[![Docker MCP Setup](https://img.shields.io/badge/Docker%20MCP%20Setup-supported-blue?style=flat-square&logo=docker)](https://github.com/Ozymandros/Unity-MCP-Server/blob/main/Docs/docker-mcp-setup.md)

<!-- Project -->
[![License](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square&logo=opensourceinitiative&logoColor=white)](https://opensource.org/licenses/MIT)

A pure .NET Model Context Protocol (MCP) server for Unity Editor automation. Enables AI assistants and LLMs to scaffold projects, create scenes, scripts, prefabs, and manage assets with proper .meta sidecars — no Unity or UPM dependencies at build or runtime.

---

## Features
- **Extended MCP tool set (80+ tools)**: Project scaffolding, native/file scene graph, typed asset saving (with .meta sidecars), Roslyn C# validation, hybrid Editor bridge (live + batch) for import validation and native asset ops, UPM packages, ProjectSettings/build profiles, documentation search, UI/navigation/input/animation/VFX/physics tools, and orchestration recipes.
- **Complete .meta generation**: MonoImporter, TextureImporter, AudioImporter, DefaultImporter, and folder .meta — Unity recognises all assets on import.
- **Capability-aware modes**: `unity_get_capabilities` distinguishes native-file, partial-file, native-editor, compatibility-surrogate, and Editor-backed validation. `liveBridgeConnected` reports an open Unity Editor bridge.
- **Hybrid Editor bridge**: Optional `com.unitymcp.bridge` package enables live localhost or batch-mode Unity API access without referencing Unity assemblies in the MCP server.
- **Extensible tool/skill architecture**: Tools organised in partial classes by concern; add new automation easily.
- **MCP over stdio**: compatible with Claude Desktop, Cursor, and other MCP hosts that launch local stdio servers.
- **Pure .NET**: No Unity/UPM dependencies in the server process. Runs on Windows, Linux, and macOS where .NET 10.0+ is supported.
- **CI/CD**: GitHub Actions for build, test (multi-OS), CodeQL, Dependabot, and NuGet release packaging.
- **Comprehensive test suite**: 130+ tests (unit + integration), including path-containment coverage; Editor-gated tests use `UNITY_EDITOR_PATH`.

---

## Quick Start

Get up and running in minutes!

### Prerequisites
- .NET 10.0 SDK or later
- Python 3.7+ or Node.js 14+ (for client examples)

### Installation
You can either run from source, or install the published NuGet tool.

**Option A – Run from source**

1. Clone this repo and build:
   ```sh
   dotnet build Unity-MCP-Server.sln
   ```
2. Run the server:
   ```sh
   dotnet run --project UnityMCP.Server/UnityMCP.Server.csproj
   ```

**Option B – Install via NuGet (global tool)**

Install the tool globally from NuGet.org:

```sh
dotnet tool install --global UnityMCP.Server
```

After installation you can run:

```sh
unity-mcp
```

To upgrade to the latest version:

```sh
dotnet tool update --global UnityMCP.Server
```

For local development builds instead of NuGet, use `install-tool.ps1` (see [Skills/SKILL.md](Skills/SKILL.md)).

### Test Connection
Use an MCP stdio client such as the official Inspector:

```sh
npx @modelcontextprotocol/inspector unity-mcp
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
- **UnityMcp.Tests/**: 128 tests in fixture-split files (unit + integration).
- **Docs/**: Validation pipeline, tool contracts, UI/nav/recipes, and [Docker MCP setup](Docs/docker-mcp-setup.md) (install/run/configure + integration example).
- **Skills/SKILL.md**: AI agent skill reference.

---

## Protocol & API Reference

See [Docs/](Docs/) for tool contracts, validation pipeline, and scenario docs. Tool list and usage are in [Skills/SKILL.md](Skills/SKILL.md).

---

## Troubleshooting
- **No tools registered:** Check file locations and namespaces.
- **Unity import validation unavailable:** Set `UNITY_EDITOR_PATH` to a Unity Editor executable. Without it, file-only tools still run, but `unity_validate_import` returns an `ExternalTool` error instead of a false success.
- **MCP client cannot connect:** This server uses stdio. Configure the host to launch `unity-mcp` or `dotnet run --project UnityMcp.Server/UnityMCP.Server.csproj`; do not connect to a TCP port.

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