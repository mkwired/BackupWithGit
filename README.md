# BackupWithGit User Manual

BackupWithGit is a command-line utility that uses Git as a backup engine for a working directory that is already stored in a Git repository.

It supports four main tasks:

- create a backup commit of all current changes
- search tracked files and show their commit history
- restore tracked files to another folder
- push the current branch to another Git repository for off-machine backup

## Requirements

- Git must be installed and available on your `PATH`
- the source folder must be inside a Git repository
- if you are running from source, you need the .NET SDK required by the project

## Running the program

If you have built or published the application, run it as:

```bash
BackupWithGit [command] [options]
```

If you are running it from this repository, use:

```bash
dotnet run --project BackupWithGit/BackupWithGit.csproj -- [command] [options]
```

To display built-in help:

```bash
BackupWithGit --help
```

## Source directory

By default, BackupWithGit works against the current directory.

Use `--source` or `-d` to point at another repository:

```bash
BackupWithGit --backup --source /path/to/repo
```

## Commands

### 1. Create a backup commit

Command:

```bash
BackupWithGit --backup
```

Short form:

```bash
BackupWithGit -b
```

What it does:

- stages all changes with `git add -A`
- includes new, modified, and deleted files
- creates a commit using the current local date and time as the commit message
- returns success if there is nothing to commit

Typical use:

```bash
BackupWithGit --backup
```

### 2. Search for files and show commit history

Command:

```bash
BackupWithGit --search <pattern>
```

Short form:

```bash
BackupWithGit -s <pattern>
```

What it does:

- searches tracked files that match the supplied glob pattern
- shows every commit that touched each matching file
- lists results from most recent to oldest
- displays file path, abbreviated commit hash, commit message, and commit date

Examples:

```bash
BackupWithGit --search "*.txt"
BackupWithGit --search "**/*.cs"
```

Notes:

- only tracked files are searched
- a file that matches but has not been committed yet is shown as `(uncommitted)`

### 3. Restore files to another folder

Command:

```bash
BackupWithGit --restore <pattern> <destination>
```

Short form:

```bash
BackupWithGit -r <pattern> <destination>
```

Optional commit selection:

```bash
BackupWithGit --restore <pattern> <destination> --commit <commit>
```

What it does:

- finds tracked files that match the glob pattern
- copies them into the destination folder
- creates the destination folder if needed
- preserves the files' relative directory structure
- overwrites existing files in the destination
- can restore either from the current working tree or from a specific commit

Examples:

```bash
BackupWithGit --restore "*.json" /tmp/restore
BackupWithGit --restore "**/*.cs" /tmp/restore --commit abc1234
```

Notes:

- when `--commit` is omitted, files are copied from the current working tree
- when `--commit` is supplied, files are restored as they existed in that commit

### 4. Back up the repository to another Git repository

Command:

```bash
BackupWithGit --backup-repo <destination>
```

Short form:

```bash
BackupWithGit -br <destination>
```

What it does:

- verifies the destination is a Git repository
- adds or updates a remote named `backup`
- force-pushes the current branch to that remote

Example:

```bash
BackupWithGit --backup-repo /backups/project.git
```

Important:

- this command performs a force push
- a bare repository is the safest destination for backups
- if you push to a non-bare repository with a checked-out branch, Git may reject the push unless that repository is configured to allow it

## Glob pattern examples

Examples of patterns you can use with `--search` and `--restore`:

- `*.txt` - all tracked `.txt` files
- `*.cs` - all tracked `.cs` files
- `**/*.cs` - tracked `.cs` files in nested folders
- `docs/*.md` - tracked Markdown files directly under `docs`

## Exit codes

BackupWithGit returns these process exit codes:

- `0` - success, including "nothing to commit" and "no files matched"
- `1` - Git is not installed or not found in `PATH`
- `2` - the source directory is not a Git repository
- `3` - command execution failed

## Troubleshooting

### "Git is not installed or not found in PATH"

Install Git and make sure the `git` command works in your terminal.

### "The directory '...' is not a Git repository"

Run the command from inside a Git repository, or pass the repository path with `--source`.

### No files found matching the pattern

Make sure:

- the files are tracked by Git
- the glob pattern matches the file paths you expect
- if needed, use a recursive pattern such as `**/*.ext`
