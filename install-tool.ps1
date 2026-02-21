


# install-tool.ps1 — Build, pack, and update the UnityMcp.Server .NET global tool
#
# Usage: Run this script from the repo root to update your global UnityMcp.Server tool.
#
# Steps performed (exactly as required):
#   1. Uninstall any existing global UnityMcp.Server tool
#   2. Build the solution in Release mode
#   3. Pack the solution (creates .nupkg in UnityMcp.Server/nupkg/)
#   4. Update the global tool from the explicit nupkg directory
#
# Equivalent manual commands:
#   dotnet tool uninstall --global UnityMcp.Server
#   dotnet build Unity-MCP-Server.sln --configuration Release
#   dotnet pack -c Release
#   dotnet tool update --global --add-source "C:\Projects\Unity-MCP-Server\UnityMcp.Server\nupkg" UnityMcp.Server
#
# To test the tool with Inspector:
#   npx @modelcontextprotocol/inspector unity-mcp

Write-Host "Uninstalling any existing UnityMcp.Server tool..." -ForegroundColor Yellow
dotnet tool uninstall --global UnityMcp.Server

Write-Host "Building solution in Release mode..." -ForegroundColor Cyan
dotnet build Unity-MCP-Server.sln --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit 1
}

Write-Host "Packing solution in Release mode..." -ForegroundColor Cyan
dotnet pack -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Error "Pack failed."
    exit 1
}

$nupkgPath = "C:\Projects\Unity-MCP-Server\UnityMcp.Server\nupkg"
Write-Host "Updating global tool from $nupkgPath..." -ForegroundColor Green
dotnet tool update --global --add-source $nupkgPath UnityMcp.Server
if ($LASTEXITCODE -eq 0) {
    Write-Host "`nInstallation successful!" -ForegroundColor Green
    Write-Host "You can now run the server from anywhere using: " -NoNewline
    Write-Host "unity-mcp" -ForegroundColor Cyan
    Write-Host "\nTo test with Inspector, run: npx @modelcontextprotocol/inspector unity-mcp" -ForegroundColor Magenta
} else {
    Write-Error "Installation failed."
    exit 1
}
