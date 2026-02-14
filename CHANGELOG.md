# Changelog

All notable changes to the Unity MCP Server project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-02-14

### Added

- **License-Free CI/CD Infrastructure**:
  - Pure .NET build support using `UnityServer.sln`.
  - Standalone compilation capability without Unity Editor license.
  - `UnityEngineStubs.cs` for mocking Unity types in external builds.
  - GitHub Actions workflows for `.NET CI`, `Static Analysis`, and `Schema Validation`.
- **Unit Test Suite**:
  - New `UnityMCP.Tests` project using NUnit.
  - Core protocol logic validation (McpMessage, McpDispatcher, McpToolRegistry).
  - Mocked dispatcher queue processing for non-Unity environments.
- **Project Organization**:
  - Reorganized file structure into standard Unity Package (UPM) layout.
  - Added project-level `CONTRIBUTING.md` and `LICENSE`.
- **Documentation**:
  - Comprehensive `CI_CD.md` guide for automated workflows.
  - Updated README with professional badge system and UPM installation notes.

### Changed

- Moved core scripts to `Editor/Core/` and tools to `Editor/Tools/`.
- Updated all core files version constants to `1.1.0`.
- ProcessMainThreadQueue in `McpDispatcher` made public for test accessibility.
- RegisterAllTools in `McpToolRegistry` made public for discovery testing.

### Fixed

- Improved JSON serialization compatibility with .NET Standard 2.0.
- Resolved static constructor auto-start issues in test environments.

## [1.0.0] - 2025-02-14

### Added

#### Core Infrastructure

- **McpServer**: Main TCP server implementing Model Context Protocol
  - Listens on localhost:8765 by default
  - Supports multiple concurrent client connections
  - Automatic startup with Unity Editor via `[InitializeOnLoad]`
  - Graceful shutdown on editor quit
  - Message buffering with newline delimiters
  - Thread-safe client handling

- **McpMessage**: JSON-RPC 2.0 message serialization
  - Request/response parsing and validation
  - Error response creation with standard error codes
  - Support for various ID types (string, number, null)
  - Null-aware serialization with `JsonIgnoreCondition`

- **McpDispatcher**: Main thread execution coordinator
  - Thread-safe action queue using `lock`
  - `TaskCompletionSource` for async coordination
  - Integration with `EditorApplication.update`
  - Support for both direct method calls and `tools/call` format
  - Comprehensive error handling and propagation

- **McpToolRegistry**: Dynamic tool discovery and management
  - Reflection-based tool discovery across all assemblies
  - Automatic registration on Unity compile
  - Tool lookup by name
  - Schema generation for `tools/list` responses
  - Support for tool registration/unregistration

#### Protocol Support

- **initialize** method for MCP handshake
- **tools/list** method for tool discovery
- **tools/call** method for standard MCP tool invocation
- Direct tool method calls (backward compatibility)
- Protocol version: 2025-11-25

#### Built-in Tools

- **PingTool**: Connectivity testing with optional echo
- **CreateSceneTool**: Scene creation with setup options
- **CreateGameObjectTool**: GameObject instantiation with primitives
- **GetSceneInfoTool**: Scene introspection and hierarchy
- **CreateScriptTool**: C# script generation with templates

#### Tool Features

- **BaseMcpTool**: Abstract base class for tool development
  - Helper methods for parameter extraction
  - JSON Schema support for input validation
  - Consistent error handling pattern

#### Documentation

- Comprehensive README with overview and examples
- Detailed INSTALLATION guide with troubleshooting
- Complete API_REFERENCE with all methods and types
- ARCHITECTURE documentation explaining design
- Python client example with full API coverage
- Node.js client example with async/await support

#### Developer Experience

- Auto-discovery of custom tools via reflection
- Simple tool creation with `IMcpTool` interface
- Type-safe parameter helpers in `BaseMcpTool`
- Extensive logging for debugging
- Unity Console integration for all events

### Features

#### Thread Safety

- All Unity API calls automatically execute on main thread
- No manual thread synchronization needed in tools
- Queue-based execution ensures serial processing
- Exception isolation per tool execution

#### Error Handling

- JSON-RPC 2.0 compliant error codes
- Detailed error messages with stack traces
- Tool-level exception catching
- Network error recovery

#### Extensibility

- Reflection-based tool discovery
- Plugin architecture for custom tools
- No core modification needed for new tools
- Support for multiple tool implementations

### Technical Details

#### Dependencies

- Unity Editor 2020.3 or later
- .NET Standard 2.0
- System.Text.Json (built-in Unity 2020.3+)

#### Performance

- Minimal idle overhead (~1-2 MB memory)
- ~100 KB per client connection
- 8 KB message buffer per client
- Sub-millisecond dispatch overhead

#### Network

- TCP transport on IPv4 loopback
- JSON-RPC 2.0 message format
- Newline-delimited message framing
- No authentication (localhost-only)

### Testing

- Manual testing with example clients
- Python client example validates all tools
- Node.js client example demonstrates async patterns
- Ping tool for connectivity verification

### Known Limitations

- Single protocol version (2025-11-25)
- No authentication mechanism
- Localhost-only connections
- No encryption (plain TCP)
- No built-in rate limiting
- Tools must complete before next frame

### Future Roadmap

- Additional Unity tools (assets, build, etc.)
- Authentication and authorization
- WebSocket transport option
- Resource system for file access
- Event streaming for notifications
- Progress reporting for long operations

## [Unreleased]

### Planned

- Asset management tools
- Build automation tools
- Prefab manipulation tools
- Material and shader tools
- Animation tools
- Physics simulation controls
- Audio management tools
- Version control integration
- Package management tools
- Test runner integration

### Under Consideration

- WebSocket transport
- HTTP REST endpoints
- stdin/stdout transport (for subprocess mode)
- Authentication via API keys
- Role-based access control
- Request rate limiting
- Encryption via TLS
- Batch operations
- Transaction support
- Undo/redo integration

---

## Infrastructure

### Version Numbering

- MAJOR: Breaking changes to MCP protocol or API
- MINOR: New features, backward compatible
- PATCH: Bug fixes, backward compatible

### Support Policy

- Current version: Full support
- Previous minor: Security fixes
- Older versions: Community support only

### Upgrade Guide

- From 1.0.0 to 1.1.0: Files have been moved to subdirectories. Update your project structure or re-import the package.

---

## Contributing

Contributions are welcome! Please see CONTRIBUTING.md for guidelines.

## License

See LICENSE file for details.

## Acknowledgments

- Model Context Protocol specification by Anthropic
- Unity Technologies for the Unity Editor API
- JSON-RPC 2.0 specification authors
- Open source community for inspiration and feedback
