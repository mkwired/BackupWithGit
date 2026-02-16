namespace BackupWithGit.Commands;

/// <summary>
/// Handles the --restore|-r command: copies files matching a glob pattern to a destination folder.
/// </summary>
public class RestoreCommand
{
    private readonly IGitService _gitService;

    public RestoreCommand(IGitService gitService)
    {
        _gitService = gitService;
    }

    public int Execute(string pattern, string destination, string? commitHash = null)
    {
        var destPath = Path.GetFullPath(destination);
        
        if (commitHash != null)
        {
            Console.WriteLine($"Restoring files matching '{pattern}' at commit {commitHash} to: {destPath}");
        }
        else
        {
            Console.WriteLine($"Restoring files matching '{pattern}' to: {destPath}");
        }
        Console.WriteLine();

        // Create destination directory if it doesn't exist
        if (!Directory.Exists(destPath))
        {
            try
            {
                Directory.CreateDirectory(destPath);
                Console.WriteLine($"Created destination directory: {destPath}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to create destination directory: {ex.Message}");
                return 3;
            }
        }

        if (commitHash != null)
        {
            return RestoreFromCommit(pattern, destPath, commitHash);
        }

        return RestoreFromWorkingTree(pattern, destPath);
    }

    private int RestoreFromCommit(string pattern, string destPath, string commitHash)
    {
        var matchingFiles = _gitService.GetMatchingFilesAtCommit(pattern, commitHash).ToList();

        if (matchingFiles.Count == 0)
        {
            Console.WriteLine("No files found matching the pattern.");
            return 0;
        }

        int successCount = 0;
        int errorCount = 0;

        foreach (var relativePath in matchingFiles)
        {
            try
            {
                var destFile = Path.Combine(destPath, relativePath);
                var destDir = Path.GetDirectoryName(destFile);

                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                var contentResult = _gitService.GetFileContentsAtCommit(relativePath, commitHash);
                if (!contentResult.Success)
                {
                    Console.Error.WriteLine($"  Failed to read {relativePath} at commit {commitHash}: {contentResult.Output}");
                    errorCount++;
                    continue;
                }

                File.WriteAllText(destFile, contentResult.Output);
                Console.WriteLine($"  Restored: {relativePath}");
                successCount++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  Failed to restore {relativePath}: {ex.Message}");
                errorCount++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Restore complete: {successCount} file(s) restored, {errorCount} error(s).");

        return errorCount > 0 ? 3 : 0;
    }

    private int RestoreFromWorkingTree(string pattern, string destPath)
    {
        var matchingFiles = _gitService.GetMatchingFiles(pattern).ToList();

        if (matchingFiles.Count == 0)
        {
            Console.WriteLine("No files found matching the pattern.");
            return 0;
        }

        var repoRoot = _gitService.GetRepositoryRoot();
        if (repoRoot == null)
        {
            Console.Error.WriteLine("Could not determine repository root.");
            return 3;
        }

        int successCount = 0;
        int errorCount = 0;

        foreach (var sourceFile in matchingFiles)
        {
            try
            {
                // Preserve relative path structure
                var relativePath = Path.GetRelativePath(repoRoot, sourceFile);
                var destFile = Path.Combine(destPath, relativePath);
                var destDir = Path.GetDirectoryName(destFile);

                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                File.Copy(sourceFile, destFile, overwrite: true);
                Console.WriteLine($"  Copied: {relativePath}");
                successCount++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  Failed to copy {sourceFile}: {ex.Message}");
                errorCount++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Restore complete: {successCount} file(s) copied, {errorCount} error(s).");

        return errorCount > 0 ? 3 : 0;
    }
}
