namespace BackupWithGit.Commands;

/// <summary>
/// Handles the --backup-repo|-br command: syncs the git repo to a destination repository.
/// </summary>
public class BackupRepoCommand
{
    private readonly IGitService _gitService;

    public BackupRepoCommand(IGitService gitService)
    {
        _gitService = gitService;
    }

    public int Execute(string destination)
    {
        var destPath = Path.GetFullPath(destination);
        
        Console.WriteLine($"Syncing repository to: {destPath}");
        Console.WriteLine();

        var result = _gitService.SyncToRepository(destination);

        if (result.Success)
        {
            Console.WriteLine("Repository sync completed successfully.");
            if (!string.IsNullOrWhiteSpace(result.Output))
            {
                Console.WriteLine(result.Output);
            }
            return 0;
        }
        else
        {
            Console.Error.WriteLine($"Repository sync failed: {result.Output}");
            return 3;
        }
    }
}
