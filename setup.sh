#!/bin/bash
set -e

# Check for .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo "dotnet command not found. Please install .NET 10.0 SDK."
    exit 1
fi

echo "Found .NET SDK version: $(dotnet --version)"

# Restore and Build
echo "Restoring dependencies..."
dotnet restore

echo "Building solution..."
dotnet build --configuration Release

echo "Build successful! You can now configure Claude Desktop."
