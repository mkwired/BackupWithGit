using NSubstitute;
using BackupWithGit.Commands;

namespace BackupWithGit.Tests;

public class BackupCommandTests
{
    private readonly IGitService _gitService;
    private readonly BackupCommand _command;

    public BackupCommandTests()
    {
        _gitService = Substitute.For<IGitService>();
        _command = new BackupCommand(_gitService);
    }

    [Fact]
    public void Execute_WhenCommitSucceeds_ReturnsZero()
    {
        _gitService.Commit(Arg.Any<string>())
            .Returns(new GitResult(0, "1 file changed"));

        var result = _command.Execute();

        Assert.Equal(0, result);
    }

    [Fact]
    public void Execute_WhenCommitFails_ReturnsThree()
    {
        _gitService.Commit(Arg.Any<string>())
            .Returns(new GitResult(1, "fatal: error"));

        var result = _command.Execute();

        Assert.Equal(3, result);
    }

    [Fact]
    public void Execute_CommitsWithDateTimeMessage()
    {
        var before = DateTime.Now;

        _gitService.Commit(Arg.Any<string>())
            .Returns(new GitResult(0, "ok"));

        _command.Execute();

        var after = DateTime.Now;

        var call = _gitService.ReceivedCalls().Single(c => c.GetMethodInfo().Name == "Commit");
        var message = (string)call.GetArguments()[0]!;
        Assert.True(DateTime.TryParse(message, out var parsed), $"Commit message '{message}' is not a valid date/time.");
        Assert.InRange(parsed, before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public void Execute_WhenNothingToCommit_ReturnsZero()
    {
        _gitService.Commit(Arg.Any<string>())
            .Returns(new GitResult(0, "Nothing to commit, working tree clean."));

        var result = _command.Execute();

        Assert.Equal(0, result);
    }
}
