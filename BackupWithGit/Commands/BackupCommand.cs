namespace BackupWithGit.Commands;

/// <summary>
/// Handles the --backup|-b command: commits all changes with current date as message.
/// </summary>
public class BackupCommand
{
    private readonly IGitService _gitService;

    public BackupCommand(IGitService gitService)
    {
        _gitService = gitService;
    }

    public int Execute()
    {
        var commitMessage = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        Console.WriteLine($"Committing changes with message: {commitMessage}");
        
        var result = _gitService.Commit(commitMessage);
        
        if (result.Success)
        {
            Console.WriteLine(result.Output);
            return 0;
        }
        else
        {
            Console.Error.WriteLine($"Backup failed: {result.Output}");
            return 3;
        }
    }
}
