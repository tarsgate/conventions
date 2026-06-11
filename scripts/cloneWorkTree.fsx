#!/usr/bin/env -S dotnet fsi

open System
open System.IO
open System.Linq

#r "System.Configuration"
open System.Configuration

#r "nuget: Fsdk, Version=0.9.99--date20260525-0605.git-a5cfc39"

open Fsdk
open Fsdk.Process

let initialDir = Directory.GetCurrentDirectory()

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

type ArgType =
    | Url of fullUrl: string * owner: string * headBranch: string
    | FolderName

type InitialState =
    {
        RepoAndFolderName: string
        ArgType: ArgType
        AlreadyCloned: bool
    }

type BranchTargetInfo =
    {
        Name: string
        ExistsAlready: bool
        SubFolderName: string
    }

let IsUrl(str: string) : bool =
    str.Contains("://")
    || str.StartsWith("git@", StringComparison.OrdinalIgnoreCase)

let ExtractGhOwnerAndRepoNameFromUrl(maybeUrl: string) =
    if not(IsUrl maybeUrl) then
        failwith <| "Can't extract URL details from non-URL: " + maybeUrl

    let url = maybeUrl

    let pathPart =
        if url.StartsWith("git@", StringComparison.OrdinalIgnoreCase) then
            let colonIndex = url.IndexOf ':'
            url.Substring(colonIndex + 1)
        else
            (Uri url).AbsolutePath.TrimStart '/'

    let owner = pathPart.TrimEnd('/').Split('/').[0]

    let repoName =
        let pathPart =
            if url.StartsWith("git@", StringComparison.OrdinalIgnoreCase) then
                // SCP-style SSH URL: git@host:path/to/repo.git
                let colonIndex = url.IndexOf(':')

                if colonIndex < 0 then
                    failwith "Invalid SCP-style git URL: missing ':' separator"

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
            if lastSegment.EndsWith(".git", StringComparison.OrdinalIgnoreCase) then
                lastSegment.Substring(0, lastSegment.Length - ".git".Length)
            else
                lastSegment

    (owner, repoName)

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


// Determine branch existence via ls-remote against a single remote target (name or URL)
let CheckRemoteBranchExists branchName remote =
    let output =
        Process
            .Execute(
                {
                    Command = "git"
                    Arguments =
                        sprintf "ls-remote --heads %s %s" remote branchName
                },
                Echo.Off
            )
            .UnwrapDefault()
            .Trim()

    output.Contains branchName

let AllRemotes initialState =
    match initialState with
    | {
          ArgType = Url _
          AlreadyCloned = false
          RepoAndFolderName = repoName
      } when (not(Directory.Exists(Path.Combine(initialDir, repoName)))) ->
        failwithf
            "BUG: can't check remotes if repo is not cloned yet to %s"
            (Path.Combine(initialDir, repoName))
    | _ ->
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

        Misc.CrossPlatformStringSplitInLines remoteOutput
        |> Seq.map(fun line -> line.Trim())
        |> Seq.filter(fun trimmed -> not(String.IsNullOrEmpty trimmed))
        |> Seq.choose(fun trimmed ->
            let parts =
                trimmed.Split(
                    [| ' '; '\t' |],
                    StringSplitOptions.RemoveEmptyEntries
                )

            if parts.Length >= 2 then
                Some(parts.[0], parts.[1])
            else
                None
        )
        |> Seq.distinctBy fst
        |> Map.ofSeq

let (initialState, branchTargetInfo): (InitialState * BranchTargetInfo) =
    let args = Misc.FsxOnlyArguments()

    if args.Length <> 2 then
        let exitCode, errMsg = errUsage
        Console.Error.WriteLine errMsg

        Environment.Exit exitCode

    let firstArg = args.[0]
    let branchName = args.[1]

    // Sanitize branch name for use as a folder name by replacing slashes/backslashes with dashes
    let branchFolderName = branchName.Replace('/', '-').Replace('\\', '-')

    let firstArgIsUrl = IsUrl firstArg

    let alreadyCloned, argType, repoAndFolderName =
        if firstArgIsUrl then
            let owner, repoAndFolderName =
                ExtractGhOwnerAndRepoNameFromUrl firstArg

            let existing = Directory.Exists repoAndFolderName

            if existing then
                if (not(File.Exists(Path.Combine(repoAndFolderName, ".git"))))
                   || (not(
                       Directory.Exists(
                           Path.Combine(repoAndFolderName, ".bare")
                       )
                   )) then
                    let exitCode, errMsg =
                        ErrDirectoryIsNotClone repoAndFolderName

                    Console.Error.WriteLine errMsg
                    Environment.Exit exitCode

            if not existing then
                Directory.CreateDirectory repoAndFolderName
                |> ignore<DirectoryInfo>

            let headBranch =
                let gitSymbolicRemoteRef =
                    {
                        Command = "git"
                        Arguments =
                            sprintf "ls-remote --symref %s HEAD" firstArg
                    }

                // the split game below is meant to extract "master" from this example output:
                //     ref: refs/heads/master\tHEAD
                //     d2f140d0d...\tHEAD
                Process
                    .Execute(gitSymbolicRemoteRef, Echo.Off)
                    .UnwrapDefault()
                    .Trim()
                    .Split('/')
                    .Last()
                    .Split('\t')
                    .First()

            existing, Url(firstArg, owner, headBranch), repoAndFolderName
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

            true, FolderName, firstArg

    let initialState =
        {
            RepoAndFolderName = repoAndFolderName
            ArgType = argType
            AlreadyCloned = alreadyCloned
        }

    Directory.SetCurrentDirectory initialState.RepoAndFolderName

    let branchExists =
        let existsOnConfiguredRemotes() =
            AllRemotes initialState
            |> Map.toSeq
            |> Seq.map fst
            |> Seq.exists(CheckRemoteBranchExists branchName)

        match initialState with
        | {
              ArgType = Url(fullUrl, _owner, _headBranch)
              AlreadyCloned = false
          } -> CheckRemoteBranchExists branchName fullUrl
        | {
              ArgType = Url(fullUrl, _owner, _headBranch)
              AlreadyCloned = true
          } ->
            CheckRemoteBranchExists branchName fullUrl
            || existsOnConfiguredRemotes()
        | {
              ArgType = FolderName
              AlreadyCloned = true
          } -> existsOnConfiguredRemotes()
        | {
              ArgType = FolderName
              AlreadyCloned = false
          } -> false

    let branchTargetInfo =
        {
            Name = branchName
            SubFolderName = branchFolderName
            ExistsAlready = branchExists
        }

    initialState, branchTargetInfo

match initialState with
| {
      ArgType = Url(fullUrl, owner, headBranch)
      AlreadyCloned = false
      RepoAndFolderName = repoAndFolderName
  } ->
    let gitClone =
        {
            Command = "git"
            Arguments =
                sprintf
                    "clone --single-branch --branch %s --bare %s .bare"
                    headBranch
                    fullUrl
        }

    let cloneProc = Process.Execute(gitClone, Echo.All)

    match cloneProc.Result with
    | Error _ ->
        // Clean up the directory we created
        Directory.SetCurrentDirectory initialDir
        Directory.Delete(repoAndFolderName, true)
        let exitCode, errMsg = errGitCloneFailed
        Console.Error.WriteLine errMsg
        Environment.Exit exitCode
    | WarningsOrAmbiguous _
    | Success _ -> ()

    // Create .git file pointing to ./.bare (using F# instead of echo)
    File.WriteAllText(".git", "gitdir: ./.bare" + Environment.NewLine)

    let isGitHubUrl =
        fullUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase)

    // If repo is a GitHub fork, rename 'origin' remote to '<owner>Fork'
    if isGitHubUrl then
        match CheckIsGitHubFork owner repoAndFolderName with
        | None ->
            Console.Error.WriteLine(
                "Could not check whether the repo is a GitHub fork. Skipping remote rename."
            )
        | Some isFork ->
            if isFork then
                let newRemoteName = sprintf "%sFork" owner
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

| {
      ArgType = Url(fullUrl, owner, _headBranch)
      AlreadyCloned = true
      RepoAndFolderName = repoAndFolderName
  } ->
    let maybeFoundRemote =
        AllRemotes initialState
        |> Map.tryPick(fun name url ->
            if url.Contains fullUrl then
                Some name
            else
                None
        )

    match maybeFoundRemote with
    | Some _ -> ()
    | None ->
        let isGitHubUrl =
            fullUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase)

        let newRemoteName =
            if not isGitHubUrl then
                let exitCode, errMsg =
                    ErrNotGitHubCannotDetermineRemoteName repoAndFolderName

                Console.Error.WriteLine errMsg
                Environment.Exit exitCode
                failwith <| "Unreachable because of: " + errMsg
            else
                let remoteMap = AllRemotes initialState

                match CheckIsGitHubFork owner repoAndFolderName with
                | Some false ->
                    if not(Map.containsKey "upstream" remoteMap) then
                        "upstream"
                    elif not(Map.containsKey "origin" remoteMap) then
                        "origin"
                    else
                        let exitCode, errMsg = errCannotDetermineRemoteName

                        Console.Error.WriteLine errMsg

                        Environment.Exit exitCode
                        failwith <| "Unreachable because of: " + errMsg
                | Some true -> sprintf "%sFork" owner
                | None ->
                    let exitCode, errMsg = errCannotCheckGitHubFork
                    Console.Error.WriteLine errMsg

                    Environment.Exit exitCode
                    failwith <| "Unreachable because of: " + errMsg

        let addRemoteProc =
            Process.Execute(
                {
                    Command = "git"
                    Arguments = sprintf "remote add %s %s" newRemoteName fullUrl
                },
                Echo.All
            )

        match addRemoteProc.Result with
        | Error _ ->
            let exitCode, errMsg = ErrFailedToAddRemote fullUrl
            Console.Error.WriteLine errMsg

            Environment.Exit exitCode
        | WarningsOrAmbiguous _
        | Success _ -> ()
| _ -> ()

// Ensure the target branch and head branch are included in fetch refspec if it exists on remote
if branchTargetInfo.ExistsAlready then
    let branchesToFetch =
        match initialState with
        | {
              ArgType = Url(_fullUrl, _owner, headBranch)
              AlreadyCloned = _
              RepoAndFolderName = _
          } -> [ headBranch; branchTargetInfo.Name ]
        // TODO: add headBranch even if ArgType is not URL?
        | _ -> branchTargetInfo.Name :: List.Empty

    let allRemoteNames =
        AllRemotes initialState |> Map.toSeq |> Seq.map fst |> Seq.toList

    allRemoteNames
    |> Seq.iter(fun remoteName ->
        for branchToFetch in branchesToFetch do
            if CheckRemoteBranchExists branchToFetch remoteName then
                let gitCmd =
                    sprintf
                        "remote set-branches --add %s %s"
                        remoteName
                        branchToFetch

                let setBranchesProc =
                    Process.Execute(
                        {
                            Command = "git"
                            Arguments = gitCmd
                        },
                        Echo.All
                    )

                match setBranchesProc.Result with
                | Error(_exitCode, output) ->
                    Console.Error.WriteLine(output.ToString())
                    failwithf "Command 'git %s' failed" gitCmd
                | _ -> ()
    )

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

let gitWorktreeAddArgs =
    if branchTargetInfo.ExistsAlready then
        sprintf "%s %s" branchTargetInfo.SubFolderName branchTargetInfo.Name
    else
        sprintf "-b %s %s" branchTargetInfo.Name branchTargetInfo.SubFolderName

let cmd =
    {
        Command = "git"
        Arguments = sprintf "worktree add %s" gitWorktreeAddArgs
    }

let worktreeProc = Process.Execute(cmd, Echo.All)

match worktreeProc.Result with
| Error _ ->
    let exitCode, errMsg = errGitWorktreeAddFailed
    Console.Error.WriteLine errMsg
    Environment.Exit exitCode
| WarningsOrAmbiguous _
| Success _ -> ()

if branchTargetInfo.ExistsAlready then
    Console.WriteLine(
        sprintf
            "Successfully created worktree '%s' from branch '%s' of repo '%s'"
            branchTargetInfo.SubFolderName
            branchTargetInfo.Name
            initialState.RepoAndFolderName
    )
else
    Console.WriteLine(
        sprintf
            "Successfully created worktree '%s' from base branch of repo '%s'"
            branchTargetInfo.SubFolderName
            initialState.RepoAndFolderName
    )
