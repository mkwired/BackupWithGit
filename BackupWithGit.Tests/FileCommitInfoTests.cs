namespace BackupWithGit.Tests;

public class FileCommitInfoTests
{
    [Fact]
    public void Properties_AreSetCorrectly()
    {
        var info = new FileCommitInfo
        {
            FilePath = "src/Program.cs",
            CommitHash = "abc1234567890",
            CommitMessage = "Initial commit",
            CommitDate = "2026-01-15 10:30:00 +0000"
        };

        Assert.Equal("src/Program.cs", info.FilePath);
        Assert.Equal("abc1234567890", info.CommitHash);
        Assert.Equal("Initial commit", info.CommitMessage);
        Assert.Equal("2026-01-15 10:30:00 +0000", info.CommitDate);
    }

    [Fact]
    public void UncommittedFile_HasExpectedPlaceholders()
    {
        var info = new FileCommitInfo
        {
            FilePath = "newfile.txt",
            CommitHash = "(uncommitted)",
            CommitMessage = "",
            CommitDate = ""
        };

        Assert.Equal("(uncommitted)", info.CommitHash);
        Assert.Empty(info.CommitMessage);
        Assert.Empty(info.CommitDate);
    }
}
