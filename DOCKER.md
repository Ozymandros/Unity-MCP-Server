# Docker Integration (Legal & Functional)

**As of v3.0.0**, the Unity MCP Server is a pure .NET application and runs on Windows, Linux, and macOS without any Unity DLLs. The server works fully in a container without mounting Unity. The volume-mount approach below is optional (e.g. for future Unity CLI batch-mode validation).

**Full instructions:** For install, run, and **using the container as an MCP server** (Claude Desktop, Cursor, integration example), see **[Docs/docker-mcp-setup.md](Docs/docker-mcp-setup.md)**.

---

To use the server in Docker while optionally giving it access to Unity managed assemblies (e.g. for batch validation), you can mount your local Unity installation as a read-only volume.

This approach ensures that:

1. **Legality**: You are not redistributing `UnityEditor.dll` or `UnityEngine.dll` inside the image.
2. **Functionality**: The server has access to the native assemblies at runtime.
3. **Consistency**: The same image can be used with different Unity versions by just changing the volume mount.

## 🚀 Running with Docker

### 1. Build the Image

No Unity DLLs or build secrets are required; the server is pure .NET.

```bash
docker build -t unity-mcp-server .
```

### 2. Run (stdio)

MCP uses stdio. Your MCP host (e.g. Claude Desktop, Cursor) should start the container with stdin/stdout connected, for example:

```bash
docker run -i --rm unity-mcp-server
```

### 3. Optional: Run with Unity Volume Mount

If you later add Unity CLI integration, you can mount your Unity Editor's `Managed` folder and set `UNITY_PATH`:

#### Windows (PowerShell)

```powershell
docker run -it --rm `
  -v "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Data\Managed:/unity:ro" `
  -e UNITY_PATH=/unity `
  unity-mcp-server
```

#### macOS / Linux

```bash
docker run -it --rm \
  -v "/Applications/Unity/Hub/Editor/2022.3.0f1/Editor/Data/Managed:/unity:ro" \
  -e UNITY_PATH=/unity \
  unity-mcp-server
```

## 🏗️ Technical Details

- **Dockerfile**: Multi-stage build (SDK for build, runtime for final image). No Unity references; `UNITY_PATH` is reserved for future optional Unity CLI integration.
- **Dev container**: `.devcontainer/devcontainer.json` uses `mcr.microsoft.com/devcontainers/dotnet:dev-10.0` and restores `Unity-MCP-Server.sln` on create.

## 📝 Configuration (Claude Desktop and other MCP hosts)

Example MCP config using the Docker image (stdio; no port):

```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "docker",
      "args": ["run", "-i", "--rm", "unity-mcp-server"]
    }
  }
}
```

Optional: to pass a Unity mount and `UNITY_PATH`, add `-v` and `-e` to `args` as in the “Run with Unity Volume Mount” section 3 above. Full Docker MCP guide: **Docs/docker-mcp-setup.md**.
