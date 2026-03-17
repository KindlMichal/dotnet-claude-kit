using System.Collections.Concurrent;
using System.Text.Json;
using CWM.RoslynNavigator.Responses;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CWM.RoslynNavigator;

/// <summary>
/// Manages the MSBuildWorkspace lifecycle: loading, on-demand refresh, and compilation caching.
/// File watching is intentionally avoided — on Linux/WSL, recursive FileSystemWatcher creates
/// one inotify watch per subdirectory (including bin/obj/.git), quickly exhausting the kernel limit.
/// Instead, documents are refreshed on demand when tools are invoked.
/// </summary>
public sealed class WorkspaceManager : IDisposable
{
    private const int LazyLoadThreshold = 50;
    private const int MaxCachedCompilations = 30;

    private readonly ILogger<WorkspaceManager> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly ConcurrentDictionary<ProjectId, Compilation> _compilationCache = new();
    private readonly ConcurrentDictionary<ProjectId, long> _cacheAccessOrder = new();
    private readonly ConcurrentDictionary<DocumentId, DateTime> _knownFileTimestamps = new();
    private readonly ConcurrentDictionary<string, DateTime> _projectFileTimestamps = new();
    private readonly ConcurrentDictionary<string, byte> _knownDocumentPaths = new(StringComparer.OrdinalIgnoreCase);
    private long _accessCounter;
    private int _rootsAttempted; // 0 = not tried, 1 = tried

    private MSBuildWorkspace? _workspace;
    private Solution? _solution;
    private string? _solutionPath;
    private string? _errorMessage;

    public WorkspaceState State { get; private set; } = WorkspaceState.NotStarted;
    public string? ErrorMessage => _errorMessage;
    public int ProjectCount => _solution?.ProjectIds.Count ?? 0;
    public bool IsLazyLoading => ProjectCount > LazyLoadThreshold;

    /// <summary>
    /// Set by Program.cs after host build to allow lazy IMcpServer resolution.
    /// </summary>
    internal IServiceProvider? Services { get; set; }

    public WorkspaceManager(ILogger<WorkspaceManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loads the solution at the specified path. Call this once on startup.
    /// </summary>
    public async Task LoadSolutionAsync(string solutionPath, CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            State = WorkspaceState.Loading;
            _solutionPath = solutionPath;
            _errorMessage = null;

            _logger.LogInformation("Loading solution: {SolutionPath}", solutionPath);

            // Dispose previous workspace to avoid leaking Roslyn Solution snapshots
            _workspace?.Dispose();

            _workspace = MSBuildWorkspace.Create();
            _workspace.RegisterWorkspaceFailedHandler(args =>
            {
                if (args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                    _logger.LogError("Workspace failure: {Message}", args.Diagnostic.Message);
                else
                    _logger.LogWarning("Workspace warning: {Message}", args.Diagnostic.Message);
            });

            _solution = await _workspace.OpenSolutionAsync(solutionPath, cancellationToken: ct);

            _logger.LogInformation("Solution loaded: {ProjectCount} projects", _solution.ProjectIds.Count);

            if (!IsLazyLoading)
            {
                await WarmCompilationsAsync(ct);
            }
            else
            {
                _logger.LogInformation("Large solution detected ({Count} projects). Using lazy loading.",
                    _solution.ProjectIds.Count);
            }

            SnapshotFileTimestamps();
            State = WorkspaceState.Ready;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load solution: {SolutionPath}", solutionPath);
            _errorMessage = ex.Message;
            State = WorkspaceState.Error;
            throw;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Gets the current solution snapshot. Returns null if workspace is not ready.
    /// </summary>
    public Solution? GetSolution() => _solution;

    /// <summary>
    /// Gets or creates a Compilation for the specified project. Thread-safe and cached.
    /// </summary>
    public async Task<Compilation?> GetCompilationAsync(ProjectId projectId, CancellationToken ct = default)
    {
        if (_compilationCache.TryGetValue(projectId, out var cached))
        {
            _cacheAccessOrder[projectId] = Interlocked.Increment(ref _accessCounter);
            return cached;
        }

        var project = _solution?.GetProject(projectId);
        if (project is null)
            return null;

        var compilation = await project.GetCompilationAsync(ct);
        if (compilation is not null)
        {
            EvictIfNeeded();
            _compilationCache[projectId] = compilation;
            _cacheAccessOrder[projectId] = Interlocked.Increment(ref _accessCounter);
        }

        return compilation;
    }

    private void EvictIfNeeded()
    {
        if (!IsLazyLoading || _compilationCache.Count < MaxCachedCompilations)
            return;

        // Evict least-recently-used entries until under limit
        var toEvict = _cacheAccessOrder
            .OrderBy(kvp => kvp.Value)
            .Take(_compilationCache.Count - MaxCachedCompilations + 1)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var projectId in toEvict)
        {
            _compilationCache.TryRemove(projectId, out _);
            _cacheAccessOrder.TryRemove(projectId, out _);
            _logger.LogDebug("Evicted compilation cache for project {ProjectId}", projectId);
        }
    }

    /// <summary>
    /// Gets compilations for all projects. Lazy-loaded on demand.
    /// </summary>
    public async Task<IReadOnlyList<Compilation>> GetAllCompilationsAsync(CancellationToken ct = default)
    {
        if (_solution is null)
            return [];

        var compilations = new List<Compilation>();
        foreach (var projectId in _solution.ProjectIds)
        {
            var compilation = await GetCompilationAsync(projectId, ct);
            if (compilation is not null)
                compilations.Add(compilation);
        }

        return compilations;
    }

    /// <summary>
    /// Returns a status message suitable for MCP tool responses when the workspace is not ready.
    /// </summary>
    public string GetStatusMessage() => State switch
    {
        WorkspaceState.NotStarted => "Workspace has not been initialized. Waiting for solution path.",
        WorkspaceState.Loading => "Workspace is loading the solution. Please try again in a moment.",
        WorkspaceState.Error => $"Workspace failed to load: {_errorMessage}",
        WorkspaceState.Ready => "Workspace is ready.",
        _ => "Unknown workspace state."
    };

    /// <summary>
    /// Returns null when the workspace is ready; otherwise attempts one-shot discovery
    /// from MCP roots and returns a JSON status response if still not ready.
    /// When the workspace is ready, refreshes any documents that have changed on disk.
    /// </summary>
    public async Task<string?> EnsureReadyOrStatusAsync(CancellationToken ct)
    {
        if (State == WorkspaceState.Ready)
        {
            // Refresh any source files that changed since the last tool call
            await RefreshChangedDocumentsAsync(ct);
            return null;
        }

        // One-shot attempt to discover workspace from MCP roots
        if (Interlocked.CompareExchange(ref _rootsAttempted, 1, 0) == 0)
        {
            await TryInitializeFromRootsAsync(ct);
            if (State == WorkspaceState.Ready) return null;
        }

        return JsonSerializer.Serialize(new StatusResponse(State.ToString(), GetStatusMessage()));
    }

    private async Task TryInitializeFromRootsAsync(CancellationToken ct)
    {
        try
        {
            var server = Services?.GetService(typeof(IMcpServer)) as IMcpServer;
            if (server is null) return;

            var rootsResult = await server.RequestRootsAsync(new ListRootsRequestParams(), ct);
            foreach (var root in rootsResult.Roots)
            {
                if (!Uri.TryCreate(root.Uri, UriKind.Absolute, out var uri)) continue;
                var localPath = uri.LocalPath;
                if (!Directory.Exists(localPath)) continue;

                var solutionPath = SolutionDiscovery.FindSolutionPath([], localPath);
                if (solutionPath is not null)
                {
                    _logger.LogInformation("Discovered solution from MCP roots: {SolutionPath}", solutionPath);
                    await LoadSolutionAsync(solutionPath, ct);
                    return;
                }
            }

            _logger.LogWarning("MCP roots available but no .sln/.slnx found in any root directory.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to discover workspace from MCP roots.");
        }
    }

    private async Task WarmCompilationsAsync(CancellationToken ct)
    {
        if (_solution is null) return;

        _logger.LogInformation("Warming compilations for {Count} projects...", _solution.ProjectIds.Count);

        foreach (var projectId in _solution.ProjectIds)
        {
            ct.ThrowIfCancellationRequested();
            await GetCompilationAsync(projectId, ct);
        }

        _logger.LogInformation("All compilations warmed.");
    }

    /// <summary>
    /// Records the last-write time of every document and project file in the solution.
    /// Called once after solution load to establish a baseline for staleness detection.
    /// </summary>
    private void SnapshotFileTimestamps()
    {
        if (_solution is null) return;

        _knownFileTimestamps.Clear();
        _projectFileTimestamps.Clear();
        _knownDocumentPaths.Clear();

        foreach (var projectId in _solution.ProjectIds)
        {
            var project = _solution.GetProject(projectId);
            if (project is null) continue;

            if (project.FilePath is not null && File.Exists(project.FilePath))
            {
                _projectFileTimestamps[project.FilePath] = File.GetLastWriteTimeUtc(project.FilePath);
            }

            foreach (var document in project.Documents)
            {
                if (document.FilePath is not null && File.Exists(document.FilePath))
                {
                    _knownFileTimestamps[document.Id] = File.GetLastWriteTimeUtc(document.FilePath);
                    _knownDocumentPaths[document.FilePath] = 0;
                }
            }
        }

        _logger.LogInformation("Captured timestamps for {Count} documents across {ProjectCount} projects",
            _knownFileTimestamps.Count, _projectFileTimestamps.Count);
    }

    /// <summary>
    /// Refreshes the workspace to reflect on-disk changes. Checks for structural changes
    /// (new files, changed .csproj) that require a full reload, then incrementally updates
    /// any modified existing documents.
    /// </summary>
    public async Task<bool> RefreshChangedDocumentsAsync(CancellationToken ct = default)
    {
        if (_solution is null || _solutionPath is null) return false;

        // Phase 1: detect structural changes that require a full MSBuild reload.
        // This runs outside the write lock because LoadSolutionAsync acquires it.
        if (NeedsStructuralReload())
        {
            _compilationCache.Clear();
            _cacheAccessOrder.Clear();
            await LoadSolutionAsync(_solutionPath, ct);
            return true;
        }

        // Phase 2: incremental text updates for modified existing documents.
        await _writeLock.WaitAsync(ct);
        try
        {
            var refreshed = false;

            foreach (var projectId in _solution.ProjectIds)
            {
                var project = _solution.GetProject(projectId);
                if (project is null) continue;

                foreach (var document in project.Documents)
                {
                    if (document.FilePath is null || !File.Exists(document.FilePath))
                        continue;

                    var currentWriteTime = File.GetLastWriteTimeUtc(document.FilePath);

                    if (_knownFileTimestamps.TryGetValue(document.Id, out var lastKnown)
                        && currentWriteTime <= lastKnown)
                        continue;

                    var text = await File.ReadAllTextAsync(document.FilePath, ct);
                    var sourceText = Microsoft.CodeAnalysis.Text.SourceText.From(text);
                    _solution = _solution.WithDocumentText(document.Id, sourceText);
                    _knownFileTimestamps[document.Id] = currentWriteTime;

                    _compilationCache.TryRemove(project.Id, out _);
                    _cacheAccessOrder.TryRemove(project.Id, out _);

                    refreshed = true;
                    _logger.LogDebug("Refreshed changed document: {Path}", document.FilePath);
                }
            }

            return refreshed;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Checks whether the project structure has changed on disk: a .csproj was modified,
    /// or new source files were added to a project directory. Either case requires a full
    /// MSBuild reload because the Roslyn Solution snapshot doesn't know about them.
    /// </summary>
    private bool NeedsStructuralReload()
    {
        // Check if any project file (.csproj) was modified
        foreach (var (path, lastKnown) in _projectFileTimestamps)
        {
            if (File.Exists(path) && File.GetLastWriteTimeUtc(path) > lastKnown)
            {
                _logger.LogInformation("Project file changed: {Path}. Full reload needed.", path);
                return true;
            }
        }

        // Check for new .cs files in project directories (SDK-style projects auto-include via globs)
        if (_solution is null) return false;

        foreach (var projectId in _solution.ProjectIds)
        {
            var project = _solution.GetProject(projectId);
            if (project?.FilePath is null) continue;

            var projectDir = Path.GetDirectoryName(project.FilePath);
            if (projectDir is null || !Directory.Exists(projectDir)) continue;

            foreach (var file in Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories))
            {
                if (IsInBinObjDirectory(file, projectDir))
                    continue;

                if (!_knownDocumentPaths.ContainsKey(file))
                {
                    _logger.LogInformation("New source file detected: {Path}. Full reload needed.", file);
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsInBinObjDirectory(string filePath, string projectDir)
    {
        var relative = Path.GetRelativePath(projectDir, filePath);
        var sep = Path.DirectorySeparatorChar;
        return relative.StartsWith($"bin{sep}", StringComparison.OrdinalIgnoreCase)
            || relative.StartsWith($"obj{sep}", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _workspace?.Dispose();
        _writeLock.Dispose();
    }
}
