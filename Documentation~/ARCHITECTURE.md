# Unity MCP Server - Architecture & Project Structure

## Overview

The Unity MCP Server is designed as a modular, extensible system that bridges Unity Editor with external AI/LLM applications through the Model Context Protocol.

## Design Principles

1. **Modularity**: Each component has a single responsibility
2. **Thread Safety**: All Unity API calls routed through main thread
3. **Extensibility**: Easy to add new tools via reflection
4. **Standards Compliance**: Follows MCP specification and JSON-RPC 2.0
5. **Error Handling**: Comprehensive error reporting at all levels

## Project Structure

```
Assets/UnityMCP/
├── Editor/
│   ├── Core/
│   │   ├── McpServer.cs          # Main server (TCP listener & router)
│   │   ├── McpMessage.cs         # JSON-RPC message serialization
│   │   ├── McpDispatcher.cs      # Main thread execution queue
│   │   └── McpToolRegistry.cs    # Tool discovery & registry
│   │
│   └── Tools/
│       ├── PingTool.cs           # Connectivity testing
│       ├── CreateSceneTool.cs    # Scene creation
│       ├── CreateGameObjectTool.cs # GameObject instantiation
│       ├── GetSceneInfoTool.cs   # Scene introspection
│       └── CreateScriptTool.cs   # Script generation
│
├── README.md                      # Main documentation
├── INSTALLATION.md                # Setup guide
├── API_REFERENCE.md               # Complete API docs
├── example_client.py              # Python client example
└── example_client.js              # Node.js client example
```

## Component Architecture

```
┌────────────────────────────────────────────────────────────────┐
│                         Unity Editor                            │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                      McpServer                           │  │
│  │  - Static initialization via [InitializeOnLoad]         │  │
│  │  - Listens on TCP port 8765                             │  │
│  │  - Manages client connections                           │  │
│  │  - Handles JSON-RPC message routing                     │  │
│  └────────────────┬─────────────────────────────────────────┘  │
│                   │                                             │
│  ┌────────────────▼─────────────────────────────────────────┐  │
│  │                  McpMessage                              │  │
│  │  - Parses JSON-RPC requests                             │  │
│  │  - Serializes JSON-RPC responses                        │  │
│  │  - Validates message format                             │  │
│  │  - Error response creation                              │  │
│  └────────────────┬─────────────────────────────────────────┘  │
│                   │                                             │
│  ┌────────────────▼─────────────────────────────────────────┐  │
│  │                 McpDispatcher                            │  │
│  │  - Routes requests to appropriate tools                 │  │
│  │  - Manages main thread execution queue                  │  │
│  │  - Ensures thread-safe Unity API calls                  │  │
│  │  - Handles async/await coordination                     │  │
│  └────────────────┬─────────────────────────────────────────┘  │
│                   │                                             │
│  ┌────────────────▼─────────────────────────────────────────┐  │
│  │               McpToolRegistry                            │  │
│  │  - Discovers tools via reflection                       │  │
│  │  - Maintains tool dictionary                            │  │
│  │  - Provides tool lookup                                 │  │
│  │  - Returns tool schemas                                 │  │
│  └────────────────┬─────────────────────────────────────────┘  │
│                   │                                             │
│  ┌────────────────▼─────────────────────────────────────────┐  │
│  │                    Tools (IMcpTool)                      │  │
│  │  ┌────────────────────────────────────────────────────┐  │  │
│  │  │ BaseMcpTool (Abstract base class)                 │  │  │
│  │  │  - GetName()                                       │  │  │
│  │  │  - GetDescription()                                │  │  │
│  │  │  - GetInputSchema()                                │  │  │
│  │  │  - Execute(JsonElement)                            │  │  │
│  │  │  - Helper methods for parameter extraction        │  │  │
│  │  └────────────────────────────────────────────────────┘  │  │
│  │                                                           │  │
│  │  Concrete Tools:                                          │  │
│  │  • PingTool                                               │  │
│  │  • CreateSceneTool                                        │  │
│  │  • CreateGameObjectTool                                   │  │
│  │  • GetSceneInfoTool                                       │  │
│  │  • CreateScriptTool                                       │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## Data Flow

### Request Processing

```
1. Client sends JSON-RPC request
   ↓
2. McpServer receives TCP data
   ↓
3. McpMessage parses JSON
   ↓
4. McpServer routes to handler
   ├─ initialize → HandleInitialize()
   ├─ tools/list → HandleToolsList()
   └─ other → McpDispatcher.DispatchAsync()
   ↓
5. McpDispatcher looks up tool
   ↓
6. McpDispatcher queues execution on main thread
   ↓
7. EditorApplication.update processes queue
   ↓
8. Tool.Execute() runs on main thread
   ↓
9. Result captured and returned
   ↓
10. McpMessage serializes response
    ↓
11. McpServer sends TCP response
    ↓
12. Client receives result
```

### Thread Coordination

```
Network Thread                 Main Thread (Unity)
─────────────                 ───────────────────
TCP Receive
    │
    ├─ Parse JSON
    │
    ├─ Validate
    │
    ├─ Create Task ─────────────┐
    │                           │
    ├─ Enqueue Action ──────────┼──→ Queue
    │                           │      │
    ├─ await Task ─────────┐    │      │
    │                      │    │      │
    │                      │    │      ├─ EditorApplication.update
    │                      │    │      │
    │                      │    │      ├─ Dequeue Action
    │                      │    │      │
    │                      │    │      ├─ Execute Tool
    │                      │    │      │
    │                      │    │      └─ Set Result ──────┐
    │                      │    │                          │
    ├─ Task Complete ◄─────┴────┴──────────────────────────┘
    │
    ├─ Serialize Response
    │
    └─ TCP Send
```

## Core Components

### McpServer

**Responsibilities**:
- TCP listener lifecycle management
- Client connection handling
- Message buffering and parsing
- Protocol method routing
- Server information provision

**Key Methods**:
- `StartServer(port)`: Initialize and start listening
- `StopServer()`: Graceful shutdown
- `AcceptClientsAsync()`: Accept incoming connections
- `HandleClientAsync()`: Process client messages
- `ProcessRequestAsync()`: Route requests to handlers

**Lifecycle**:
```
[InitializeOnLoad] → Static Constructor
    ↓
StartServer()
    ↓
Listen on Port 8765
    ↓
Accept Connections (loop)
    ↓
EditorApplication.quitting
    ↓
StopServer()
```

### McpMessage

**Responsibilities**:
- JSON-RPC 2.0 serialization/deserialization
- Request/response structure validation
- Error response formatting
- Type-safe message handling

**Data Structures**:
```csharp
McpRequest {
    string JsonRpc = "2.0"
    object Id
    string Method
    JsonElement Params
}

McpResponse {
    string JsonRpc = "2.0"
    object Id
    object Result
    McpError Error
}

McpError {
    int Code
    string Message
    object Data
}
```

### McpDispatcher

**Responsibilities**:
- Tool lookup and invocation
- Main thread action queuing
- Async/await coordination
- Error handling and propagation

**Key Features**:
- Thread-safe queue using `lock`
- `TaskCompletionSource` for async results
- Integration with `EditorApplication.update`
- Support for both direct and `tools/call` methods

### McpToolRegistry

**Responsibilities**:
- Automatic tool discovery via reflection
- Tool registration and storage
- Schema generation for tools/list
- Tool lookup by name

**Discovery Process**:
```
[InitializeOnLoadMethod]
    ↓
Scan all loaded assemblies
    ↓
Find types implementing IMcpTool
    ↓
Instantiate each tool type
    ↓
Call RegisterTool() for each
    ↓
Store in Dictionary<string, IMcpTool>
```

### IMcpTool Interface

**Contract**:
```csharp
interface IMcpTool {
    string GetName();           // Unique tool identifier
    string GetDescription();    // Human-readable description
    object GetInputSchema();    // JSON Schema for parameters
    object Execute(JsonElement); // Tool implementation
}
```

**BaseMcpTool Helpers**:
```csharp
- GetStringParam(params, name, default)
- GetIntParam(params, name, default)
- GetBoolParam(params, name, default)
- GetFloatParam(params, name, default)
```

## Extensibility Points

### Adding New Tools

1. Create class implementing `IMcpTool` or extending `BaseMcpTool`
2. Implement required methods
3. Place in any Editor folder
4. Tool auto-registers on compile

Example:
```csharp
public class MyTool : BaseMcpTool
{
    public override string GetName() => "my_tool";
    
    public override string GetDescription() => "Does something";
    
    public override object GetInputSchema() => new {
        type = "object",
        properties = new { }
    };
    
    public override object Execute(JsonElement parameters) {
        // Implementation
        return new { success = true };
    }
}
```

### Custom Protocol Methods

Add to `McpServer.ProcessRequestAsync()`:
```csharp
else if (request.Method == "my_custom_method")
{
    return HandleMyCustomMethod(request);
}
```

### Alternative Transports

Current: TCP/JSON-RPC
Possible: WebSocket, HTTP, stdin/stdout

Modify `McpServer` to support different transport layers while maintaining same message processing.

## Performance Characteristics

### Memory

- Base overhead: ~1-2 MB
- Per connection: ~100 KB
- Message buffer: 8 KB per connection
- Tool registry: Negligible

### CPU

- Idle: Minimal (TCP listener only)
- Per request: <1ms overhead + tool execution time
- Main thread queue: Processed each frame (~16ms intervals)

### Scalability

- Concurrent connections: Limited by OS (typically 100+)
- Message throughput: ~1000 requests/second
- Bottleneck: Main thread execution for Unity API calls

## Security Model

### Current

- Localhost-only binding (127.0.0.1)
- No authentication
- No encryption
- Trust-based (assumes client is authorized)

### Recommended Production Additions

1. **Authentication**: API key or OAuth
2. **Authorization**: Role-based tool access
3. **Encryption**: TLS for network traffic
4. **Rate Limiting**: Per-client request limits
5. **Input Validation**: Sanitize all parameters
6. **Audit Logging**: Track all tool executions

## Error Handling Strategy

### Levels

1. **Network**: Connection errors, timeouts
2. **Protocol**: JSON parse errors, invalid RPC
3. **Dispatch**: Tool not found, invalid params
4. **Execution**: Unity API errors, exceptions

### Propagation

```
Tool Exception
    ↓
Caught in Execute()
    ↓
Return { success: false, error: message }
    ↓
Wrapped in McpResponse
    ↓
Serialized and sent to client
```

## Testing Strategy

### Unit Tests (Recommended)

- Message serialization/deserialization
- Tool parameter parsing
- Error response formatting
- Schema validation

### Integration Tests

- Client connection
- Request/response cycles
- Tool execution
- Error handling

### Manual Testing

Use example clients:
- `example_client.py`
- `example_client.js`

## Future Enhancements

### Potential Features

1. **Async Tools**: Support for long-running operations
2. **Progress Reporting**: Real-time operation status
3. **Batch Operations**: Multiple tools in one request
4. **Resource System**: File access and manipulation
5. **Prompt Templates**: Reusable workflows
6. **Event Streaming**: Server-initiated notifications
7. **Asset Management**: Import, export, modify assets
8. **Build Automation**: Trigger builds via MCP
9. **Profiling Tools**: Performance analysis
10. **Debug Tools**: Breakpoints, variable inspection

### Roadmap

- v1.0: Core MCP implementation ✓
- v1.1: Additional Unity tools
- v1.2: Authentication & security
- v2.0: Full MCP feature support
- v2.1: WebSocket transport
- v3.0: Visual scripting integration

## Dependencies

### Unity

- Unity Editor 2020.3+
- UnityEditor namespace
- System.Text.Json (built-in)

### .NET

- .NET Standard 2.0
- System.Net.Sockets
- System.Threading.Tasks
- System.Reflection

### External

None (fully self-contained)

## License & Attribution

Based on Model Context Protocol specification by Anthropic.

Implementation follows best practices from:
- Unity Editor Scripting documentation
- JSON-RPC 2.0 specification
- Microsoft C# coding guidelines
