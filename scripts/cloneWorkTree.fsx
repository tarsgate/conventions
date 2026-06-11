#!/usr/bin/env -S dotnet fsi

open System
open System.IO

#r "System.Configuration"
open System.Configuration

#r "nuget: Fsdk, Version=0.9.99--date20260525-0605.git-a5cfc39"

open Fsdk
open Fsdk.Process

let errUsage =
    (1, $"Usage: dotnet fsi {__SOURCE_FILE__} <repoUrl|folderPath> <branchName>")

let ErrDirectoryDoesNotExist path =
    (2, sprintf "Directory '%s' does not exist." path)

let ErrDirectoryIsNotClone path =
    (3, sprintf "Directory '%s' already exists and is not a clone." path)

let errCannotDetermineRemoteName =
    (4,
     "Both 'upstream' and 'origin' remotes already exist and the new remote is not a fork. Cannot determine a name for the new remote.")

let errCannotCheckGitHubFork =
    (5,
     "Could not check whether the repo is a GitHub fork. Cannot determine a name for the new remote.")

let ErrFailedToAddRemote url =
    (6, sprintf "Failed to add remote '%s'." url)

let errGitFetchAllFailed = (7, "Git fetch --all failed.")

let errGitCloneFailed = (8, "Git clone failed.")

let ErrNotGitHubCannotDetermineRemoteName dir =
    (9,
     sprintf
         "Directory '%s' already exists; URL is not GitHub so API cannot be queried to find best name for new remote"
         dir)

let errGitWorktreeAddFailed = (10, "Git worktree add failed.")

let errGitCheckoutFailed = (11, "Git checkout -b failed.")

let ErrFailedToRenameRemote newName =
    (12, sprintf "Failed to rename remote 'origin' to '%s'." newName)

let args = Misc.FsxOnlyArguments()

if args.Length <> 2 then
    let exitCode, errMsg = errUsage
    Console.Error.WriteLine errMsg

    Environment.Exit exitCode

let firstArg = args.[0]
let branchName = args.[1]

// Sanitize branch name for use as a folder name by replacing slashes/backslashes with dashes
let branchFolderName = branchName.Replace('/', '-').Replace('\\', '-')

let IsUrl(str: string) : bool =
    str.Contains("://")
    || str.StartsWith("git@", StringComparison.OrdinalIgnoreCase)

let isUrl = IsUrl firstArg

// 1) Extract repo name from URL, or validate folder path
let repoUrl, repoName, isExistingClone =
    if isUrl then
        let url = firstArg

        let name =
            let pathPart =
                if url.StartsWith("git@", StringComparison.OrdinalIgnoreCase) then
                    // SCP-style SSH URL: git@host:path/to/repo.git
                    let colonIndex = url.IndexOf(':')

                    if colonIndex < 0 then
                        failwith
                            "Invalid SCP-style git URL: missing ':' separator"

                    url.Substring(colonIndex + 1)
                else
                    // Standard URI (https, ssh://, file://, etc.)
                    let uri = Uri url
                    uri.AbsolutePath

            let segments = pathPart.TrimEnd('/').Split('/')
            let lastSegmentOpt = Array.tryLast segments

            match lastSegmentOpt with
            | None -> failwith "Unreachable"
            | Some lastSegment ->
                if
                    lastSegment.EndsWith
                        (
                            ".git",
                            StringComparison.OrdinalIgnoreCase
                        )
                then
                    lastSegment.Substring(0, lastSegment.Length - ".git".Length)
                else
                    lastSegment

        let existing =
            Directory.Exists name
            && File.Exists(Path.Combine(name, ".git"))
            && Directory.Exists(Path.Combine(name, ".bare"))

        url, name, existing
    else
        let fullPath = Path.GetFullPath firstArg

        if not(Directory.Exists fullPath) then
            let exitCode, errMsg = ErrDirectoryDoesNotExist firstArg
            Console.Error.WriteLine errMsg

            Environment.Exit exitCode

        let gitFile = Path.Combine(fullPath, ".git")
        let bareDir = Path.Combine(fullPath, ".bare")

        if not(File.Exists gitFile) || not(Directory.Exists bareDir) then
            let exitCode, errMsg = ErrDirectoryIsNotClone firstArg
            Console.Error.WriteLine errMsg

            Environment.Exit exitCode

        let name = Path.GetFileName(Path.TrimEndingDirectorySeparator fullPath)

        String.Empty, name, true

let ghOwner =
    if isUrl then
        let pathPart =
            if repoUrl.StartsWith("git@", StringComparison.OrdinalIgnoreCase) then
                let colonIndex = repoUrl.IndexOf ':'
                repoUrl.Substring(colonIndex + 1)
            else
                (Uri repoUrl).AbsolutePath.TrimStart '/'

        pathPart.TrimEnd('/').Split('/').[0]
    else
        String.Empty

let CheckIsGitHubFork (owner: string) (repo: string) : Option<bool> =
    use httpClient = new System.Net.Http.HttpClient()

    // required or HTTP call will fail
    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd "dotnet-fsi"

    let apiUrl = sprintf "https://api.github.com/repos/%s/%s" owner repo

    try
        let response =
            httpClient.GetStringAsync apiUrl
            |> Async.AwaitTask
            |> Async.RunSynchronously

        Some(response.Contains "\"fork\":true")
    with
    | _ -> None

// Check branch existence on remote only when a URL was given
let remoteBranchExists =
    if not isUrl then
        false
    else
        let output =
            Process
                .Execute(
                    {
                        Command = "git"
                        Arguments =
                            sprintf "ls-remote --heads %s %s" repoUrl branchName
                    },
                    Echo.Off
                )
                .UnwrapDefault()
                .Trim()

        output.Contains branchName

// 2) Handle directory creation or validate existing clone
if not isExistingClone then
    Directory.CreateDirectory repoName |> ignore<DirectoryInfo>

// 3) cd into that folder
let targetDir =
    if isUrl then
        repoName
    else
        firstArg

Directory.SetCurrentDirectory targetDir

// Determine branch existence after entering the directory
let branchExists =
    if isUrl then
        remoteBranchExists
    else
        let localCheck =
            Process.Execute(
                {
                    Command = "git"
                    Arguments =
                        sprintf
                            "rev-parse --verify --quiet refs/heads/%s"
                            branchName
                },
                Echo.Off
            )

        match localCheck.Result with
        | Error _ -> false
        | WarningsOrAmbiguous _
        | Success _ -> true

if isExistingClone then
    if isUrl then
        // Check if repoUrl is already a remote
        let remoteOutput =
            Process
                .Execute(
                    {
                        Command = "git"
                        Arguments = "remote --verbose"
                    },
                    Echo.Off
                )
                .UnwrapDefault()

        let remoteAlreadyExists =
            Misc.CrossPlatformStringSplitInLines remoteOutput
            |> Seq.exists(fun line -> line.Contains repoUrl)

        if not remoteAlreadyExists then
            let existingRemotes =
                Misc.CrossPlatformStringSplitInLines remoteOutput
                |> Seq.choose(fun line ->
                    let trimmed = line.Trim()

                    if String.IsNullOrEmpty trimmed then
                        None
                    else
                        let parts =
                            trimmed.Split(
                                [| ' '; '\t' |],
                                StringSplitOptions.RemoveEmptyEntries
                            )

                        if parts.Length > 0 then
                            Some parts.[0]
                        else
                            None
                )
                |> Seq.distinct
                |> Seq.toList

            let isGitHubUrl =
                repoUrl.Contains(
                    "github.com",
                    StringComparison.OrdinalIgnoreCase
                )

            let remoteName =
                if not isGitHubUrl then
                    let exitCode, errMsg =
                        ErrNotGitHubCannotDetermineRemoteName repoName

                    Console.Error.WriteLine errMsg
                    Environment.Exit exitCode
                    failwith <| "Unreachable because of: " + errMsg
                else
                    match CheckIsGitHubFork ghOwner repoName with
                    | Some false ->
                        if not(List.contains "upstream" existingRemotes) then
                            "upstream"
                        elif not(List.contains "origin" existingRemotes) then
                            "origin"
                        else
                            let exitCode, errMsg = errCannotDetermineRemoteName
                            Console.Error.WriteLine errMsg

                            Environment.Exit exitCode
                            failwith <| "Unreachable because of: " + errMsg
                    | Some true -> sprintf "%sFork" ghOwner
                    | None ->
                        let exitCode, errMsg = errCannotCheckGitHubFork
                        Console.Error.WriteLine errMsg

                        Environment.Exit exitCode
                        failwith <| "Unreachable because of: " + errMsg

            let addRemoteProc =
                Process.Execute(
                    {
                        Command = "git"
                        Arguments =
                            sprintf "remote add %s %s" remoteName repoUrl
                    },
                    Echo.All
                )

            match addRemoteProc.Result with
            | Error _ ->
                let exitCode, errMsg = ErrFailedToAddRemote repoUrl
                Console.Error.WriteLine errMsg

                Environment.Exit exitCode
            | WarningsOrAmbiguous _
            | Success _ -> ()

    // Fetch all remotes
    let fetchProc =
        Process.Execute(
            {
                Command = "git"
                Arguments = "fetch --all"
            },
            Echo.All
        )

    match fetchProc.Result with
    | Error _ ->
        let exitCode, errMsg = errGitFetchAllFailed
        Console.Error.WriteLine errMsg
        Environment.Exit exitCode
    | WarningsOrAmbiguous _
    | Success _ -> ()
else
    let cloneSpecificSingleBranch =
        if branchExists then
            sprintf "--branch %s" branchName
        else
            String.Empty

    // 4) git clone --single-branch --bare <repository-url> .bare
    let gitClone =
        {
            Command = "git"
            Arguments =
                sprintf
                    "clone --single-branch %s --bare -- %s .bare"
                    cloneSpecificSingleBranch
                    repoUrl
        }

    let cloneProc = Process.Execute(gitClone, Echo.All)

    match cloneProc.Result with
    | Error _ ->
        // Clean up the directory we created
        Directory.SetCurrentDirectory("..")
        Directory.Delete(repoName, true)
        let exitCode, errMsg = errGitCloneFailed
        Console.Error.WriteLine errMsg
        Environment.Exit exitCode
    | WarningsOrAmbiguous _
    | Success _ -> ()

    // 5) Create .git file pointing to ./.bare (using F# instead of echo)
    File.WriteAllText(".git", "gitdir: ./.bare" + Environment.NewLine)

// 6) Find default branch
let gitSymbolicRef =
    {
        Command = "git"
        Arguments = "symbolic-ref HEAD --short"
    }

let baseBranch =
    if branchExists then
        branchName
    else
        Process
            .Execute(gitSymbolicRef, Echo.Off)
            .UnwrapDefault()
            .Trim()

// 7) git worktree add
let gitWorktreeAdd =
    if branchExists then
        {
            Command = "git"
            Arguments = sprintf "worktree add %s %s" branchFolderName baseBranch
        }
    else
        {
            Command = "git"
            Arguments =
                sprintf "worktree add -b %s %s" branchName branchFolderName
        }

let worktreeProc = Process.Execute(gitWorktreeAdd, Echo.All)

match worktreeProc.Result with
| Error _ ->
    let exitCode, errMsg = errGitWorktreeAddFailed
    Console.Error.WriteLine errMsg
    Environment.Exit exitCode
| WarningsOrAmbiguous _
| Success _ -> ()

// 8) cd into branchName
Directory.SetCurrentDirectory branchFolderName

Console.WriteLine(
    sprintf
        "Successfully created worktree '%s' from branch '%s' of repo '%s'"
        baseBranch
        branchName
        repoName
)

// 9) If repo is a GitHub fork, rename 'origin' remote to '<owner>Fork'
if not isExistingClone then
    let isGitHubUrl =
        repoUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase)

    if isGitHubUrl then
        match CheckIsGitHubFork ghOwner repoName with
        | None ->
            Console.Error.WriteLine(
                "Could not check whether the repo is a GitHub fork. Skipping remote rename."
            )
        | Some isFork ->
            if isFork then
                let newRemoteName = sprintf "%sFork" ghOwner
                let renameArgs = sprintf "remote rename origin %s" newRemoteName

                let gitRemoteRename =
                    {
                        Command = "git"
                        Arguments = renameArgs
                    }

                let renameProc = Process.Execute(gitRemoteRename, Echo.All)

                match renameProc.Result with
                | Error _ ->
                    let exitCode, errMsg = ErrFailedToRenameRemote newRemoteName

                    Console.Error.WriteLine errMsg
                    Environment.Exit exitCode
                | WarningsOrAmbiguous _
                | Success _ -> ()
