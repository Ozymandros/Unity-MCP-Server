# Contributing to Unity MCP Server

Thank you for your interest in contributing to the Unity MCP Server! We welcome contributions from the community to make this tool better for everyone.

## How to Contribute

### Reporting Issues

If you find a bug or have a feature request, please open an issue in the GitHub repository. Provide as much detail as possible, including:

- Unity version
- Operating system
- Steps to reproduce (for bugs)
- Expected behavior vs. actual behavior

### Submitting Pull Requests

1. **Fork the repository** and clone it locally.
2. **Create a new branch** for your feature or bug fix:
   ```bash
   git checkout -b feature/my-new-feature
   ```
3. **Make your changes**. Ensure your code follows the existing style and conventions.
4. **Test your changes** in a Unity project to ensure everything works as expected.
5. **Commit your changes**:
   ```bash
   git commit -m "Add some feature"
   ```
6. **Push to your branch**:
   ```bash
   git push origin feature/my-new-feature
   ```
7. **Open a Pull Request** against the `main` branch. Describe your changes and link to any relevant issues.

## Development Setup

1. Open the project folder in Unity Hub (add as a new project).
2. Open the project in Unity Editor (2020.3 or later recommended).
3. The server should start automatically. Check the Console for "Unity MCP Server started" messages.

## Code Style

- Use standard C# naming conventions (PascalCase for classes/methods, camelCase for local variables).
- Include XML documentation comments for public classes and methods.
- Keep tools focused on a single responsibility.
- Ensure all Unity API calls are thread-safe (use `McpDispatcher` if needed, though tools run on main thread by default).

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
