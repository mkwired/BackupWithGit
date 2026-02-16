using Spectre.Console;

namespace BackupWithGit.Commands;

/// <summary>
/// Handles the --search|-s command: finds files matching a glob pattern with their commit info.
/// </summary>
public class SearchCommand
{
    private readonly IGitService _gitService;

    public SearchCommand(IGitService gitService)
    {
        _gitService = gitService;
    }

    public int Execute(string pattern)
    {
        AnsiConsole.MarkupLine($"[grey]Searching for files matching:[/] [yellow]{Markup.Escape(pattern)}[/]");
        AnsiConsole.WriteLine();

        var results = _gitService.SearchFiles(pattern).ToList();

        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No files found matching the pattern.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Expand();

        table.AddColumn(new TableColumn("[bold]File[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Commit[/]").Centered());
        table.AddColumn(new TableColumn("[bold]Message[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Date[/]").RightAligned());

        // Group by file so we can show the file path only on the first row of each group
        var groupedResults = results
            .GroupBy(r => r.FilePath)
            .ToList();

        foreach (var group in groupedResults)
        {
            bool first = true;
            foreach (var file in group)
            {
                var shortHash = file.CommitHash.Length > 7 ? file.CommitHash[..7] : file.CommitHash;
                var fileCell = first ? Markup.Escape(file.FilePath) : "";
                var commitCell = $"[grey]{Markup.Escape(shortHash)}[/]";
                var messageCell = Markup.Escape(file.CommitMessage);
                var dateCell = $"[dim]{Markup.Escape(file.CommitDate)}[/]";

                table.AddRow(fileCell, commitCell, messageCell, dateCell);
                first = false;
            }

            table.AddEmptyRow();
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        var fileCount = groupedResults.Count;
        var commitCount = results.Count;
        AnsiConsole.MarkupLine($"[green]Found[/] [bold]{fileCount}[/] [green]file(s) with[/] [bold]{commitCount}[/] [green]commit(s).[/]");

        return 0;
    }
}
