using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.FileSystemGlobbing;

namespace BackupWithGit;

/// <summary>
/// Provides Git operations by invoking the Git CLI.
/// </summary>
public class GitService(string workingDirectory) : IGitService
{
    private readonly string workingDirectory = workingDirectory;

    /// <summary>
    /// Checks if Git is installed on the system.
    /// </summary>
    public bool IsGitInstalled()
    {
        try
        {
            var result = RunGitCommand("--version");
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the working directory is inside a Git repository.
    /// </summary>
    public bool IsGitRepository()
    {
        try
        {
            var result = RunGitCommand("rev-parse --is-inside-work-tree");
            return result.ExitCode == 0 
                && result.Output.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the root directory of the Git repository.
    /// </summary>
    public string? GetRepositoryRoot()
    {
        var result = RunGitCommand("rev-parse --show-toplevel");
        return result.ExitCode == 0 ? result.Output.Trim() : null;
    }

    /// <summary>
    /// Stages all changes and commits with the specified message.
    /// </summary>
    public GitResult Commit(string message)
    {
        // Stage all changes
        var stageResult = RunGitCommand("add -A");
        if (stageResult.ExitCode != 0)
        {
            return stageResult;
        }

        // Check if there are changes to commit
        var statusResult = RunGitCommand("status --porcelain");
        if (statusResult.ExitCode != 0)
        {
            return statusResult;
        }

        if (string.IsNullOrWhiteSpace(statusResult.Output))
        {
            return new GitResult(0, "Nothing to commit, working tree clean.");
        }

        // Commit changes
        return RunGitCommand($"commit -m \"{message}\"");
    }

    /// <summary>
    /// Searches for files matching the glob pattern and returns their commit history.
    /// </summary>
    public IEnumerable<FileCommitInfo> SearchFiles(string globPattern)
    {
        // Get all tracked files
        var filesResult = RunGitCommand($"ls-files -- {globPattern}");
        if (filesResult.ExitCode != 0)
        {
            yield break;
        }

        var matchedFiles = filesResult.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var filePath in matchedFiles)
        {
            // Get every commit that touched this file, most recent first
            var logResult = RunGitCommand($"log --format=\"%H|%s|%ai\" -- \"{filePath}\"");
            if (logResult.ExitCode == 0 && !string.IsNullOrWhiteSpace(logResult.Output))
            {
                var lines = logResult.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var parts = line.Trim().Split('|', 3);
                    if (parts.Length >= 3)
                    {
                        yield return new FileCommitInfo
                        {
                            FilePath = filePath,
                            CommitHash = parts[0],
                            CommitMessage = parts[1],
                            CommitDate = parts[2],
                        };
                    }
                }
            }
            else
            {
                // File exists but has no commits yet (newly added)
                yield return new FileCommitInfo
                {
                    FilePath = filePath,
                    CommitHash = "(uncommitted)",
                    CommitMessage = "",
                    CommitDate = ""
                };
            }
        }
    }

    /// <summary>
    /// Gets all files matching the glob pattern from the repository.
    /// </summary>
    public IEnumerable<string> GetMatchingFiles(string globPattern)
    {
        var filesResult = RunGitCommand($"ls-files -- {globPattern}");
        if (filesResult.ExitCode != 0)
        {
            yield break;
        }

        var matchedFiles = filesResult.Output.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        var repoRoot = GetRepositoryRoot() ?? workingDirectory;
        
        foreach (var file in matchedFiles)
        {
            yield return Path.Combine(repoRoot, file);
        }
    }

    /// <summary>
    /// Gets all files matching the glob pattern at a specific commit.
    /// Returns relative paths within the repository.
    /// </summary>
    public IEnumerable<string> GetMatchingFilesAtCommit(string globPattern, string commitHash)
    {
        var result = RunGitCommand($"ls-tree -r --name-only {commitHash}");
        if (result.ExitCode != 0)
        {
            yield break;
        }

        // Git pathspecs without a '/' match against the basename (recursively).
        // FileSystemGlobbing requires '**/' prefix for recursive matching.
        var matchPattern = globPattern.Contains('/') ? globPattern : "**/" + globPattern;

        var matcher = new Matcher();
        matcher.AddInclude(matchPattern);

        var allFiles = result.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        foreach (var file in allFiles)
        {
            var trimmed = file.Trim();
            if (matcher.Match(trimmed).HasMatches)
            {
                yield return trimmed;
            }
        }
    }

    /// <summary>
    /// Gets the contents of a file at a specific commit.
    /// </summary>
    public GitResult GetFileContentsAtCommit(string relativePath, string commitHash)
    {
        return RunGitCommand($"show {commitHash}:\"{relativePath}\"");
    }

    /// <summary>
    /// Syncs the current repository to a destination repository using push.
    /// </summary>
    public GitResult SyncToRepository(string destinationPath)
    {
        var destFullPath = Path.GetFullPath(destinationPath);

        // Verify destination is a git repository
        var destGitDir = Path.Combine(destFullPath, ".git");
        var destHeadFile = Path.Combine(destFullPath, "HEAD");
        if (!Directory.Exists(destFullPath))
        {
            return new GitResult(1, $"Destination directory does not exist: {destFullPath}");
        }

        // Check for regular repo (.git dir/file) or bare repo (HEAD file in root)
        if (!Directory.Exists(destGitDir) && !File.Exists(destGitDir) && !File.Exists(destHeadFile))
        {
            return new GitResult(1, $"Destination is not a Git repository: {destFullPath}");
        }

        // Check if 'backup' remote already exists
        var remoteResult = RunGitCommand("remote");
        var remotes = remoteResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var hasBackupRemote = remotes.Any(r => r.Trim() == "backup");

        if (hasBackupRemote)
        {
            // Update existing remote
            var setUrlResult = RunGitCommand($"remote set-url backup \"{destFullPath}\"");
            if (setUrlResult.ExitCode != 0)
            {
                return setUrlResult;
            }
        }
        else
        {
            // Add new remote
            var addResult = RunGitCommand($"remote add backup \"{destFullPath}\"");
            if (addResult.ExitCode != 0)
            {
                return addResult;
            }
        }

        // Get current branch name
        var branchResult = RunGitCommand("rev-parse --abbrev-ref HEAD");
        if (branchResult.ExitCode != 0)
        {
            return branchResult;
        }
        var currentBranch = branchResult.Output.Trim();

        // Push to backup remote
        var pushResult = RunGitCommand($"push backup {currentBranch} --force");
        return pushResult;
    }

    /// <summary>
    /// Matches file paths against a glob pattern.
    /// </summary>
    // private static IEnumerable<string> MatchFilesAgainstPattern(IEnumerable<string> files, string globPattern)
    // {
    //     var matcher = new Matcher();
    //     matcher.AddInclude(globPattern);

    //     foreach (var file in files)
    //     {
    //         var result = matcher.Match(file);
    //         if (result.HasMatches)
    //         {
    //             yield return file;
    //         }
    //     }
    // }

    private GitResult RunGitCommand(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = startInfo };
        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
                output.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                error.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        var result = output.ToString();
        if (process.ExitCode != 0 && error.Length > 0)
        {
            result = error.ToString();
        }

        return new GitResult(process.ExitCode, result);
    }
}

/// <summary>
/// Represents the result of a Git command execution.
/// </summary>
public record GitResult(int ExitCode, string Output)
{
    public bool Success => ExitCode == 0;
}

/// <summary>
/// Represents file information along with its last commit details.
/// </summary>
public class FileCommitInfo
{
    public required string FilePath { get; init; }
    public required string CommitHash { get; init; }
    public required string CommitMessage { get; init; }
    public required string CommitDate { get; init; }
}
