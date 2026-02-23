# CWM.RoslynNavigator — Roslyn MCP Server

> Token-efficient .NET codebase navigation via Roslyn semantic analysis.

## Overview

CWM.RoslynNavigator is a Model Context Protocol (MCP) server that provides Claude Code with semantic understanding of .NET solutions. Instead of reading entire source files (hundreds of tokens), Claude can query for specific symbols, references, and type hierarchies (tens of tokens).

## Prerequisites

- .NET 10 SDK
- A .NET solution file (`.sln` or `.slnx`)

## Tools

| Tool | Description |
|------|-------------|
| `find_symbol` | Find where a type, method, or property is defined |
| `find_references` | All usages of a symbol across the solution |
| `find_implementations` | Types that implement an interface or derive from a base class |
| `find_callers` | All methods that call a specific method |
| `find_overrides` | Overrides of a virtual or abstract method |
| `find_dead_code` | Unused types, methods, and properties |
| `get_type_hierarchy` | Inheritance chain, interfaces, and derived types |
| `get_public_api` | Public members of a type without reading the full file |
| `get_symbol_detail` | Full signature, parameters, return type, and XML docs |
| `get_project_graph` | Solution project dependency tree |
| `get_dependency_graph` | Call dependency graph for a method |
| `get_diagnostics` | Compiler and analyzer warnings/errors |
| `get_test_coverage_map` | Heuristic test coverage by naming convention |
| `detect_antipatterns` | .NET anti-patterns (async void, sync-over-async, etc.) |
| `detect_circular_dependencies` | Circular dependency detection at project or type level |

## Installation

### As a Global Tool (Recommended)

```bash
dotnet tool install -g CWM.RoslynNavigator
```

Then add to your Claude Code global settings (`~/.claude/settings.json`):

```json
{
  "mcpServers": {
    "cwm-roslyn-navigator": {
      "command": "cwm-roslyn-navigator",
      "args": ["--solution", "${workspaceFolder}"]
    }
  }
}
```

The `--solution` argument accepts either a direct path to a `.sln`/`.slnx` file or a directory to scan for solution files.

### As a Local Tool (per-repo)

```bash
dotnet new tool-manifest   # if you don't have one
dotnet tool install CWM.RoslynNavigator
```

Then add to your project's `.mcp.json`:

```json
{
  "mcpServers": {
    "cwm-roslyn-navigator": {
      "command": "dotnet",
      "args": ["tool", "run", "cwm-roslyn-navigator", "--", "--solution", "${workspaceFolder}"]
    }
  }
}
```

### From Source (for contributors)

```bash
dotnet run --project mcp/CWM.RoslynNavigator/CWM.RoslynNavigator.csproj -- --solution /path/to/your/Solution.sln
```

## Solution Discovery

The server resolves the solution file in this order:

1. **Explicit `--solution` argument** — Pass a `.sln`/`.slnx` file path directly, or a directory to scan
2. **Working directory scan** — If no argument, scans the current working directory for solution files
3. **Deterministic selection** — If multiple solutions exist, prefers one in the root directory, otherwise picks the first alphabetically

## Architecture

```
Program.cs              → MSBuildLocator → Host → MCP stdio transport
WorkspaceManager.cs     → MSBuildWorkspace lifecycle, file watching, compilation caching
WorkspaceInitializer.cs → BackgroundService triggers workspace load on startup
SolutionDiscovery.cs    → Auto-detect .sln/.slnx from args or working directory
SymbolResolver.cs       → Cross-project symbol resolution with disambiguation
Tools/                  → MCP tool implementations (15 read-only tools)
Responses/              → Token-optimized JSON response DTOs
```

## Scaling

| Solution Size | Strategy |
|---|---|
| Small (1-15 projects) | Load entire workspace on startup, keep compilations warm |
| Large (15-50 projects) | Lazy-load compilations on first query per project |
| Enterprise (50+) | Lazy loading + warn if query touches unloaded project |

## Development

```bash
# Build
dotnet build mcp/CWM.RoslynNavigator/CWM.RoslynNavigator.csproj

# Run tests
dotnet test mcp/CWM.RoslynNavigator/tests/CWM.RoslynNavigator.Tests.csproj

# Run manually against a directory
dotnet run --project mcp/CWM.RoslynNavigator/CWM.RoslynNavigator.csproj -- --solution /path/to/your/project/

# Run manually against a solution file
dotnet run --project mcp/CWM.RoslynNavigator/CWM.RoslynNavigator.csproj -- --solution /path/to/your/Solution.sln
```
