# CI/CD Workflows

This document describes the automated workflows implemented for the Unity MCP Server to ensure code quality, protocol compliance, and reliable releases.

## Overview

The project uses GitHub Actions for continuous integration and delivery. A key feature of our CI setup is the **license-free validation strategy**, which allows us to build and test the core logic without requiring a Unity Editor license in the CI environment.

## Workflows

### 1. .NET CI (`dotnet.yml`)

- **Purpose**: Core logic validation and unit testing.
- **Triggers**: Push to `main`/`master`, Pull Requests.
- **Strategy**: Uses a custom `UnityServer.sln` and `#if !UNITY_EDITOR` stubs to compile core classes in a pure .NET environment.
- **Actions**:
  - Restores dependencies
  - Builds the solution
  - Runs NUnit tests in `UnityMCP.Tests/`
- **Badges**: Indicates the current build status of the core logic.

### 2. Static Analysis (`static_analysis.yml`)

- **Purpose**: Enforce coding standards and formatting.
- **Triggers**: Push to `main`/`master`, Pull Requests.
- **Tools**: `dotnet-format`.
- **Actions**:
  - Checks C# code style and formatting against standard conventions.
  - Fails if formatting violations are found (use `dotnet format` locally to fix).

### 3. Schema Validation (`schema_validation.yml`)

- **Purpose**: Ensure metadata compliance.
- **Triggers**: Push to `main`/ master, Pull Requests.
- **Actions**:
  - Validates `package.json` structure.
  - Ensures UPM (Unity Package Manager) compatibility.
  - Verifies that version strings are consistent.

### 4. UPM Packaging & Release (`upm_packaging.yml`)

- **Purpose**: Automate the release process.
- **Triggers**: Tag push (e.g., `v1.1.0`).
- **Actions**:
  - Validates the package content.
  - Prepares a release distribution.
  - Automates the creation of GitHub Releases with the UPM package structure.

## Local Verification

You can run the primary CI checks locally without Unity:

```bash
# Build the core logic
dotnet build UnityServer.sln

# Run the unit tests
dotnet test UnityServer.sln

# Run formatting check
dotnet format --verify-no-changes
```

## Internal CI Infrastructure

The CI environment relies on:

- `Editor/Core/Stubs/UnityEngineStubs.cs`: Provides minimal stubs for `Debug`, `MonoBehaviour`, and other Unity types used in core logic.
- `UnityServer.sln`: A specialized solution file that includes only the core logic and tests.
- `#if UNITY_EDITOR` guards: Used extensively to decouple protocol logic from editor-only Unity APIs.
