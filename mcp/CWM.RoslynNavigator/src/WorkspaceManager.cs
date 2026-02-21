using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

namespace CWM.RoslynNavigator;

/// <summary>
/// Manages the MSBuildWorkspace lifecycle: loading, file watching, incremental updates, and compilation caching.
/// </summary>
public sealed class WorkspaceManager : IDisposable
{
    private const int LazyLoadThreshold = 50;
    private const int MaxCachedCompilations = 30;

    private readonly ILogger<WorkspaceManager> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly ConcurrentDictionary<ProjectId, Compilation> _compilationCache = new();
    private readonly ConcurrentDictionary<ProjectId, long> _cacheAccessOrder = new();
    private long _accessCounter;
    private readonly List<FileSystemWatcher> _watchers = [];

    private MSBuildWorkspace? _workspace;
    private Solution? _solution;
    private string? _solutionPath;
    private string? _errorMessage;

    public WorkspaceState State { get; private set; } = WorkspaceState.NotStarted;
    public string? ErrorMessage => _errorMessage;
    public int ProjectCount => _solution?.ProjectIds.Count ?? 0;
    public bool IsLazyLoading => ProjectCount > LazyLoadThreshold;

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

            _workspace = MSBuildWorkspace.Create();
            _workspace.WorkspaceFailed += (_, args) =>
            {
                if (args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                    _logger.LogError("Workspace failure: {Message}", args.Diagnostic.Message);
                else
                    _logger.LogWarning("Workspace warning: {Message}", args.Diagnostic.Message);
            };

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

            SetupFileWatchers();
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

    private void SetupFileWatchers()
    {
        if (_solutionPath is null) return;

        var solutionDir = Path.GetDirectoryName(_solutionPath);
        if (solutionDir is null) return;

        // Watch .cs files for incremental text updates
        var csWatcher = new FileSystemWatcher(solutionDir, "*.cs")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };
        csWatcher.Changed += OnSourceFileChanged;
        csWatcher.Created += OnSourceFileChanged;
        csWatcher.Deleted += OnSourceFileChanged;
        csWatcher.EnableRaisingEvents = true;
        _watchers.Add(csWatcher);

        // Watch .csproj files for full reload
        var projWatcher = new FileSystemWatcher(solutionDir, "*.csproj")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };
        projWatcher.Changed += OnProjectFileChanged;
        projWatcher.EnableRaisingEvents = true;
        _watchers.Add(projWatcher);

        _logger.LogInformation("File watchers configured for {Directory}", solutionDir);
    }

    private void OnSourceFileChanged(object sender, FileSystemEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _writeLock.WaitAsync();
                try
                {
                    if (_solution is null) return;

                    var documentIds = _solution.GetDocumentIdsWithFilePath(e.FullPath);
                    foreach (var docId in documentIds)
                    {
                        var document = _solution.GetDocument(docId);
                        if (document is null) continue;

                        // Read the updated text
                        if (!File.Exists(e.FullPath)) continue;
                        var text = await File.ReadAllTextAsync(e.FullPath);
                        var sourceText = Microsoft.CodeAnalysis.Text.SourceText.From(text);

                        _solution = _solution.WithDocumentText(docId, sourceText);

                        // Invalidate compilation cache for the affected project
                        _compilationCache.TryRemove(document.Project.Id, out _);
                        _cacheAccessOrder.TryRemove(document.Project.Id, out _);
                    }
                }
                finally
                {
                    _writeLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process source file change: {Path}", e.FullPath);
            }
        });
    }

    private void OnProjectFileChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("Project file changed: {Path}. Full reload required.", e.FullPath);

        // For .csproj changes, we need a full reload
        _ = Task.Run(async () =>
        {
            try
            {
                if (_solutionPath is not null)
                {
                    _compilationCache.Clear();
                    _cacheAccessOrder.Clear();
                    await LoadSolutionAsync(_solutionPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reload solution after project file change");
            }
        });
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _watchers.Clear();
        _workspace?.Dispose();
        _writeLock.Dispose();
    }
}
