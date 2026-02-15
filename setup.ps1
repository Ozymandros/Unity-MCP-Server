# Check for .NET SDK
$dotnetVersion = dotnet --version
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet command not found. Please install .NET 10.0 SDK."
    exit 1
}

Write-Host "Found .NET SDK version: $dotnetVersion"

# Restore and Build
Write-Host "Restoring dependencies..."
dotnet restore

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to restore dependencies."
    exit 1
}

Write-Host "Building solution..."
dotnet build --configuration Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful! You can now configure Claude Desktop." -ForegroundColor Green
} else {
    Write-Error "Build failed."
    exit 1
}
