# Build and Pack the Unity MCP Server as a Global .NET Tool

# 1. Clean up old packages
if (Test-Path "./dist") {
    Remove-Item -Recurse -Force "./dist"
}

# 2. Create the package and output to 'dist' folder at root
Write-Host "Packing UnityMcp.Server..." -ForegroundColor Cyan
dotnet pack UnityMcp.Server/UnityMcp.Server.csproj -c Release -o ./dist

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to pack the project."
    exit 1
}

# 3. Check if tool is already installed and uninstall if necessary
$toolCheck = dotnet tool list --global | Select-String "unitymcp.server"
if ($toolCheck) {
    Write-Host "Uninstalling existing version..." -ForegroundColor Yellow
    dotnet tool uninstall --global UnityMcp.Server
}

# 4. Install the tool using the local 'dist' folder as source
Write-Host "Installing Unity-MCP as a global tool..." -ForegroundColor Green
dotnet tool install --global --add-source ./dist UnityMcp.Server

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nInstallation successful!" -ForegroundColor Green
    Write-Host "You can now run the server from anywhere using: " -NoNewline
    Write-Host "unity-mcp" -ForegroundColor Cyan
} else {
    Write-Error "Installation failed."
    exit 1
}
