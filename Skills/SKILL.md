---
name: unity-mcp
description: Full-featured Model Context Protocol (MCP) server for Unity Editor automation. Enables AI agents to orchestrate project structure, script creation, scene hierarchy, and CI/CD pipelines directly via JSON-RPC.
---

# Unity MCP Server Skill

This skill provides a bridge between AI agents and the Unity Editor using the Model Context Protocol (MCP). It allows for seamless project management, asset creation, and automation tasks.

## 🚀 Core Capabilities

### 1. Project Orchestration

AI agents can explore, modify, and restructure Unity projects programmatically.

- **Discovery**: Full visibility into the `Assets` folder structure.
- **Code Generation**: Automated creation of MonoBehaviours with proper class naming and boilerplate.
- **Scene Assembly**: Programmatic creation of Scenes and GameObjects.

### 2. DevOps & CI/CD

Trigger and monitor project builds directly from the agent conversation.

- **Multi-platform support**: Build for Windows, macOS, Linux, Android, and iOS.
- **Build Reporting**: Failures are captured and bubbled up to the agent for troubleshooting.

### 3. Protocol & Reliability

- **JSON-RPC 2.0 Compliance**: Standard-compliant communication (Handshake, Tools, Health).
- **Strict Validation**: All tool parameters are validated against C# DataAnnotations before execution.
- **Stderr Logging**: Safe logging that doesn't interfere with the Stdio-based MCP protocol.

## 🛠️ Environment Configuration

### Global Tool (Recommended)

Install system-wide to run via `unity-mcp`:

```powershell
./install-tool.ps1
```

### Required Environment Variables

| Variable | Purpose | Example |
| :--- | :--- | :--- |
| `UNITY_PATH` | Path to Unity Managed DLLs | `C:\Program Files\Unity\...\Managed` |
| `DOTNET_ENVIRONMENT` | Runtime mode | `Development` or `Production` |

## 📖 Complete Tool Reference

The following tools are available to any MCP-compatible agent:

### 📡 Connectivity & System

| Method | Description | Parameters |
| :--- | :--- | :--- |
| `ping` | Verifies server health. | None |
| `tools/list` | Returns discovery schema for all tools. | None |
| `initialize` | Handshake (protocol versioning). | `clientName`, `clientVersion` |

### 🛠️ Unity Engine Tools

| Method | Description | Required Parameters |
| :--- | :--- | :--- |
| `unity_list_assets` | Lists assets in a directory. | `path` (can be default "Assets") |
| `unity_create_script` | Creates a new C# script. | `path`, `scriptName` |
| `unity_create_scene` | Creates a new `.unity` file. | `path` |
| `unity_create_gameobject` | Adds object to a scene. | `scenePath`, `gameObjectName` |
| `unity_create_asset` | Creates generic text assets. | `path`, `content` |
| `unity_build_project` | Triggers BuildPipeline. | `target` (Win64, OSX etc), `outputPath` |

## 💡 Best Practices for AI Agents

1. **Path Formatting**: Always use **forward slashes** (`/`) for `path` parameters, even if the host is Windows. The server handles normalization.
2. **Execution Flow**:
   - `unity_list_assets` → Confirm directory exists.
   - `unity_create_scene` → Verify it was created.
   - `unity_create_gameobject` → Populate the scene.
3. **Build Targets**: Valid targets for `unity_build_project` are: `Win64`, `OSX`, `Linux64`, `Android`, `iOS`.

## 🔧 Installation & Setup

For detailed setup, see the **[QUICKSTART.md](./QUICKSTART.md)** or **[README.md](./README.md)**.
