using System.CommandLine;
using BackupWithGit;
using BackupWithGit.Commands;

// Exit codes
const int ExitGitNotInstalled = 1;
const int ExitNotGitRepository = 2;

// Global option for source directory
var sourceOption = new Option<DirectoryInfo?>(
    aliases: ["--source", "-d"],
    description: "The source directory (defaults to current directory)");
sourceOption.SetDefaultValue(null);

// Root command
var rootCommand = new RootCommand("BackupWithGit - A Git-based backup utility")
{
    sourceOption
};

// --backup | -b command
var backupCommand = new Command("--backup", "Commit all changes with current date as message");
backupCommand.AddAlias("-b");
rootCommand.AddCommand(backupCommand);

backupCommand.SetHandler((DirectoryInfo? source) =>
{
    var exitCode = ValidateAndRun(source, gitService =>
    {
        var cmd = new BackupCommand(gitService);
        return cmd.Execute();
    });
    Environment.ExitCode = exitCode;
}, sourceOption);

// --search | -s command
var searchPatternArg = new Argument<string>("pattern", "Glob pattern to search for files");
var searchCommand = new Command("--search", "Find files matching a glob pattern with commit info")
{
    searchPatternArg
};
searchCommand.AddAlias("-s");
rootCommand.AddCommand(searchCommand);

searchCommand.SetHandler((DirectoryInfo? source, string pattern) =>
{
    var exitCode = ValidateAndRun(source, gitService =>
    {
        var cmd = new SearchCommand(gitService);
        return cmd.Execute(pattern);
    });
    Environment.ExitCode = exitCode;
}, sourceOption, searchPatternArg);

// --restore | -r command
var restorePatternArg = new Argument<string>("pattern", "Glob pattern to match files for restore");
var restoreDestArg = new Argument<string>("destination", "Destination folder to copy files to");
var restoreCommitOption = new Option<string?>(
    aliases: ["--commit", "-c"],
    description: "Restore files as they existed at the specified Git commit hash instead of the current working tree");
restoreCommitOption.SetDefaultValue(null);
var restoreCommand = new Command("--restore", "Copy files matching a glob pattern to a destination folder. Use --commit to restore from a specific commit")
{
    restorePatternArg,
    restoreDestArg,
    restoreCommitOption
};
restoreCommand.AddAlias("-r");
rootCommand.AddCommand(restoreCommand);

restoreCommand.SetHandler((DirectoryInfo? source, string pattern, string destination, string? commitHash) =>
{
    var exitCode = ValidateAndRun(source, gitService =>
    {
        var cmd = new RestoreCommand(gitService);
        return cmd.Execute(pattern, destination, commitHash);
    });
    Environment.ExitCode = exitCode;
}, sourceOption, restorePatternArg, restoreDestArg, restoreCommitOption);

// --backup-repo | -br command
var backupRepoDestArg = new Argument<string>("destination", "Destination folder containing the backup Git repository");
var backupRepoCommand = new Command("--backup-repo", "Sync the git repo to a destination repository")
{
    backupRepoDestArg
};
backupRepoCommand.AddAlias("-br");
rootCommand.AddCommand(backupRepoCommand);

backupRepoCommand.SetHandler((DirectoryInfo? source, string destination) =>
{
    var exitCode = ValidateAndRun(source, gitService =>
    {
        var cmd = new BackupRepoCommand(gitService);
        return cmd.Execute(destination);
    });
    Environment.ExitCode = exitCode;
}, sourceOption, backupRepoDestArg);

// Run the command
return await rootCommand.InvokeAsync(args);

/// <summary>
/// Validates Git installation and repository, then runs the specified action.
/// </summary>
int ValidateAndRun(DirectoryInfo? source, Func<GitService, int> action)
{
    string sourceDir = source?.FullName ?? Directory.GetCurrentDirectory();
    sourceDir = Path.GetFullPath(sourceDir);
    var gitService = new GitService(sourceDir);

    // Check if Git is installed
    if (!gitService.IsGitInstalled())
    {
        Console.Error.WriteLine("Error: Git is not installed or not found in PATH.");
        Console.Error.WriteLine("Please install Git and ensure it is available in your system PATH.");
        return ExitGitNotInstalled;
    }

    // Check if source is a Git repository
    if (!gitService.IsGitRepository())
    {
        Console.Error.WriteLine($"Error: The directory '{sourceDir}' is not a Git repository.");
        Console.Error.WriteLine("Please run this command from within a Git repository or specify a valid repository with --source.");
        return ExitNotGitRepository;
    }

    return action(gitService);
}

