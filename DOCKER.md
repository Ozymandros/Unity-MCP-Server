# Docker Integration (Legal & Functional)

To use the Unity MCP Server within a Docker container while complying with Unity's licensing (by not redistributing their DLLs), you can mount your local Unity installation as a read-only volume.

This approach ensures that:

1. **Legality**: You are not redistributing `UnityEditor.dll` or `UnityEngine.dll` inside the image.
2. **Functionality**: The server has access to the native assemblies at runtime.
3. **Consistency**: The same image can be used with different Unity versions by just changing the volume mount.

## 🚀 Running with Docker

### 1. Build the Image

First, build the server image. You may need to provide the Unity DLLs to the build context or build locally first if you are using a strictly isolated build environment.

```bash
docker build -t unity-mcp-server .
```

### 2. Run with Volume Mount

Mount your Unity Editor's `Managed` folder to a directory inside the container (e.g., `/unity`) and set the `UNITY_PATH` environment variable.

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

In the `.csproj` file, we use a dynamic path that prioritizes the `UNITY_PATH` environment variable:

```xml
<PropertyGroup>
  <UnityManagedPath Condition="'$(UNITY_PATH)' != ''">$(UNITY_PATH)</UnityManagedPath>
  <UnityEditorPath Condition="'$(UnityEditorPath)' == ''">$(UnityManagedPath)\UnityEditor.dll</UnityEditorPath>
</PropertyGroup>

<ItemGroup>
  <Reference Include="UnityEditor">
    <HintPath>$(UnityEditorPath)</HintPath>
  </Reference>
</ItemGroup>
```

## 📝 Configuration (Claude Desktop)

When using Docker via Claude Desktop, you can configure it like this:

```json
{
  "mcpServers": {
    "unity-docker": {
      "command": "docker",
      "args": [
        "run",
        "-i",
        "--rm",
        "-v", "C:/Program Files/Unity/Hub/Editor/6000.3.2f1/Editor/Data/Managed:/unity:ro",
        "-e", "UNITY_PATH=/unity",
        "unity-mcp-server"
      ]
    }
  }
}
```
