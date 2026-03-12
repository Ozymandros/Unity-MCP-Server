# Docker: Install, Run, and Use as MCP Server

This guide covers installing and running the Unity MCP Server as a **Docker container** and wiring it as an MCP server in your host (Claude Desktop, Cursor, Windsurf, or any MCP client).

---

## Prerequisites

- **Docker** installed and running ([Get Docker](https://docs.docker.com/get-docker/)).
- No .NET SDK required on the host; the image includes the runtime.

---

## 1. Install (build the image)

From the repo root:

```bash
docker build -t unity-mcp-server .
```

- **`-t unity-mcp-server`** tags the image so you can run it by name.
- Optional: if you publish the image to a registry, use `docker pull <your-registry>/unity-mcp-server` and tag it locally as `unity-mcp-server`.

---

## 2. Run the container (stdio)

The server speaks MCP over **stdio**. Run it in interactive mode so the host can attach stdin/stdout:

```bash
docker run -i --rm unity-mcp-server
```

- **`-i`** keeps stdin open (required for MCP).
- **`--rm`** removes the container when it exits.
- Do **not** use `-d` (detached); MCP hosts need to be the parent process and own the pipes.

You normally **don’t** run this by hand; your MCP host runs it and connects its stdin/stdout to the server.

---

## 3. Configure your host to use the container as MCP server

Your MCP client runs `docker run -i --rm unity-mcp-server` and talks JSON-RPC over the process’s stdin/stdout.

### Claude Desktop

Edit your Claude Desktop config:

- **Windows:** `%APPDATA%\Claude\claude_desktop_config.json`
- **macOS:** `~/Library/Application Support/Claude/claude_desktop_config.json`

Add the server (replace `unity-mcp` with any key you like):

```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "docker",
      "args": [
        "run",
        "-i",
        "--rm",
        "unity-mcp-server"
      ]
    }
  }
}
```

Restart Claude Desktop so it picks up the config. The Unity MCP tools will appear when you start a conversation.

### Cursor / Windsurf / VS Code (MCP)

In your IDE’s MCP settings, add a server that runs Docker, for example:

- **Command:** `docker`
- **Args:** `run`, `-i`, `--rm`, `unity-mcp-server`

(Exact UI varies; use “Add MCP server” or similar and enter command + args as above.)

### Generic MCP config (JSON)

Many MCP clients accept a config file in this shape:

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

Point your client at this file (or merge this block into your existing config).

---

## 4. Integration example (end-to-end)

### Step 1: Build and verify the image

```bash
cd /path/to/Unity-MCP-Server
docker build -t unity-mcp-server .
docker run -i --rm unity-mcp-server
```

Press Ctrl+C to stop; the server is waiting for JSON-RPC on stdin.

### Step 2: Configure Claude Desktop

Save this as your Claude Desktop config (adjust path if needed):

**Windows** — `%APPDATA%\Claude\claude_desktop_config.json`:

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

**macOS / Linux** — `~/.config/Claude/claude_desktop_config.json` (or path shown in Claude Desktop settings):

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

### Step 3: Restart and use

1. Restart Claude Desktop (or your MCP client).
2. Start a new conversation.
3. You should see MCP tools available (e.g. `ping`, `unity_create_scene`, `unity_create_script`, `unity_scaffold_project`, etc.).
4. Example prompts:
   - *“Ping the MCP server.”* → uses `ping`.
   - *“Create a new Unity project named MyGame under C:\Projects (or /home/me/Projects).”* → can use `unity_scaffold_project`.
   - *“Create a C# script called PlayerController in Assets/Scripts.”* → can use `unity_create_script` or `unity_save_script`.

### Step 4: Optional — mount a project directory

If you want the server to read/write a specific folder on your machine (e.g. an existing Unity project), mount it and set the project path in your prompts:

**Windows (PowerShell):**

```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "docker",
      "args": [
        "run",
        "-i",
        "--rm",
        "-v", "C:\\Projects\\MyUnityProject:/workspace:rw",
        "unity-mcp-server"
      ]
    }
  }
}
```

Then in prompts, use **projectPath** = `/workspace` (the path inside the container).

**macOS / Linux:**

```json
"args": [
  "run",
  "-i",
  "--rm",
  "-v", "/home/me/MyUnityProject:/workspace:rw",
  "unity-mcp-server"
]
```

Again, use **projectPath** = `/workspace` when calling tools.

---

## 5. Troubleshooting

| Issue | What to do |
|-------|------------|
| **“docker: command not found”** | Install Docker and ensure it’s on your PATH. Restart the MCP host after installing. |
| **“Cannot connect to the Docker daemon”** | Start Docker Desktop (or the Docker service). |
| **Image not found** | Run `docker build -t unity-mcp-server .` from the repo root. |
| **No tools in Claude / Cursor** | Confirm the server entry is in the correct config file and restart the app. Check the host’s MCP logs for errors. |
| **Permission denied (volume)** | On Linux/macOS, ensure the mounted directory is readable (and writable if tools will create files). |

---

## 6. Summary

| Step | Action |
|------|--------|
| **Install** | `docker build -t unity-mcp-server .` |
| **Run (manual)** | `docker run -i --rm unity-mcp-server` |
| **Use as MCP** | In your MCP config, set `command` to `docker` and `args` to `["run", "-i", "--rm", "unity-mcp-server"]`. |
| **Optional** | Add `-v /host/path:/workspace:rw` to `args` and use `projectPath: "/workspace"` in tool calls. |

For non-Docker install (global .NET tool or run from source), see [QUICKSTART.md](../QUICKSTART.md).
