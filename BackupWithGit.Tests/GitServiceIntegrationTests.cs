using System.Diagnostics;
using System.Text;

namespace BackupWithGit.Tests;

/// <summary>
/// Integration tests for GitService using real temporary Git repositories.
/// These tests require Git to be installed on the system.
/// </summary>
public class GitServiceIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly GitService _gitService;

    public GitServiceIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "BackupWithGitTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        RunGit(_tempDir, "init");
        RunGit(_tempDir, "config user.email \"test@test.com\"");
        RunGit(_tempDir, "config user.name \"Test\"");
        _gitService = new GitService(_tempDir);
    }

    public void Dispose()
    {
        // Force-remove readonly attributes set by git
        ForceDeleteDirectory(_tempDir);
    }

    #region IsGitInstalled

    [Fact]
    public void IsGitInstalled_ReturnsTrue()
    {
        Assert.True(_gitService.IsGitInstalled());
    }

    #endregion

    #region IsGitRepository

    [Fact]
    public void IsGitRepository_InGitRepo_ReturnsTrue()
    {
        Assert.True(_gitService.IsGitRepository());
    }

    [Fact]
    public void IsGitRepository_OutsideGitRepo_ReturnsFalse()
    {
        var nonRepoDir = Path.Combine(Path.GetTempPath(), "BackupWithGitTests_NoRepo_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(nonRepoDir);
        try
        {
            var service = new GitService(nonRepoDir);
            Assert.False(service.IsGitRepository());
        }
        finally
        {
            Directory.Delete(nonRepoDir, recursive: true);
        }
    }

    #endregion

    #region GetRepositoryRoot

    [Fact]
    public void GetRepositoryRoot_ReturnsRootDirectory()
    {
        var root = _gitService.GetRepositoryRoot();
        Assert.NotNull(root);
        // Normalize paths for comparison
        Assert.Equal(
            Path.GetFullPath(_tempDir).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar));
    }

    [Fact]
    public void GetRepositoryRoot_FromSubdirectory_ReturnsRoot()
    {
        var subDir = Path.Combine(_tempDir, "sub", "folder");
        Directory.CreateDirectory(subDir);
        var service = new GitService(subDir);

        var root = service.GetRepositoryRoot();
        Assert.NotNull(root);
        Assert.Equal(
            Path.GetFullPath(_tempDir).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar));
    }

    [Fact]
    public void GetRepositoryRoot_OutsideGitRepo_ReturnsNull()
    {
        var nonRepoDir = Path.Combine(Path.GetTempPath(), "BackupWithGitTests_NoRepo2_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(nonRepoDir);
        try
        {
            var service = new GitService(nonRepoDir);
            Assert.Null(service.GetRepositoryRoot());
        }
        finally
        {
            Directory.Delete(nonRepoDir, recursive: true);
        }
    }

    #endregion

    #region Commit

    [Fact]
    public void Commit_WithNoChanges_ReturnsSuccessWithCleanMessage()
    {
        // Need at least one commit first (empty repos have no HEAD)
        File.WriteAllText(Path.Combine(_tempDir, "initial.txt"), "init");
        _gitService.Commit("initial");

        var result = _gitService.Commit("no changes");

        Assert.True(result.Success);
        Assert.Contains("Nothing to commit", result.Output);
    }

    [Fact]
    public void Commit_WithNewFile_CommitsSuccessfully()
    {
        File.WriteAllText(Path.Combine(_tempDir, "newfile.txt"), "content");

        var result = _gitService.Commit("add new file");

        Assert.True(result.Success);
    }

    [Fact]
    public void Commit_WithModifiedFile_CommitsSuccessfully()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file.txt"), "original");
        _gitService.Commit("first commit");

        File.WriteAllText(Path.Combine(_tempDir, "file.txt"), "modified");
        var result = _gitService.Commit("modified file");

        Assert.True(result.Success);
    }

    [Fact]
    public void Commit_WithDeletedFile_CommitsSuccessfully()
    {
        File.WriteAllText(Path.Combine(_tempDir, "todelete.txt"), "will be deleted");
        _gitService.Commit("add file");

        File.Delete(Path.Combine(_tempDir, "todelete.txt"));
        var result = _gitService.Commit("deleted file");

        Assert.True(result.Success);
    }

    [Fact]
    public void Commit_StagesUntrackedFiles()
    {
        File.WriteAllText(Path.Combine(_tempDir, "untracked.txt"), "new");

        var result = _gitService.Commit("stage untracked");

        Assert.True(result.Success);

        // Verify the file is tracked
        var logOutput = RunGit(_tempDir, "log --oneline");
        Assert.Contains("stage untracked", logOutput);
    }

    [Fact]
    public void Commit_UsesProvidedMessage()
    {
        File.WriteAllText(Path.Combine(_tempDir, "msg.txt"), "data");

        _gitService.Commit("my custom message");

        var logOutput = RunGit(_tempDir, "log -1 --format=%s");
        Assert.Contains("my custom message", logOutput.Trim());
    }

    #endregion

    #region SearchFiles

    [Fact]
    public void SearchFiles_ReturnsMatchingCommittedFiles()
    {
        File.WriteAllText(Path.Combine(_tempDir, "search1.txt"), "a");
        File.WriteAllText(Path.Combine(_tempDir, "search2.txt"), "b");
        File.WriteAllText(Path.Combine(_tempDir, "search3.cs"), "c");
        _gitService.Commit("add files");

        var results = _gitService.SearchFiles("*.txt").ToList();

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.EndsWith(".txt", r.FilePath));
    }

    [Fact]
    public void SearchFiles_ReturnsCommitInfo()
    {
        File.WriteAllText(Path.Combine(_tempDir, "infofile.txt"), "data");
        _gitService.Commit("commit for info test");

        var results = _gitService.SearchFiles("infofile.txt").ToList();

        Assert.Single(results);
        var file = results[0];
        Assert.Equal("infofile.txt", file.FilePath);
        Assert.NotEmpty(file.CommitHash);
        Assert.Equal("commit for info test", file.CommitMessage);
        Assert.NotEmpty(file.CommitDate);
    }

    [Fact]
    public void SearchFiles_ReturnsFullHistory()
    {
        File.WriteAllText(Path.Combine(_tempDir, "history.txt"), "v1");
        _gitService.Commit("first version");

        File.WriteAllText(Path.Combine(_tempDir, "history.txt"), "v2");
        _gitService.Commit("second version");

        File.WriteAllText(Path.Combine(_tempDir, "history.txt"), "v3");
        _gitService.Commit("third version");

        var results = _gitService.SearchFiles("history.txt").ToList();

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.Equal("history.txt", r.FilePath));
        // Most recent commit first
        Assert.Equal("third version", results[0].CommitMessage);
        Assert.Equal("second version", results[1].CommitMessage);
        Assert.Equal("first version", results[2].CommitMessage);
    }

    [Fact]
    public void SearchFiles_MultipleFiles_ReturnsFullHistoryForEach()
    {
        File.WriteAllText(Path.Combine(_tempDir, "a.txt"), "a1");
        File.WriteAllText(Path.Combine(_tempDir, "b.txt"), "b1");
        _gitService.Commit("add both files");

        File.WriteAllText(Path.Combine(_tempDir, "a.txt"), "a2");
        _gitService.Commit("update a only");

        var results = _gitService.SearchFiles("*.txt").ToList();

        var aResults = results.Where(r => r.FilePath == "a.txt").ToList();
        var bResults = results.Where(r => r.FilePath == "b.txt").ToList();

        Assert.Equal(2, aResults.Count);
        Assert.Single(bResults);
        Assert.Equal("update a only", aResults[0].CommitMessage);
        Assert.Equal("add both files", aResults[1].CommitMessage);
        Assert.Equal("add both files", bResults[0].CommitMessage);
    }

    [Fact]
    public void SearchFiles_NoMatches_ReturnsEmpty()
    {
        File.WriteAllText(Path.Combine(_tempDir, "only.txt"), "data");
        _gitService.Commit("add");

        var results = _gitService.SearchFiles("*.xyz").ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void SearchFiles_InSubdirectory_ReturnsRelativePath()
    {
        var subDir = Path.Combine(_tempDir, "sub");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "deep.txt"), "data");
        _gitService.Commit("add nested file");

        var results = _gitService.SearchFiles("**/*.txt").ToList();

        Assert.Contains(results, r => r.FilePath.Contains("sub/deep.txt"));
    }

    #endregion

    #region GetMatchingFiles

    [Fact]
    public void GetMatchingFiles_ReturnsFullPaths()
    {
        File.WriteAllText(Path.Combine(_tempDir, "match1.txt"), "a");
        File.WriteAllText(Path.Combine(_tempDir, "match2.txt"), "b");
        _gitService.Commit("add files");

        var files = _gitService.GetMatchingFiles("*.txt").ToList();

        Assert.Equal(2, files.Count);
        Assert.All(files, f => Assert.True(Path.IsPathRooted(f)));
    }

    [Fact]
    public void GetMatchingFiles_NoMatches_ReturnsEmpty()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file.txt"), "data");
        _gitService.Commit("add");

        var files = _gitService.GetMatchingFiles("*.xyz").ToList();

        Assert.Empty(files);
    }

    [Fact]
    public void GetMatchingFiles_ReturnsCorrectFileNames()
    {
        File.WriteAllText(Path.Combine(_tempDir, "alpha.cs"), "code");
        File.WriteAllText(Path.Combine(_tempDir, "beta.cs"), "code");
        File.WriteAllText(Path.Combine(_tempDir, "gamma.txt"), "text");
        _gitService.Commit("add");

        var files = _gitService.GetMatchingFiles("*.cs").ToList();

        Assert.Equal(2, files.Count);
        Assert.All(files, f => Assert.EndsWith(".cs", f));
    }

    #endregion

    #region GetMatchingFilesAtCommit

    [Fact]
    public void GetMatchingFilesAtCommit_ReturnsMatchingFiles()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file1.cs"), "code");
        File.WriteAllText(Path.Combine(_tempDir, "file2.txt"), "text");
        _gitService.Commit("add files");

        var commitHash = RunGit(_tempDir, "rev-parse HEAD").Trim();

        var files = _gitService.GetMatchingFilesAtCommit("*.cs", commitHash).ToList();

        Assert.Single(files);
        Assert.Equal("file1.cs", files[0]);
    }

    [Fact]
    public void GetMatchingFilesAtCommit_MatchesFilesInSubdirectories()
    {
        var subDir = Path.Combine(_tempDir, "sub", "folder");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "nested.cs"), "code");
        File.WriteAllText(Path.Combine(_tempDir, "root.cs"), "code");
        _gitService.Commit("add nested files");

        var commitHash = RunGit(_tempDir, "rev-parse HEAD").Trim();

        var files = _gitService.GetMatchingFilesAtCommit("*.cs", commitHash).OrderBy(f => f).ToList();

        Assert.Equal(2, files.Count);
        Assert.Equal("root.cs", files[0]);
        Assert.Equal("sub/folder/nested.cs", files[1]);
    }

    [Fact]
    public void GetMatchingFilesAtCommit_NoMatches_ReturnsEmpty()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file.txt"), "text");
        _gitService.Commit("add file");

        var commitHash = RunGit(_tempDir, "rev-parse HEAD").Trim();

        var files = _gitService.GetMatchingFilesAtCommit("*.xyz", commitHash).ToList();

        Assert.Empty(files);
    }

    [Fact]
    public void GetMatchingFilesAtCommit_WithDoubleStarGlob_MatchesRecursively()
    {
        var subDir = Path.Combine(_tempDir, "src", "models");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "model.cs"), "code");
        File.WriteAllText(Path.Combine(_tempDir, "program.cs"), "code");
        _gitService.Commit("add files");

        var commitHash = RunGit(_tempDir, "rev-parse HEAD").Trim();

        var files = _gitService.GetMatchingFilesAtCommit("**/*.cs", commitHash).OrderBy(f => f).ToList();

        Assert.Equal(2, files.Count);
    }

    #endregion

    #region GetFileContentsAtCommit

    [Fact]
    public void GetFileContentsAtCommit_ReturnsFileContent()
    {
        File.WriteAllText(Path.Combine(_tempDir, "test.txt"), "hello world");
        _gitService.Commit("add test file");

        var commitHash = RunGit(_tempDir, "rev-parse HEAD").Trim();

        var result = _gitService.GetFileContentsAtCommit("test.txt", commitHash);

        Assert.True(result.Success);
        Assert.Contains("hello world", result.Output);
    }

    [Fact]
    public void GetFileContentsAtCommit_ReturnsContentFromSpecificCommit()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file.txt"), "version 1");
        _gitService.Commit("first version");
        var firstCommit = RunGit(_tempDir, "rev-parse HEAD").Trim();

        File.WriteAllText(Path.Combine(_tempDir, "file.txt"), "version 2");
        _gitService.Commit("second version");

        var result = _gitService.GetFileContentsAtCommit("file.txt", firstCommit);

        Assert.True(result.Success);
        Assert.Contains("version 1", result.Output);
        Assert.DoesNotContain("version 2", result.Output);
    }

    [Fact]
    public void GetFileContentsAtCommit_InvalidCommit_Fails()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file.txt"), "content");
        _gitService.Commit("add file");

        var result = _gitService.GetFileContentsAtCommit("file.txt", "deadbeef000000");

        Assert.False(result.Success);
    }

    #endregion

    #region SyncToRepository

    [Fact]
    public void SyncToRepository_ToValidRepo_Succeeds()
    {
        // Create a commit in source
        File.WriteAllText(Path.Combine(_tempDir, "sync.txt"), "data");
        _gitService.Commit("sync commit");

        // Create destination repo (non-bare, allow pushes to checked-out branch)
        var destDir = Path.Combine(Path.GetTempPath(), "BackupWithGitTests_Dest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(destDir);
        RunGit(destDir, "init");
        RunGit(destDir, "config user.email \"test@test.com\"");
        RunGit(destDir, "config user.name \"Test\"");
        // Create an initial commit so the branch exists, then allow receiving pushes
        File.WriteAllText(Path.Combine(destDir, "placeholder.txt"), "init");
        RunGit(destDir, "add -A");
        RunGit(destDir, "commit -m \"init\"");
        RunGit(destDir, "config receive.denyCurrentBranch updateInstead");

        try
        {
            var result = _gitService.SyncToRepository(destDir);
            Assert.True(result.Success);
        }
        finally
        {
            ForceDeleteDirectory(destDir);
        }
    }

    [Fact]
    public void SyncToRepository_ToNonExistentDirectory_Fails()
    {
        File.WriteAllText(Path.Combine(_tempDir, "x.txt"), "data");
        _gitService.Commit("commit");

        var result = _gitService.SyncToRepository(Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid().ToString("N")));

        Assert.False(result.Success);
    }

    [Fact]
    public void SyncToRepository_ToNonGitDirectory_Fails()
    {
        File.WriteAllText(Path.Combine(_tempDir, "x.txt"), "data");
        _gitService.Commit("commit");

        var nonGitDir = Path.Combine(Path.GetTempPath(), "BackupWithGitTests_NoGit_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(nonGitDir);

        try
        {
            var result = _gitService.SyncToRepository(nonGitDir);
            Assert.False(result.Success);
        }
        finally
        {
            Directory.Delete(nonGitDir, recursive: true);
        }
    }

    [Fact]
    public void SyncToRepository_PushesCurrentBranch()
    {
        File.WriteAllText(Path.Combine(_tempDir, "pushed.txt"), "content");
        _gitService.Commit("push test");

        var destDir = Path.Combine(Path.GetTempPath(), "BackupWithGitTests_Push_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(destDir);
        RunGit(destDir, "init --bare");

        try
        {
            _gitService.SyncToRepository(destDir);

            // Verify commit exists in destination
            var logResult = RunGit(destDir, "log --oneline --all");
            Assert.Contains("push test", logResult);
        }
        finally
        {
            ForceDeleteDirectory(destDir);
        }
    }

    [Fact]
    public void SyncToRepository_CalledTwice_UpdatesRemote()
    {
        File.WriteAllText(Path.Combine(_tempDir, "file.txt"), "v1");
        _gitService.Commit("version 1");

        var destDir = Path.Combine(Path.GetTempPath(), "BackupWithGitTests_Twice_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(destDir);
        RunGit(destDir, "init --bare");

        try
        {
            var result1 = _gitService.SyncToRepository(destDir);
            Assert.True(result1.Success);

            // Make another change and sync again
            File.WriteAllText(Path.Combine(_tempDir, "file.txt"), "v2");
            _gitService.Commit("version 2");

            var result2 = _gitService.SyncToRepository(destDir);
            Assert.True(result2.Success);

            var logResult = RunGit(destDir, "log --oneline --all");
            Assert.Contains("version 2", logResult);
        }
        finally
        {
            ForceDeleteDirectory(destDir);
        }
    }

    #endregion

    #region Helpers

    private static string RunGit(string workingDirectory, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }

    private static void ForceDeleteDirectory(string path)
    {
        if (!Directory.Exists(path)) return;

        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }
        Directory.Delete(path, recursive: true);
    }

    #endregion
}
