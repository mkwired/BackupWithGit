namespace BackupWithGit.Tests;

public class GitResultTests
{
    [Fact]
    public void Success_WhenExitCodeIsZero_ReturnsTrue()
    {
        var result = new GitResult(0, "ok");
        Assert.True(result.Success);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(128)]
    [InlineData(-1)]
    public void Success_WhenExitCodeIsNonZero_ReturnsFalse(int exitCode)
    {
        var result = new GitResult(exitCode, "error");
        Assert.False(result.Success);
    }

    [Fact]
    public void Output_ReturnsProvidedOutput()
    {
        var result = new GitResult(0, "some output");
        Assert.Equal("some output", result.Output);
    }

    [Fact]
    public void ExitCode_ReturnsProvidedExitCode()
    {
        var result = new GitResult(42, "");
        Assert.Equal(42, result.ExitCode);
    }

    [Fact]
    public void RecordEquality_TwoIdenticalResults_AreEqual()
    {
        var a = new GitResult(0, "output");
        var b = new GitResult(0, "output");
        Assert.Equal(a, b);
    }

    [Fact]
    public void RecordEquality_DifferentExitCode_AreNotEqual()
    {
        var a = new GitResult(0, "output");
        var b = new GitResult(1, "output");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void RecordEquality_DifferentOutput_AreNotEqual()
    {
        var a = new GitResult(0, "output1");
        var b = new GitResult(0, "output2");
        Assert.NotEqual(a, b);
    }
}
