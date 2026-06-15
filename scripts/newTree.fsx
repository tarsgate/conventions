#!/usr/bin/env -S dotnet fsi

open System
open System.IO
open System.Linq
open System.Net.Http
open System.Text.RegularExpressions

#r "System.Configuration"
open System.Configuration

#r "nuget: Fsdk, Version=0.9.99--date20260615-1007.git-0e932e5"

open Fsdk
open Fsdk.Process

#r "nuget: FSharp.Data, Version=5.0.2"

open FSharp.Data


let initialDir = Directory.GetCurrentDirectory()

let errUsage =
    (1,
     $"Usage: dotnet fsi {__SOURCE_FILE__} <repoUrl|folderPath|prUrl> [branchName]")

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

let ErrFailedToRenameRemote newName =
    (10, sprintf "Failed to rename remote 'origin' to '%s'." newName)

let errInvalidPrUrl =
    (11,
     "Invalid PR URL. Expected format: https://github.com/<owner>/<repo>/pull/<number>")

let ErrFailedToFetchPrData exMsg =
    (12, sprintf "Failed to fetch PR data from GitHub API: %s" exMsg)

let ErrFailedToExtractHeadRepoUrl exMsg =
    (13, sprintf "Failed to extract head repo URL from PR data: %s" exMsg)

let ErrFailedToExtractHeadBranch exMsg =
    (14, sprintf "Failed to extract head branch from PR data: %s" exMsg)

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

let prUrlRegex =
    Regex(
        @"^https?://github\.com/(?<owner>[^/]+)/(?<repo>[^/]+)/pull/(?<number>\d+)/?$",
        RegexOptions.IgnoreCase
    )

let TryParseGitHubPullRequestUrl
    (str: string)
    : Option<string * string * string> =
    if not(str.Contains("github.com", StringComparison.OrdinalIgnoreCase)) then
        None
    else
        let matchInfo = prUrlRegex.Match str

        if matchInfo.Success then
            Some(
                matchInfo.Groups.["owner"].Value,
                matchInfo.Groups.["repo"].Value,
                matchInfo.Groups.["number"].Value
            )
        else
            None

let IsGitHubPullRequestUrl(str: string) : bool =
    TryParseGitHubPullRequestUrl str |> Option.isSome

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

let CreateGitHubHttpClient() =
    let httpClient = new HttpClient()
    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd "newTree.fsx"
    httpClient

let CheckIsGitHubFork (owner: string) (repo: string) : Option<bool> =
    use httpClient = CreateGitHubHttpClient()
    let apiUrl = sprintf "https://api.github.com/repos/%s/%s" owner repo

    try
        let response =
            httpClient.GetStringAsync apiUrl
            |> Async.AwaitTask
            |> Async.RunSynchronously

        Some(response.Contains "\"fork\":true")
    with
    | _ -> None

let ResolvePrUrl(prUrl: string) : string * string =
    let owner, repo, prNumber =
        match TryParseGitHubPullRequestUrl prUrl with
        | Some(owner, repo, number) -> owner, repo, number
        | None ->
            let exitCode, errMsg = errInvalidPrUrl
            Console.Error.WriteLine errMsg
            Environment.Exit exitCode
            failwith <| "Unreachable because of: " + errMsg

    let apiUrl = $"https://api.github.com/repos/{owner}/{repo}/pulls/{prNumber}"

    use httpClient = CreateGitHubHttpClient()

    let response =
        try
            httpClient.GetStringAsync(apiUrl).Result
        with
        | ex ->
            let exitCode, errMsg = ErrFailedToFetchPrData ex.Message
            Console.Error.WriteLine errMsg
            Environment.Exit exitCode
            failwith <| "Unreachable because of: " + errMsg

    let prJson = JsonValue.Parse response

    let headRepoUrl =
        try
            prJson
                .GetProperty("head")
                .GetProperty("repo")
                .GetProperty("ssh_url")
                .AsString()
        with
        | ex ->
            let exitCode, errMsg = ErrFailedToExtractHeadRepoUrl ex.Message
            Console.Error.WriteLine errMsg
            Environment.Exit exitCode
            failwith <| "Unreachable because of: " + errMsg

    let headBranch =
        try
            prJson
                .GetProperty("head")
                .GetProperty("ref")
                .AsString()
        with
        | ex ->
            let exitCode, errMsg = ErrFailedToExtractHeadBranch ex.Message
            Console.Error.WriteLine errMsg
            Environment.Exit exitCode
            failwith <| "Unreachable because of: " + errMsg

    Console.WriteLine $"PR #{prNumber} source repo: {headRepoUrl}"
    Console.WriteLine $"PR #{prNumber} source branch: {headBranch}"
    headRepoUrl, headBranch


// Determine branch existence via ls-remote against a single remote target (name or URL)
let CheckRemoteBranchExists branchName remote =
    let output =
        Process
            .ExecDefault(
                sprintf "git ls-remote --heads %s %s" remote branchName,
                echo = Echo.Off
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
                .ExecDefault("git remote --verbose", echo = Echo.Off)
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

let GetCurrentHeadBranch(repoDir: Option<string>) =
    let maybeExtraFlag =
        match repoDir with
        | Some dir -> sprintf "-C %s" dir
        | None -> String.Empty

    let cmd = sprintf "git %s symbolic-ref --short HEAD" maybeExtraFlag

    Process
        .ExecDefault(cmd, echo = Echo.Off)
        .UnwrapDefault(throwWhenWarnings = false)
        .Trim()

let rawArgs = Misc.FsxOnlyArguments()

if rawArgs.Length < 1 || rawArgs.Length > 2 then
    let exitCode, errMsg = errUsage
    Console.Error.WriteLine errMsg
    Environment.Exit exitCode

// If single arg is a PR URL, resolve it to repo URL + branch before proceeding
let resolvedArgs =
    if IsGitHubPullRequestUrl rawArgs.[0] then
        if rawArgs.Length = 2 then
            let exitCode, _ = errUsage

            Console.Error.WriteLine
                "A PR URL already encodes the branch name. Do not specify a second argument."

            Environment.Exit exitCode

        let headRepoUrl, headBranch = ResolvePrUrl rawArgs.[0]
        [ headRepoUrl; headBranch ]
    else
        rawArgs

let (initialState, branchTargetInfo): (InitialState * BranchTargetInfo) =
    let args = resolvedArgs

    if args.Length < 1 || args.Length > 2 then
        let exitCode, errMsg = errUsage
        Console.Error.WriteLine errMsg
        Environment.Exit exitCode

    let firstArg = args.[0]

    let maybeBranchName =
        if args.Length = 2 then
            Some args.[1]
        else
            None

    let firstArgIsUrl = IsUrl firstArg

    let alreadyCloned, argType, repoAndFolderName, defaultBranchName =
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
                // the split game below is meant to extract "master" from this example output:
                //     ref: refs/heads/master\tHEAD
                //     d2f140d0d...\tHEAD
                Process
                    .ExecDefault(
                        sprintf "git ls-remote --symref %s HEAD" firstArg,
                        echo = Echo.Off
                    )
                    .UnwrapDefault()
                    .Trim()
                    // FIXME: this parsing logic would be broken in case there's a head branch with a slash in its name,
                    //        but that would be a very weird naming for a head branch, so let's consider this an edge case
                    .Split(
                        '/'
                    )
                    .Last()
                    .Split('\t')
                    .First()

            existing,
            Url(firstArg, owner, headBranch),
            repoAndFolderName,
            headBranch
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

            true, FolderName, firstArg, GetCurrentHeadBranch(Some fullPath)

    let branchName =
        match maybeBranchName with
        | Some name -> name
        | None -> defaultBranchName

    // Sanitize branch name for use as a folder name by replacing slashes/backslashes with dashes
    let branchFolderName = branchName.Replace('/', '-').Replace('\\', '-')

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
      ArgType = Url(fullUrl, owner, _headBranch)
      AlreadyCloned = false
      RepoAndFolderName = repoAndFolderName
  } ->
    try
        Process
            .ExecDefault(
                sprintf "git clone --single-branch --bare %s .bare" fullUrl
            )
            .UnwrapDefault(throwWhenWarnings = false)
        |> ignore<string>
    with
    | _ ->
        // Clean up the directory we created
        Directory.SetCurrentDirectory initialDir
        Directory.Delete(repoAndFolderName, true)
        let exitCode, errMsg = errGitCloneFailed
        Console.Error.WriteLine errMsg
        Environment.Exit exitCode

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

                Process
                    .ExecDefault(
                        sprintf "git remote rename origin %s" newRemoteName
                    )
                    .UnwrapDefault(throwWhenWarnings = false)
                |> ignore<string>

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

        try
            Process
                .ExecDefault(
                    sprintf "git remote add %s %s" newRemoteName fullUrl
                )
                .UnwrapDefault(throwWhenWarnings = false)
            |> ignore<string>
        with
        | _ ->
            let exitCode, errMsg = ErrFailedToAddRemote fullUrl
            Console.Error.WriteLine errMsg
            Environment.Exit exitCode
| _ -> ()

// Ensure the target branch (and head branch, in case we want to rebase) are
// included in fetch refspec if it exists on remote
if branchTargetInfo.ExistsAlready then
    let branchesToFetch =
        let headBranch =
            match initialState with
            | {
                  ArgType = Url(_fullUrl, _owner, headBranch)
                  AlreadyCloned = _
                  RepoAndFolderName = _
              } -> headBranch
            | _ -> GetCurrentHeadBranch None

        [ headBranch; branchTargetInfo.Name ]

    let allRemoteNames =
        AllRemotes initialState |> Map.toSeq |> Seq.map fst |> Seq.toList

    allRemoteNames
    |> Seq.iter(fun remoteName ->
        for branchToFetch in branchesToFetch do
            if CheckRemoteBranchExists branchToFetch remoteName then
                let setBranchesCmd =
                    sprintf
                        "git remote set-branches --add %s %s"
                        remoteName
                        branchToFetch

                Process
                    .ExecDefault(setBranchesCmd)
                    .UnwrapDefault(throwWhenWarnings = false)
                |> ignore<string>
    )

// Fetch all remotes
Process
    .ExecDefault("git fetch --all")
    .UnwrapDefault(throwWhenWarnings = false)
|> ignore<string>

let gitWorktreeAddArgs =
    if branchTargetInfo.ExistsAlready then
        // Use remote tracking ref to avoid ambiguity when multiple remotes have the same branch
        let allRemoteNames =
            AllRemotes initialState |> Map.toSeq |> Seq.map fst |> Seq.toList

        let remotesWithBranch =
            allRemoteNames
            |> List.filter(fun remoteName ->
                CheckRemoteBranchExists branchTargetInfo.Name remoteName
            )

        // Prefer: input-URL remote > upstream/origin > non-fork remotes
        // TODO: improve this heuristic — consider checking which branch has more commits,
        // or which branch's latest commit is more recent
        let preferredRemote =
            match remotesWithBranch with
            | [] -> None // fallback
            | singleRemote :: [] -> Some singleRemote
            | _multipleRemotes ->
                // Determine which remote corresponds to the input URL (if first arg was a URL)
                let inputUrlRemote =
                    match initialState with
                    | {
                          ArgType = Url(fullUrl, _, _)
                      } ->
                        AllRemotes initialState
                        |> Map.tryPick(fun name url ->
                            if url.Contains fullUrl then
                                Some name
                            else
                                None
                        )
                    | _ -> None

                let fromInputUrl =
                    inputUrlRemote
                    |> Option.bind(fun urlRemote ->
                        remotesWithBranch
                        |> List.tryFind(fun name -> name = urlRemote)
                    )

                fromInputUrl
                |> Option.orElseWith(fun () ->
                    remotesWithBranch
                    |> List.tryFind(fun name -> name = "upstream")
                )
                |> Option.orElseWith(fun () ->
                    remotesWithBranch
                    |> List.tryFind(fun name -> name = "origin")
                )
                |> Option.orElseWith(fun () ->
                    remotesWithBranch
                    |> List.tryFind(fun name ->
                        not(
                            name.EndsWith(
                                "Fork",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                    )
                )
                |> Option.orElse(List.tryHead remotesWithBranch)

        match preferredRemote with
        | Some remote ->
            sprintf
                "%s %s/%s"
                branchTargetInfo.SubFolderName
                remote
                branchTargetInfo.Name
        | None ->
            sprintf "%s %s" branchTargetInfo.SubFolderName branchTargetInfo.Name
    else
        sprintf "-b %s %s" branchTargetInfo.Name branchTargetInfo.SubFolderName

Process
    .ExecDefault(sprintf "git worktree add %s" gitWorktreeAddArgs)
    .UnwrapDefault(throwWhenWarnings = false)
|> ignore<string>

// If branch already existed, worktree was created from a remote tracking ref
// and is in detached HEAD state; create or reset the local branch to HEAD
// (which is the latest remote tracking ref), then switch to it
if branchTargetInfo.ExistsAlready then
    let fullCmd =
        sprintf
            "git -C %s checkout -B %s"
            branchTargetInfo.SubFolderName
            branchTargetInfo.Name

    Process
        .ExecDefault(fullCmd)
        .UnwrapDefault(throwWhenWarnings = false)
    |> ignore<string>

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
