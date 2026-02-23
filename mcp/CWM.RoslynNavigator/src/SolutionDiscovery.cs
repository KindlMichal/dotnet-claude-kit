namespace CWM.RoslynNavigator;

/// <summary>
/// Discovers .sln/.slnx files from command-line arguments or the working directory.
/// </summary>
public static class SolutionDiscovery
{
    private static readonly string[] SolutionExtensions = [".sln", ".slnx"];

    /// <summary>
    /// Resolves the solution file path from the provided arguments or by scanning the working directory.
    /// </summary>
    /// <param name="args">Command-line arguments. Supports --solution path.</param>
    /// <param name="workingDirectory">The directory to scan if no explicit path is provided.</param>
    /// <returns>The full path to the solution file, or null if not found.</returns>
    public static string? FindSolutionPath(string[] args, string? workingDirectory = null)
    {
        // Check for explicit --solution argument
        var explicitPath = GetExplicitSolutionPath(args);
        if (explicitPath is not null)
        {
            // If the path points to a solution file, use it directly
            if (File.Exists(explicitPath))
                return Path.GetFullPath(explicitPath);

            // If it's a directory, scan it for solution files
            if (Directory.Exists(explicitPath))
                return ScanDirectory(Path.GetFullPath(explicitPath));

            return null;
        }

        // Scan the working directory
        var directory = workingDirectory ?? Directory.GetCurrentDirectory();
        return ScanDirectory(directory);
    }

    private static string? GetExplicitSolutionPath(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] is "--solution" or "-s")
            {
                return args[i + 1];
            }
        }
        return null;
    }

    private static string? ScanDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            return null;

        var solutionFiles = SolutionExtensions
            .SelectMany(ext => Directory.GetFiles(directory, $"*{ext}"))
            .OrderBy(f => f) // Deterministic ordering
            .ToList();

        return solutionFiles.Count switch
        {
            0 => null,
            1 => solutionFiles[0],
            // If multiple solutions exist, prefer one in the root directory
            _ => solutionFiles.FirstOrDefault(f =>
                     Path.GetDirectoryName(f) == directory) ?? solutionFiles[0]
        };
    }
}
