# Unity-Generator MCP client (reference)

This folder contains a **reference implementation** of the MCP client for Unity-Generator that talks to Unity-MCP-Server.

## Copy into Unity-Generator

Copy the following file into your Unity-Generator repo:

- `backend/app/services/unity_mcp_client.py` → `backend/app/services/unity_mcp_client.py`

Then:

1. Add the MCP Python SDK to Unity-Generator: `pip install "mcp[cli]"` or `uv add "mcp[cli]"`.
2. Ensure the orchestrator passes `timeout` into the MCP setup (e.g. from `McpUnitySetupBackend.run_setup(..., timeout=300)`) and that `_call_tool` receives it (e.g. `_call_tool(name, args, timeout_seconds=timeout)`).
3. Keep existing tests that mock `_call_tool` and `mcp_available`. Add tests that verify tool names, arguments, and response normalization when using a real or simulated server.

## Configuration (environment)

| Variable | Description |
|----------|-------------|
| `UNITY_USE_MCP` | Set to `1`, `true`, or `yes` to enable MCP. Otherwise the client returns the placeholder response. |
| `UNITY_MCP_SERVER_URL` | If set, connect via Streamable HTTP to this URL. |
| `UNITY_MCP_COMMAND` | Server command for stdio (default: `unity-mcp`). |
| `UNITY_MCP_ARGS` | Optional space-separated arguments for the stdio server. |

## Tool contract

The client calls these four tools with the given arguments and normalizes their JSON response:

| Tool | Arguments | Normalized keys in response |
|------|-----------|-----------------------------|
| `unity_install_packages` | `project_path`, `packages` | `success`, `message`, `installed` |
| `unity_create_default_scene` | `project_path`, `scene_name` | `success`, `message`, `scene_path`, `prefab_path` |
| `unity_configure_urp` | `project_path` | `success`, `message` |
| `unity_validate_import` | `project_path` | `success`, `message`, `error_count`, `warning_count`, `errors`, `warnings` |

Timeout is applied per tool call when `timeout_seconds` is passed to `_call_tool`. If the Python SDK does not support timeout on `call_tool`, the client uses `asyncio.wait_for` around the call.
