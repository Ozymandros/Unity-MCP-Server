"""
MCP client for Unity-MCP-Server (MCP-Unity contract).

When MCP is enabled (UNITY_USE_MCP=1 or equivalent) and a server is configured,
calls the four contract tools: unity_install_packages, unity_create_default_scene,
unity_configure_urp, unity_validate_import. Normalizes JSON responses to the dict
shape expected by the rest of the app (success, message, installed, scene_path,
prefab_path, error_count, warning_count, errors, warnings).

Configuration:
- UNITY_USE_MCP: Set to 1 (or "true") to enable MCP; otherwise placeholder response.
- UNITY_MCP_SERVER_URL: If set, use HTTP/SSE transport to this URL.
- Otherwise: use stdio and spawn the server process (e.g. unity-mcp or
  UNITY_MCP_COMMAND env for custom command).

Copy this file to Unity-Generator repo at backend/app/services/unity_mcp_client.py.
"""

from __future__ import annotations

import asyncio
import json
import os
from typing import Any

# Optional MCP SDK; add to Unity-Generator: pip install mcp[cli] or uv add "mcp[cli]"
try:
    from mcp import ClientSession
    from mcp.client.stdio import StdioServerParameters, stdio_client
    from mcp.types import TextContent
except ImportError:
    ClientSession = None
    StdioServerParameters = None
    stdio_client = None
    TextContent = None

# Optional Streamable HTTP for UNITY_MCP_SERVER_URL
try:
    from mcp.client.streamable_http import streamable_http_client
except ImportError:
    streamable_http_client = None


def mcp_available() -> bool:
    """Return True if MCP is enabled and the SDK is available."""
    if ClientSession is None:
        return False
    use_mcp = os.environ.get("UNITY_USE_MCP", "").strip().lower() in ("1", "true", "yes")
    if not use_mcp:
        return False
    url = os.environ.get("UNITY_MCP_SERVER_URL", "").strip()
    cmd = os.environ.get("UNITY_MCP_COMMAND", "unity-mcp").strip()
    return bool(url or cmd)


def _placeholder_response(message: str = "MCP-Unity plugin is not configured") -> dict[str, Any]:
    return {"success": False, "message": message}


def _normalize_tool_response(text: str, tool_name: str) -> dict[str, Any]:
    """Parse server JSON and normalize to the dict shape the app expects."""
    out: dict[str, Any] = {"success": False, "message": None}
    try:
        data = json.loads(text) if isinstance(text, str) else text
    except (json.JSONDecodeError, TypeError):
        out["message"] = f"Invalid JSON from {tool_name}: {text[:200] if text else 'empty'}"
        return out

    if not isinstance(data, dict):
        out["message"] = f"Unexpected response type from {tool_name}"
        return out

    out["success"] = data.get("success", False)
    if "message" in data and data["message"] is not None:
        out["message"] = data["message"]

    if tool_name == "unity_install_packages":
        out["installed"] = data.get("installed") if isinstance(data.get("installed"), list) else []
    elif tool_name == "unity_create_default_scene":
        out["scene_path"] = data.get("scene_path")
        out["prefab_path"] = data.get("prefab_path")
    elif tool_name == "unity_validate_import":
        out["error_count"] = data.get("error_count", 0)
        out["warning_count"] = data.get("warning_count", 0)
        out["errors"] = data.get("errors") if isinstance(data.get("errors"), list) else []
        out["warnings"] = data.get("warnings") if isinstance(data.get("warnings"), list) else []
        if out["error_count"] and not out.get("message"):
            out["message"] = f"Compilation had {out['error_count']} error(s)."

    return out


async def _call_tool_async(
    tool_name: str,
    arguments: dict[str, Any],
    timeout_seconds: float | None = None,
) -> dict[str, Any]:
    """Connect to MCP server, call tool, return normalized dict. Runs in event loop."""
    if not mcp_available():
        return _placeholder_response()

    url = os.environ.get("UNITY_MCP_SERVER_URL", "").strip()
    cmd = os.environ.get("UNITY_MCP_COMMAND", "unity-mcp").strip()
    args_for_cmd = os.environ.get("UNITY_MCP_ARGS", "").strip().split() if os.environ.get("UNITY_MCP_ARGS") else []

    async def _do_call(session: ClientSession) -> dict[str, Any]:
        if timeout_seconds is not None and timeout_seconds > 0:
            result = await asyncio.wait_for(
                session.call_tool(tool_name, arguments),
                timeout=timeout_seconds,
            )
        else:
            result = await session.call_tool(tool_name, arguments)

        if getattr(result, "isError", False):
            err_text = ""
            for c in getattr(result, "content", []) or []:
                if TextContent is not None and isinstance(c, TextContent):
                    err_text = c.text
                    break
            return _normalize_tool_response(err_text or "Tool returned error", tool_name)

        text = ""
        for c in getattr(result, "content", []) or []:
            if TextContent is not None and isinstance(c, TextContent):
                text = c.text
                break
        return _normalize_tool_response(text or "{}", tool_name)

    try:
        if url:
            if streamable_http_client is None:
                return _placeholder_response("UNITY_MCP_SERVER_URL set but streamable HTTP not available (mcp[cli]?).")
            async with streamable_http_client(url) as (read_stream, write_stream, _):
                async with ClientSession(read_stream, write_stream) as session:
                    await session.initialize()
                    return await _do_call(session)
        else:
            params = StdioServerParameters(command=cmd, args=args_for_cmd)
            async with stdio_client(params) as (read, write):
                async with ClientSession(read, write) as session:
                    await session.initialize()
                    return await _do_call(session)
    except asyncio.TimeoutError:
        return {"success": False, "message": f"MCP tool {tool_name} timed out after {timeout_seconds}s"}
    except Exception as e:
        return {"success": False, "message": f"MCP client error: {e!s}"}


def _call_tool(
    tool_name: str,
    arguments: dict[str, Any],
    timeout_seconds: float | None = None,
) -> dict[str, Any]:
    """
    Call an MCP tool by name with the given arguments. Returns a dict with at least
    'success' and optionally 'message', 'installed', 'scene_path', 'prefab_path',
    'error_count', 'warning_count', 'errors', 'warnings'.

    If MCP is disabled or not configured, returns {"success": False, "message": "MCP-Unity plugin is not configured"}.
    """
    if not mcp_available():
        return _placeholder_response()

    try:
        return asyncio.run(_call_tool_async(tool_name, arguments, timeout_seconds))
    except Exception as e:
        return {"success": False, "message": f"MCP client error: {e!s}"}
