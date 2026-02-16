using NSubstitute;
using BackupWithGit.Commands;

namespace BackupWithGit.Tests;

public class RestoreCommandTests : IDisposable
{
    private readonly IGitService _gitService;
    private readonly RestoreCommand _command;
    private readonly string _tempDir;

    public RestoreCommandTests()
    {
        _gitService = Substitute.For<IGitService>();
        _command = new RestoreCommand(_gitService);
        _tempDir = Path.Combine(Path.GetTempPath(), "BackupWithGitTests_Restore_" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void Execute_WhenNoFilesMatch_ReturnsZero()
    {
        _gitService.GetMatchingFiles("*.xyz")
            .Returns(Enumerable.Empty<string>());

        var dest = Path.Combine(_tempDir, "output");
        var result = _command.Execute("*.xyz", dest);

        Assert.Equal(0, result);
    }

    [Fact]
    public void Execute_CopiesMatchingFiles_ReturnsZero()
    {
        // Set up a source file in temp
        var repoRoot = Path.Combine(_tempDir, "repo");
        Directory.CreateDirectory(repoRoot);
        var sourceFile = Path.Combine(repoRoot, "test.txt");
        File.WriteAllText(sourceFile, "hello world");

        _gitService.GetMatchingFiles("*.txt")
            .Returns(new[] { sourceFile });
        _gitService.GetRepositoryRoot().Returns(repoRoot);

        var dest = Path.Combine(_tempDir, "output");
        var result = _command.Execute("*.txt", dest);

        Assert.Equal(0, result);
        Assert.True(File.Exists(Path.Combine(dest, "test.txt")));
        Assert.Equal("hello world", File.ReadAllText(Path.Combine(dest, "test.txt")));
    }

    [Fact]
    public void Execute_PreservesRelativeDirectoryStructure()
    {
        var repoRoot = Path.Combine(_tempDir, "repo");
        var subDir = Path.Combine(repoRoot, "sub", "folder");
        Directory.CreateDirectory(subDir);
        var sourceFile = Path.Combine(subDir, "nested.txt");
        File.WriteAllText(sourceFile, "nested content");

        _gitService.GetMatchingFiles("**/*.txt")
            .Returns(new[] { sourceFile });
        _gitService.GetRepositoryRoot().Returns(repoRoot);

        var dest = Path.Combine(_tempDir, "output");
        var result = _command.Execute("**/*.txt", dest);

        Assert.Equal(0, result);
        var expectedDest = Path.Combine(dest, "sub", "folder", "nested.txt");
        Assert.True(File.Exists(expectedDest));
        Assert.Equal("nested content", File.ReadAllText(expectedDest));
    }

    [Fact]
    public void Execute_CreatesDestinationDirectory()
    {
        _gitService.GetMatchingFiles("*.txt")
            .Returns(Enumerable.Empty<string>());

        var dest = Path.Combine(_tempDir, "new", "nested", "output");
        Assert.False(Directory.Exists(dest));

        _command.Execute("*.txt", dest);

        Assert.True(Directory.Exists(dest));
    }

    [Fact]
    public void Execute_OverwritesExistingFiles()
    {
        var repoRoot = Path.Combine(_tempDir, "repo");
        Directory.CreateDirectory(repoRoot);
        var sourceFile = Path.Combine(repoRoot, "test.txt");
        File.WriteAllText(sourceFile, "new content");

        var dest = Path.Combine(_tempDir, "output");
        Directory.CreateDirectory(dest);
        File.WriteAllText(Path.Combine(dest, "test.txt"), "old content");

        _gitService.GetMatchingFiles("*.txt")
            .Returns(new[] { sourceFile });
        _gitService.GetRepositoryRoot().Returns(repoRoot);

        var result = _command.Execute("*.txt", dest);

        Assert.Equal(0, result);
        Assert.Equal("new content", File.ReadAllText(Path.Combine(dest, "test.txt")));
    }

    [Fact]
    public void Execute_WhenRepositoryRootIsNull_ReturnsThree()
    {
        var repoRoot = Path.Combine(_tempDir, "repo");
        Directory.CreateDirectory(repoRoot);
        var sourceFile = Path.Combine(repoRoot, "test.txt");
        File.WriteAllText(sourceFile, "content");

        _gitService.GetMatchingFiles("*.txt")
            .Returns(new[] { sourceFile });
        _gitService.GetRepositoryRoot().Returns((string?)null);

        var dest = Path.Combine(_tempDir, "output");
        var result = _command.Execute("*.txt", dest);

        Assert.Equal(3, result);
    }

    [Fact]
    public void Execute_CopiesMultipleFiles()
    {
        var repoRoot = Path.Combine(_tempDir, "repo");
        Directory.CreateDirectory(repoRoot);

        var file1 = Path.Combine(repoRoot, "a.txt");
        var file2 = Path.Combine(repoRoot, "b.txt");
        File.WriteAllText(file1, "aaa");
        File.WriteAllText(file2, "bbb");

        _gitService.GetMatchingFiles("*.txt")
            .Returns(new[] { file1, file2 });
        _gitService.GetRepositoryRoot().Returns(repoRoot);

        var dest = Path.Combine(_tempDir, "output");
        var result = _command.Execute("*.txt", dest);

        Assert.Equal(0, result);
        Assert.True(File.Exists(Path.Combine(dest, "a.txt")));
        Assert.True(File.Exists(Path.Combine(dest, "b.txt")));
    }

    [Fact]
    public void Execute_WithCommitHash_RestoresFilesFromCommit()
    {
        var commitHash = "abc1234";
        _gitService.GetMatchingFilesAtCommit("*.txt", commitHash)
            .Returns(new[] { "test.txt" });
        _gitService.GetFileContentsAtCommit("test.txt", commitHash)
            .Returns(new GitResult(0, "content from commit"));

        var dest = Path.Combine(_tempDir, "output");
        var result = _command.Execute("*.txt", dest, commitHash);

        Assert.Equal(0, result);
        Assert.True(File.Exists(Path.Combine(dest, "test.txt")));
        Assert.Equal("content from commit", File.ReadAllText(Path.Combine(dest, "test.txt")));
    }

    [Fact]
    public void Execute_WithCommitHash_NoMatchingFiles_ReturnsZero()
    {
        var commitHash = "abc1234";
        _gitService.GetMatchingFilesAtCommit("*.xyz", commitHash)
            .Returns(Enumerable.Empty<string>());

        var dest = Path.Combine(_tempDir, "output");
        var result = _command.Execute("*.xyz", dest, commitHash);

        Assert.Equal(0, result);
    }

    [Fact]
    public void Execute_WithCommitHash_PreservesDirectoryStructure()
    {
        var commitHash = "abc1234";
        _gitService.GetMatchingFilesAtCommit("**/*.txt", commitHash)
            .Returns(new[] { "sub/folder/nested.txt" });
        _gitService.GetFileContentsAtCommit("sub/folder/nested.txt", commitHash)
            .Returns(new GitResult(0, "nested content"));

        var dest = Path.Combine(_tempDir, "output");
        var result = _command.Execute("**/*.txt", dest, commitHash);

        Assert.Equal(0, result);
        var expectedDest = Path.Combine(dest, "sub", "folder", "nested.txt");
        Assert.True(File.Exists(expectedDest));
        Assert.Equal("nested content", File.ReadAllText(expectedDest));
    }

    [Fact]
    public void Execute_WithCommitHash_WhenGetContentsFails_ReturnsThree()
    {
        var commitHash = "abc1234";
        _gitService.GetMatchingFilesAtCommit("*.txt", commitHash)
            .Returns(new[] { "test.txt" });
        _gitService.GetFileContentsAtCommit("test.txt", commitHash)
            .Returns(new GitResult(1, "fatal: bad revision"));

        var dest = Path.Combine(_tempDir, "output");
        var result = _command.Execute("*.txt", dest, commitHash);

        Assert.Equal(3, result);
    }

    [Fact]
    public void Execute_WithCommitHash_MultipleFiles()
    {
        var commitHash = "abc1234";
        _gitService.GetMatchingFilesAtCommit("*.txt", commitHash)
            .Returns(new[] { "a.txt", "b.txt" });
        _gitService.GetFileContentsAtCommit("a.txt", commitHash)
            .Returns(new GitResult(0, "aaa"));
        _gitService.GetFileContentsAtCommit("b.txt", commitHash)
            .Returns(new GitResult(0, "bbb"));

        var dest = Path.Combine(_tempDir, "output");
        var result = _command.Execute("*.txt", dest, commitHash);

        Assert.Equal(0, result);
        Assert.Equal("aaa", File.ReadAllText(Path.Combine(dest, "a.txt")));
        Assert.Equal("bbb", File.ReadAllText(Path.Combine(dest, "b.txt")));
    }

    [Fact]
    public void Execute_WithNullCommitHash_UsesWorkingTree()
    {
        var repoRoot = Path.Combine(_tempDir, "repo");
        Directory.CreateDirectory(repoRoot);
        var sourceFile = Path.Combine(repoRoot, "test.txt");
        File.WriteAllText(sourceFile, "working tree content");

        _gitService.GetMatchingFiles("*.txt")
            .Returns(new[] { sourceFile });
        _gitService.GetRepositoryRoot().Returns(repoRoot);

        var dest = Path.Combine(_tempDir, "output");
        var result = _command.Execute("*.txt", dest, null);

        Assert.Equal(0, result);
        Assert.Equal("working tree content", File.ReadAllText(Path.Combine(dest, "test.txt")));
    }
}
