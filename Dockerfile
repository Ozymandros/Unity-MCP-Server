# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# ARGs for build-time configuration
ARG UNITY_PATH=/unity_libs

# Copy solution and project files first for better caching
COPY UnityMcpServer.slnx ./
COPY UnityMcp.Core/UnityMcp.Core.csproj UnityMcp.Core/
COPY UnityMcp.Infrastructure/UnityMcp.Infrastructure.csproj UnityMcp.Infrastructure/
COPY UnityMcp.Application/UnityMcp.Application.csproj UnityMcp.Application/
COPY UnityMcp.Server/UnityMcp.Server.csproj UnityMcp.Server/
COPY UnityMcp.Tests/UnityMcp.Tests.csproj UnityMcp.Tests/

# Note: For the build to succeed, Unity DLLs must be present in the build context
# and mapped via /p:UNITY_PATH during restore/publish.
RUN dotnet restore /p:UNITY_PATH=${UNITY_PATH}

# Copy source code
COPY . .

# Build and Publish
WORKDIR /src/UnityMcp.Server
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false /p:UNITY_PATH=${UNITY_PATH}

# Runtime Stage
FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .

# Environment Variables
ENV DOTNET_ENVIRONMENT=Production
# Default to /unity for volume mounting at runtime
ENV UNITY_PATH=/unity

# Configuration
# MCP uses Stdio, so we must run in a shell that keeps stdin open
ENTRYPOINT ["dotnet", "UnityMcp.Server.dll"]
