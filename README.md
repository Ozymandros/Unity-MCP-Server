# MCP Server (.NET) v1.2.0

[![.NET CI](https://img.shields.io/github/actions/workflow/status/ozymandros/unity-mcp-server/dotnet.yml?branch=main&label=.NET%20CI&logo=dotnet&logoColor=white&style=flat-square)](https://github.com/ozymandros/unity-mcp-server/actions/workflows/dotnet.yml)
[![Quality](https://img.shields.io/github/actions/workflow/status/ozymandros/unity-mcp-server/static_analysis.yml?branch=main&label=Quality&logo=github-actions&logoColor=white&style=flat-square)](https://github.com/ozymandros/unity-mcp-server/actions/workflows/static_analysis.yml)
[![MCP Protocol](https://img.shields.io/badge/MCP-2025--11--25-orange?style=flat-square&logo=json)](https://modelcontextprotocol.io)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square&logo=opensourceinitiative&logoColor=white)](https://opensource.org/licenses/MIT)

A pure .NET implementation of the Model Context Protocol (MCP) server. Enables AI assistants and LLMs to interact with applications via JSON-RPC 2.0 over TCP. No Unity or UPM dependencies.

---

## Features
- **Extensible tool/skill architecture**: Add new automation and integration tools easily.
- **JSON-RPC 2.0 over TCP**: Standard protocol for interoperability with any client (Node.js, Python, etc).
- **Pure .NET**: No Unity/UPM dependencies. Runs anywhere .NET 10.0+ is supported.
- **Docker & DevContainer ready**: Containerized for CI/CD and cloud workflows.
- **Comprehensive test suite**: Ensures reliability and correctness.

---

## Quick Start

Get up and running in minutes!

### Prerequisites
- .NET 10.0 SDK or later
- Python 3.7+ or Node.js 14+ (for client examples)

### Installation

#### Option A: Run from Source (Development)
1. Clone this repo and build:
   ```sh
   dotnet build UnityMcpServer.slnx
   ```
2. Run the server:
   ```sh
   dotnet run --project UnityMcp.Server/UnityMcp.Server.csproj
   ```

#### Option B: Install as Global Tool (Recommended)
You can install the server as a system-wide command:
1. Run the installation script:
   ```powershell
   ./install-tool.ps1
   ```
2. Now you can run it from any terminal:
   ```sh
   unity-mcp
   ```

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
- Open the project in VS Code
- If prompted, "Reopen in Container" (requires Docker)
- The `.devcontainer/devcontainer.json` configures the environment with .NET SDK, PowerShell, and C# extensions
- Port 8765 is forwarded for MCP connections

### Docker (Standalone)
- Build the Docker image:
  ```sh
  docker build -t mcp-server-dotnet .
  ```
- Run the server in a container:
  ```sh
  docker run -p 8765:8765 mcp-server-dotnet
  ```
- The server will be accessible on `localhost:8765` from your host

---

## Project Structure
- **UnityMcp.Server/**: .NET server entry point
- **UnityMcp.Core/**: Core server logic (no Unity dependencies)
- **UnityMcp.Tests/**: Test suite for core and server logic
- **QUICKSTART.md**: Getting started guide
- **SKILL.md**: AI Agent discovery and documentation
- **Legacy/**: Archived Unity/UPM code and docs (for reference only)

---

## Protocol & API Reference

See [Documentation~/API_REFERENCE.md](Documentation~/API_REFERENCE.md) for the full protocol and tool documentation.

---

## Troubleshooting
- **Port already in use:** Change port in config or close other app.
- **No tools registered:** Check file locations and namespaces.
- **Connection timeout:** Check firewall and allow port 8765.

---

## Version History

- **1.2.0** (2026-02-15)
  - Pure .NET structure, Unity/UPM code archived
  - Major documentation and protocol improvements
  - Expanded API and troubleshooting docs
  - Added VS Code DevContainer and Docker support
- **2.0.0** (2026-02-14)
  - License-free CI/CD pipeline
  - Standalone test suite and solution reorg
- **1.0.0** (2025-02-14)
  - Initial release with core MCP protocol and 5 tools

---

## Legacy Unity/UPM Support

All Unity/UPM-specific code and documentation has been archived in the `archive/` folder. This repo is now a pure .NET MCP server. For Unity integration, see the archived files.
