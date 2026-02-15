# Build and run the Unity MCP Server in a container
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish UnityMCP.Server/UnityMCP.Server.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8765
ENTRYPOINT ["dotnet", "UnityMCP.Server.dll"]
