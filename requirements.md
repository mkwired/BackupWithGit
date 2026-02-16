Create a C# project named BackupWithGit. The following are its requirements.

## General

- The program shall have a text-based user interface to allow for easy scripting.
- The program shall use the features of Git to accomplish its backup operations.
- The program shall detect if Git is installed on the system, and if not, report the missing dependency and return an error code.
- The program shall detect if the source directory is part of a Git repository, and if not, report the issue and return an error code.
- The source directory shall be the current directory unless overridden using the '--source|-d' command line argument.
- The program, when invoked with no command, shall display usage/help information.

## Exit Codes

- Exit code 0: Success (including when there are no changes to commit).
- Exit code 1: Git is not installed or not found in PATH.
- Exit code 2: The source directory is not a Git repository.
- Exit code 3: Command execution failure.

## Commands

### --backup | -b

- The program, when the command line argument '--backup|-b' is used, shall stage all changes (including untracked and deleted files) and commit them using Git with the commit message as the current date and time.
- If there are no changes to commit, the program shall report that the working tree is clean and return exit code 0.

### --search | -s

- The program, when the command line argument '--search|-s' is used, shall accept a glob pattern and find all tracked files that match that pattern.
- For each matching file, the program shall display every commit that touched the file — showing the file path, abbreviated commit hash, commit message, and commit date — ordered from most recent to oldest.

### --restore | -r

- The program, when the command line argument '--restore|-r' is used, shall accept a glob pattern and a destination folder and copy all tracked files that match that pattern to the destination folder.
- The program shall accept an optional '--commit|-c' option specifying a Git commit hash. When provided, the program shall restore the files as they existed at that commit instead of the current working tree.
- The program shall create the destination directory if it does not exist.
- The program shall preserve the relative directory structure of the source files within the destination.
- The program shall overwrite existing files in the destination.

### --backup-repo | -br

- The program, when the command line argument '--backup-repo|-br' is used, shall accept a destination folder and force-push the current branch to the Git repository found in the destination folder.
- The program shall verify the destination folder contains a valid Git repository, and if not, report the issue and return an error code.
- The program shall add or update a Git remote named 'backup' pointing to the destination, then force-push the current branch to it.