namespace BackupWithGit;

/// <summary>
/// Defines the contract for Git operations.
/// </summary>
public interface IGitService
{
    /// <summary>
    /// Checks if Git is installed on the system.
    /// </summary>
    bool IsGitInstalled();

    /// <summary>
    /// Checks if the working directory is inside a Git repository.
    /// </summary>
    bool IsGitRepository();

    /// <summary>
    /// Gets the root directory of the Git repository.
    /// </summary>
    string? GetRepositoryRoot();

    /// <summary>
    /// Stages all changes and commits with the specified message.
    /// </summary>
    GitResult Commit(string message);

    /// <summary>
    /// Searches for files matching the glob pattern and returns their full commit history.
    /// Each matching file will have one entry per commit that touched it, ordered most recent first.
    /// </summary>
    IEnumerable<FileCommitInfo> SearchFiles(string globPattern);

    /// <summary>
    /// Gets all files matching the glob pattern from the repository.
    /// </summary>
    IEnumerable<string> GetMatchingFiles(string globPattern);

    /// <summary>
    /// Gets all files matching the glob pattern at a specific commit.
    /// Returns relative paths within the repository.
    /// </summary>
    IEnumerable<string> GetMatchingFilesAtCommit(string globPattern, string commitHash);

    /// <summary>
    /// Gets the contents of a file at a specific commit.
    /// </summary>
    GitResult GetFileContentsAtCommit(string relativePath, string commitHash);

    /// <summary>
    /// Syncs the current repository to a destination repository using push.
    /// </summary>
    GitResult SyncToRepository(string destinationPath);
}
