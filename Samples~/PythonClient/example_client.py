#!/usr/bin/env python3
"""
Unity MCP Server Python Client Example

This script demonstrates how to connect to and interact with the Unity MCP Server.
"""

import socket
import json
import time
from typing import Dict, Any, Optional


class UnityMCPClient:
    """Client for communicating with Unity MCP Server."""
    
    def __init__(self, host: str = 'localhost', port: int = 8765, timeout: int = 30):
        """
        Initialize the MCP client.
        
        Args:
            host: Server hostname
            port: Server port
            timeout: Socket timeout in seconds
        """
        self.host = host
        self.port = port
        self.timeout = timeout
        self.sock = None
        self.request_id = 0
        self.connected = False
        
    def connect(self) -> bool:
        """
        Connect to the Unity MCP Server.
        
        Returns:
            True if connection successful, False otherwise
        """
        try:
            self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.sock.settimeout(self.timeout)
            self.sock.connect((self.host, self.port))
            self.connected = True
            print(f"✓ Connected to Unity MCP Server at {self.host}:{self.port}")
            return True
        except Exception as e:
            print(f"✗ Connection failed: {e}")
            return False
    
    def send_request(self, method: str, params: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        """
        Send a JSON-RPC request to the server.
        
        Args:
            method: RPC method name
            params: Method parameters
            
        Returns:
            Server response as dictionary
        """
        if not self.connected:
            raise RuntimeError("Not connected to server")
        
        self.request_id += 1
        request = {
            "jsonrpc": "2.0",
            "id": str(self.request_id),
            "method": method,
            "params": params or {}
        }
        
        try:
            # Send request
            message = json.dumps(request) + "\n"
            self.sock.sendall(message.encode('utf-8'))
            
            # Receive response
            response_data = b""
            while b"\n" not in response_data:
                chunk = self.sock.recv(8192)
                if not chunk:
                    raise RuntimeError("Connection closed by server")
                response_data += chunk
            
            response = json.loads(response_data.decode('utf-8').strip())
            
            # Check for errors
            if "error" in response:
                error = response["error"]
                print(f"✗ Error {error['code']}: {error['message']}")
                if "data" in error:
                    print(f"  Details: {error['data']}")
            
            return response
        except Exception as e:
            print(f"✗ Request failed: {e}")
            return {"error": str(e)}
    
    def initialize(self) -> Dict[str, Any]:
        """
        Send MCP initialize handshake.
        
        Returns:
            Server capabilities and info
        """
        return self.send_request("initialize", {
            "protocolVersion": "2025-11-25",
            "clientInfo": {
                "name": "Unity MCP Python Client",
                "version": "1.0.0"
            }
        })
    
    def list_tools(self) -> Dict[str, Any]:
        """
        List all available tools.
        
        Returns:
            List of tools with descriptions
        """
        return self.send_request("tools/list", {})
    
    def call_tool(self, tool_name: str, arguments: Dict[str, Any]) -> Dict[str, Any]:
        """
        Call a tool using standard MCP format.
        
        Args:
            tool_name: Name of the tool
            arguments: Tool arguments
            
        Returns:
            Tool execution result
        """
        return self.send_request("tools/call", {
            "name": tool_name,
            "arguments": arguments
        })
    
    def ping(self, message: str = "") -> Dict[str, Any]:
        """Test connectivity with ping."""
        params = {"message": message} if message else {}
        return self.send_request("ping", params)
    
    def create_scene(self, name: str, path: str = "Assets/Scenes", 
                    setup: str = "default") -> Dict[str, Any]:
        """
        Create a new Unity scene.
        
        Args:
            name: Scene name
            path: Path to save scene
            setup: 'default' or 'empty'
        """
        return self.send_request("create_scene", {
            "name": name,
            "path": path,
            "setup": setup
        })
    
    def create_gameobject(self, name: str, obj_type: str = "empty",
                         position: Optional[Dict[str, float]] = None,
                         parent: Optional[str] = None) -> Dict[str, Any]:
        """
        Create a GameObject in the scene.
        
        Args:
            name: GameObject name
            obj_type: Type (empty, cube, sphere, etc.)
            position: {x, y, z} position
            parent: Optional parent GameObject name
        """
        params = {
            "name": name,
            "type": obj_type
        }
        if position:
            params["position"] = position
        if parent:
            params["parent"] = parent
            
        return self.send_request("create_gameobject", params)
    
    def get_scene_info(self, include_hierarchy: bool = True,
                      include_components: bool = False) -> Dict[str, Any]:
        """
        Get information about the current scene.
        
        Args:
            include_hierarchy: Include full GameObject hierarchy
            include_components: Include component information
        """
        return self.send_request("get_scene_info", {
            "includeHierarchy": include_hierarchy,
            "includeComponents": include_components
        })
    
    def create_script(self, name: str, script_type: str = "monobehaviour",
                     path: str = "Assets/Scripts",
                     namespace: Optional[str] = None) -> Dict[str, Any]:
        """
        Create a C# script file.
        
        Args:
            name: Script class name
            script_type: Type (monobehaviour, scriptableobject, plain, interface)
            path: Path to save script
            namespace: Optional namespace
        """
        params = {
            "name": name,
            "type": script_type,
            "path": path
        }
        if namespace:
            params["namespace"] = namespace
            
        return self.send_request("create_script", params)
    
    def close(self):
        """Close the connection."""
        if self.sock:
            self.sock.close()
            self.connected = False
            print("✓ Connection closed")


def main():
    """Example usage of Unity MCP Client."""
    
    print("Unity MCP Server - Python Client Example")
    print("=" * 50)
    print()
    
    # Create client
    client = UnityMCPClient()
    
    # Connect
    if not client.connect():
        return
    
    print()
    
    # Initialize
    print("1. Initializing connection...")
    response = client.initialize()
    if "result" in response:
        server_info = response["result"]["serverInfo"]
        print(f"   Connected to: {server_info['name']} v{server_info['version']}")
    print()
    
    # Ping test
    print("2. Testing connectivity...")
    response = client.ping("Hello Unity!")
    if "result" in response:
        print(f"   Server responded: {response['result']['message']}")
    print()
    
    # List tools
    print("3. Listing available tools...")
    response = client.list_tools()
    if "result" in response and "tools" in response["result"]:
        tools = response["result"]["tools"]
        print(f"   Found {len(tools)} tools:")
        for tool in tools:
            print(f"   - {tool['name']}: {tool['description']}")
    print()
    
    # Get scene info
    print("4. Getting current scene info...")
    response = client.get_scene_info(include_hierarchy=False)
    if "result" in response:
        scene = response["result"]
        print(f"   Scene: {scene['name']}")
        print(f"   Path: {scene['path']}")
        print(f"   Objects: {scene.get('totalObjectCount', 'N/A')}")
    print()
    
    # Example: Create a new scene
    print("5. Creating new scene...")
    response = client.create_scene("ExampleScene", setup="default")
    if "result" in response:
        result = response["result"]
        if result.get("success"):
            print(f"   ✓ {result['message']}")
        else:
            print(f"   ✗ {result.get('error', 'Unknown error')}")
    print()
    
    # Example: Create GameObjects
    print("6. Creating GameObjects...")
    
    # Create a cube
    response = client.create_gameobject("PlayerCube", "cube", {"x": 0, "y": 1, "z": 0})
    if "result" in response and response["result"].get("success"):
        print(f"   ✓ Created cube at position (0, 1, 0)")
    
    # Create a sphere
    response = client.create_gameobject("EnemySphere", "sphere", {"x": 5, "y": 1, "z": 0})
    if "result" in response and response["result"].get("success"):
        print(f"   ✓ Created sphere at position (5, 1, 0)")
    print()
    
    # Example: Create a script
    print("7. Creating a MonoBehaviour script...")
    response = client.create_script("PlayerController", "monobehaviour", 
                                   namespace="Game.Player")
    if "result" in response:
        result = response["result"]
        if result.get("success"):
            print(f"   ✓ {result['message']}")
        else:
            print(f"   ✗ {result.get('error', 'Unknown error')}")
    print()
    
    # Get final scene info
    print("8. Final scene state...")
    response = client.get_scene_info(include_hierarchy=False)
    if "result" in response:
        scene = response["result"]
        print(f"   Scene now has {scene.get('totalObjectCount', 'N/A')} total objects")
    print()
    
    # Close connection
    client.close()
    
    print()
    print("=" * 50)
    print("Example completed successfully!")


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n\nInterrupted by user")
    except Exception as e:
        print(f"\n\nError: {e}")
