# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy solution and project files
COPY UnityMcpServer.sln ./
COPY UnityMcp.Core/UnityMcp.Core.csproj UnityMcp.Core/
COPY UnityMcp.Infrastructure/UnityMcp.Infrastructure.csproj UnityMcp.Infrastructure/
COPY UnityMcp.Application/UnityMcp.Application.csproj UnityMcp.Application/
COPY UnityMcp.Server/UnityMcp.Server.csproj UnityMcp.Server/
COPY UnityMcp.Tests/UnityMcp.Tests.csproj UnityMcp.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build and Publish
WORKDIR /src/UnityMcp.Server
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .

# Install dependencies for Unity CLI if we were to support it in container (Optional)
# RUN apk add --no-cache ...

# Configuration
ENTRYPOINT ["dotnet", "UnityMcp.Server.dll"]
