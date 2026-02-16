using NSubstitute;
using BackupWithGit.Commands;

namespace BackupWithGit.Tests;

public class SearchCommandTests
{
    private readonly IGitService _gitService;
    private readonly SearchCommand _command;

    public SearchCommandTests()
    {
        _gitService = Substitute.For<IGitService>();
        _command = new SearchCommand(_gitService);
    }

    [Fact]
    public void Execute_WhenNoFilesFound_ReturnsZero()
    {
        _gitService.SearchFiles("*.txt")
            .Returns(Enumerable.Empty<FileCommitInfo>());

        var result = _command.Execute("*.txt");

        Assert.Equal(0, result);
    }

    [Fact]
    public void Execute_WhenFilesFound_ReturnsZero()
    {
        var files = new List<FileCommitInfo>
        {
            new()
            {
                FilePath = "file1.txt",
                CommitHash = "abc1234567890",
                CommitMessage = "added file1",
                CommitDate = "2026-01-15 10:30:00 +0000"
            },
            new()
            {
                FilePath = "file2.txt",
                CommitHash = "def5678901234",
                CommitMessage = "added file2",
                CommitDate = "2026-01-16 11:00:00 +0000"
            }
        };

        _gitService.SearchFiles("*.txt").Returns(files);

        var result = _command.Execute("*.txt");

        Assert.Equal(0, result);
    }

    [Fact]
    public void Execute_WithMultipleCommitsPerFile_ReturnsZero()
    {
        var files = new List<FileCommitInfo>
        {
            new()
            {
                FilePath = "file1.txt",
                CommitHash = "abc1234567890",
                CommitMessage = "modified file1",
                CommitDate = "2026-01-16 12:00:00 +0000"
            },
            new()
            {
                FilePath = "file1.txt",
                CommitHash = "def5678901234",
                CommitMessage = "added file1",
                CommitDate = "2026-01-15 10:30:00 +0000"
            }
        };

        _gitService.SearchFiles("*.txt").Returns(files);

        var result = _command.Execute("*.txt");

        Assert.Equal(0, result);
    }

    [Fact]
    public void Execute_CallsSearchFilesWithCorrectPattern()
    {
        _gitService.SearchFiles(Arg.Any<string>())
            .Returns(Enumerable.Empty<FileCommitInfo>());

        _command.Execute("**/*.cs");

        _gitService.Received(1).SearchFiles("**/*.cs");
    }

    [Fact]
    public void Execute_WithUncommittedFile_ReturnsZero()
    {
        var files = new List<FileCommitInfo>
        {
            new()
            {
                FilePath = "newfile.txt",
                CommitHash = "(uncommitted)",
                CommitMessage = "",
                CommitDate = ""
            }
        };

        _gitService.SearchFiles("*.txt").Returns(files);

        var result = _command.Execute("*.txt");

        Assert.Equal(0, result);
    }
}
