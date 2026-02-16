using NSubstitute;
using BackupWithGit.Commands;

namespace BackupWithGit.Tests;

public class BackupRepoCommandTests
{
    private readonly IGitService _gitService;
    private readonly BackupRepoCommand _command;

    public BackupRepoCommandTests()
    {
        _gitService = Substitute.For<IGitService>();
        _command = new BackupRepoCommand(_gitService);
    }

    [Fact]
    public void Execute_WhenSyncSucceeds_ReturnsZero()
    {
        _gitService.SyncToRepository(Arg.Any<string>())
            .Returns(new GitResult(0, "Everything up-to-date"));

        var result = _command.Execute(@"C:\backup\repo");

        Assert.Equal(0, result);
    }

    [Fact]
    public void Execute_WhenSyncFails_ReturnsThree()
    {
        _gitService.SyncToRepository(Arg.Any<string>())
            .Returns(new GitResult(1, "Destination is not a Git repository"));

        var result = _command.Execute(@"C:\nonexistent");

        Assert.Equal(3, result);
    }

    [Fact]
    public void Execute_CallsSyncToRepositoryWithCorrectDestination()
    {
        _gitService.SyncToRepository(Arg.Any<string>())
            .Returns(new GitResult(0, "ok"));

        _command.Execute(@"D:\my\backup");

        _gitService.Received(1).SyncToRepository(@"D:\my\backup");
    }

    [Fact]
    public void Execute_WhenSyncSucceedsWithEmptyOutput_ReturnsZero()
    {
        _gitService.SyncToRepository(Arg.Any<string>())
            .Returns(new GitResult(0, ""));

        var result = _command.Execute(@"C:\backup");

        Assert.Equal(0, result);
    }

    [Fact]
    public void Execute_WhenSyncSucceedsWithWhitespaceOutput_ReturnsZero()
    {
        _gitService.SyncToRepository(Arg.Any<string>())
            .Returns(new GitResult(0, "   "));

        var result = _command.Execute(@"C:\backup");

        Assert.Equal(0, result);
    }
}
