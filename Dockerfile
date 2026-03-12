# Unity MCP Server v3.0.0 — pure .NET, no Unity DLLs required.
# Build and runtime stages for Windows, Linux, and macOS (stdio MCP).

# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for better layer caching
COPY Unity-MCP-Server.sln ./
COPY UnityMcp.Core/UnityMcp.Core.csproj UnityMcp.Core/
COPY UnityMcp.Infrastructure/UnityMcp.Infrastructure.csproj UnityMcp.Infrastructure/
COPY UnityMcp.Application/UnityMcp.Application.csproj UnityMcp.Application/
COPY UnityMcp.Server/UnityMCP.Server.csproj UnityMcp.Server/
COPY UnityMcp.Tests/UnityMCP.Tests.csproj UnityMcp.Tests/

RUN dotnet restore Unity-MCP-Server.sln

# Copy all source (includes Application partials, Docs, etc.)
COPY . .

# Build and publish the server (no tests in image)
RUN dotnet publish UnityMcp.Server/UnityMCP.Server.csproj -c Release -o /app/publish --no-restore

# Runtime Stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV DOTNET_ENVIRONMENT=Production

# MCP uses stdio; no port exposure. Hosts (Claude Desktop, Cursor) start this with stdio connected.
ENTRYPOINT ["dotnet", "UnityMCP.Server.dll"]
