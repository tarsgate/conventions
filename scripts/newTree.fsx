#!/usr/bin/env -S dotnet fsi

open System
// open System.IO -- REMOVED; use AbsDir/AbsFile everywhere
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

type AbsFile(fileInfo: System.IO.FileInfo, ?checkExistence: bool) =
    let shouldCheck = defaultArg checkExistence true

    do
        if not(System.IO.Path.IsPathRooted fileInfo.FullName) then
            failwithf
                "AbsFile: path must be absolute, not relative: %s"
                fileInfo.FullName

        if shouldCheck && not fileInfo.Exists then
            raise <| System.IO.FileNotFoundException fileInfo.FullName

    member self.PathlessNameWithoutExtension =
        System.IO.Path.GetFileNameWithoutExtension fileInfo.Name

    member self.PathlessNameWithExtension = fileInfo.Name

    member self.FullPath =
        let path = fileInfo.FullName

        if path.Contains(" ") then
            "\"" + path + "\""
        else
            path

    member self.Exists = fileInfo.Exists

    member self.WriteAllText(contents: string) =
        System.IO.File.WriteAllText(fileInfo.FullName, contents)

    new(path: string, ?checkExistence: bool) =
        if not(System.IO.Path.IsPathRooted path) then
            failwithf "AbsFile secondary ctor received a relative path: %s" path

        AbsFile(System.IO.FileInfo(path), ?checkExistence = checkExistence)

type AbsDir(dirInfo: System.IO.DirectoryInfo, ?checkExistence: bool) =
    let shouldCheck = defaultArg checkExistence true

    do
        if not(System.IO.Path.IsPathRooted dirInfo.FullName) then
            failwithf
                "AbsDir: path must be absolute, not relative: %s"
                dirInfo.FullName

        if shouldCheck && not dirInfo.Exists then
            raise <| System.IO.DirectoryNotFoundException dirInfo.FullName

    member self.PathlessName = dirInfo.Name

    member self.FullPath =
        let path = dirInfo.FullName

        if path.Contains(" ") then
            "\"" + path + "\""
        else
            path

    member self.Exists = dirInfo.Exists

    member self.Create() =
        dirInfo.Create()

    member self.Delete(recursive: bool) =
        dirInfo.Delete(recursive)

    member self.CombineDir(subPath: string, ?checkExistence: bool) =
        System.IO.Path.Combine(dirInfo.FullName, subPath)
        |> System.IO.DirectoryInfo
        |> fun dirInfo -> AbsDir(dirInfo, ?checkExistence = checkExistence)

    member self.CombineFile(subPath: string, ?checkExistence: bool) =
        System.IO.Path.Combine(dirInfo.FullName, subPath)
        |> System.IO.FileInfo
        |> fun fileInfo -> AbsFile(fileInfo, ?checkExistence = checkExistence)

    new(path: string, ?checkExistence: bool) =
        if not(System.IO.Path.IsPathRooted path) then
            failwithf "AbsDir secondary ctor received a relative path: %s" path

        AbsDir(System.IO.DirectoryInfo(path), ?checkExistence = checkExistence)

let initialDir = System.IO.Directory.GetCurrentDirectory() |> AbsDir

let gitPointerFileName = ".git"
let bareRepoDirName = ".bare"

let errUsage =
    (1,
     $"Usage: dotnet fsi {__SOURCE_FILE__} <repoUrl|folderPath|prUrl> [branchName]")

let ErrDirectoryDoesNotExist path =
    (2, sprintf "Directory '%s' does not exist." path)

let ErrDirectoryIsNotClone path =
    (3, sprintf "Directory '%s' already exists but is not a clone." path)

let errCannotDetermineRemoteName =
    (4,
     "Both 'upstream' and 'origin' remotes already exist and the new remote is not a fork. Cannot determine a name for the new remote.")

let errGitHubApiConnectionFailedCannotCheckIfGitHubRepoIsForkOrNot =
    (5,
     "Could not check whether the repo is a GitHub fork: check your internet connection?")

let errPrAlreadyEncodesBranchName =
    (6,
     "A PR URL already encodes the branch name. Do not specify a second argument.")

let errGitFetchAllFailed =
    (7, "Git fetch --all failed: check your internet connection?")

let errGitCloneFailed = (8, "Git clone failed: check your internet connection?")

let ErrNotGitHubCannotDetermineRemoteName dir =
    (9,
     sprintf
         "Directory '%s' already exists; URL is not GitHub so API cannot be queried to find best name for new remote"
         dir)

let errInvalidPrUrl =
    (10,
     "Invalid PR URL. Expected format: https://github.com/<owner>/<repo>/pull/<number>")

let errFailedToFetchPrData =
    (11,
     "Failed to fetch PR data from GitHub API: check your internet connection?")

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
        | _ ->
            let exitCode, errMsg = errFailedToFetchPrData
            Console.Error.WriteLine errMsg
            Environment.Exit exitCode
            failwith <| "Unreachable because of: " + errMsg

    let prJson = JsonValue.Parse response

    let headRepoUrl =
        prJson
            .GetProperty("head")
            .GetProperty("repo")
            .GetProperty("ssh_url")
            .AsString()

    let headBranch =
        prJson
            .GetProperty("head")
            .GetProperty("ref")
            .AsString()

    Console.WriteLine $"PR #{prNumber} source repo: {headRepoUrl}"
    Console.WriteLine $"PR #{prNumber} source branch: {headBranch}"
    headRepoUrl, headBranch


// Determine branch existence via ls-remote against a single remote target (name or URL)
let CheckRemoteBranchExists (cloneDir: AbsDir) branchName remote =
    let output =
        Process
            .ExecDefault(
                sprintf
                    "git -C %s ls-remote --heads %s %s"
                    cloneDir.FullPath
                    remote
                    branchName,
                echo = Echo.Off
            )
            .UnwrapDefault()
            .Trim()

    output.Contains branchName

let AllRemotes(cloneDir: AbsDir) =
    if not cloneDir.Exists then
        failwithf
            "BUG: can't check remotes if repo directory doesn't exist: %s"
            cloneDir.FullPath

    let remoteOutput =
        Process
            .ExecDefault(
                sprintf "git -C %s remote --verbose" cloneDir.FullPath,
                echo = Echo.Off
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

let GetCurrentHeadBranch(cloneDir: AbsDir) =
    Process
        .ExecDefault(
            sprintf "git -C %s symbolic-ref --short HEAD" cloneDir.FullPath,
            echo = Echo.Off
        )
        .UnwrapDefault(throwWhenWarnings = false)
        .Trim()

let ValidateDirIsClone(cloneDir: AbsDir) =
    let gitFile =
        cloneDir.CombineFile(gitPointerFileName, checkExistence = false)

    let bareDir = cloneDir.CombineDir(bareRepoDirName, checkExistence = false)

    if not gitFile.Exists || not bareDir.Exists then
        let exitCode, errMsg = ErrDirectoryIsNotClone cloneDir.PathlessName
        Console.Error.WriteLine errMsg
        Environment.Exit exitCode

let rawArgs = Misc.FsxOnlyArguments()

if rawArgs.Length < 1 || rawArgs.Length > 2 then
    let exitCode, errMsg = errUsage
    Console.Error.WriteLine errMsg
    Environment.Exit exitCode

// If single arg is a PR URL, resolve it to repo URL + branch before proceeding
let resolvedArgs =
    if IsGitHubPullRequestUrl rawArgs.[0] then
        if rawArgs.Length = 2 then
            let exitCode, errMsg = errPrAlreadyEncodesBranchName
            Console.Error.WriteLine errMsg
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

            let cloneDir =
                initialDir.CombineDir(repoAndFolderName, checkExistence = false)

            let existing = cloneDir.Exists

            if existing then
                ValidateDirIsClone cloneDir

            if not existing then
                cloneDir.Create()

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
            let cloneDir =
                initialDir.CombineDir(firstArg, checkExistence = false)

            if not cloneDir.Exists then
                let exitCode, errMsg =
                    ErrDirectoryDoesNotExist cloneDir.FullPath

                Console.Error.WriteLine errMsg

                Environment.Exit exitCode

            ValidateDirIsClone cloneDir

            true, FolderName, firstArg, GetCurrentHeadBranch cloneDir

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

    let cloneDir = initialDir.CombineDir(repoAndFolderName)

    let branchExists =
        let existsOnConfiguredRemotes() =
            AllRemotes cloneDir
            |> Map.toSeq
            |> Seq.map fst
            |> Seq.exists(CheckRemoteBranchExists cloneDir branchName)

        match initialState with
        | {
              ArgType = Url(fullUrl, _owner, _headBranch)
              AlreadyCloned = false
          } -> CheckRemoteBranchExists cloneDir branchName fullUrl
        | {
              ArgType = Url(fullUrl, _owner, _headBranch)
              AlreadyCloned = true
          } ->
            CheckRemoteBranchExists cloneDir branchName fullUrl
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

let cloneDir = initialDir.CombineDir(initialState.RepoAndFolderName)

match initialState with
| {
      ArgType = Url(fullUrl, owner, _headBranch)
      AlreadyCloned = false
      RepoAndFolderName = repoAndFolderName
  } ->
    try
        Process
            .ExecDefault(
                sprintf
                    "git -C %s clone --single-branch --bare %s %s"
                    cloneDir.FullPath
                    fullUrl
                    bareRepoDirName
            )
            .UnwrapDefault(throwWhenWarnings = false)
        |> ignore<string>
    with
    | _ ->
        // Clean up the directory we created
        cloneDir.Delete(true)
        let exitCode, errMsg = errGitCloneFailed
        Console.Error.WriteLine errMsg
        Environment.Exit exitCode

    // Create .git file pointing to ./.bare (using F# instead of echo)
    let gitFile =
        cloneDir.CombineFile(gitPointerFileName, checkExistence = false)

    gitFile.WriteAllText(
        sprintf "gitdir: ./%s" bareRepoDirName + Environment.NewLine
    )

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
                        sprintf
                            "git -C %s remote rename origin %s"
                            cloneDir.FullPath
                            newRemoteName
                    )
                    .UnwrapDefault(throwWhenWarnings = false)
                |> ignore<string>

| {
      ArgType = Url(fullUrl, owner, _headBranch)
      AlreadyCloned = true
      RepoAndFolderName = repoAndFolderName
  } ->
    let maybeFoundRemote =
        AllRemotes cloneDir
        |> Map.tryPick(fun name url ->
            if url.Contains fullUrl then
                Some name
            else
                None
        )

    match maybeFoundRemote with
    | Some _ -> ()
    | None ->
        let newRemoteName =
            let isGitHubUrl =
                fullUrl.Contains(
                    "github.com",
                    StringComparison.OrdinalIgnoreCase
                )

            if not isGitHubUrl then
                let exitCode, errMsg =
                    ErrNotGitHubCannotDetermineRemoteName repoAndFolderName

                Console.Error.WriteLine errMsg
                Environment.Exit exitCode
                failwith <| "Unreachable because of: " + errMsg

            let remoteMap = AllRemotes cloneDir

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
                let exitCode, errMsg =
                    errGitHubApiConnectionFailedCannotCheckIfGitHubRepoIsForkOrNot

                Console.Error.WriteLine errMsg

                Environment.Exit exitCode
                failwith <| "Unreachable because of: " + errMsg

        Process
            .ExecDefault(
                sprintf
                    "git -C %s remote add %s %s"
                    cloneDir.FullPath
                    newRemoteName
                    fullUrl
            )
            .UnwrapDefault(throwWhenWarnings = false)
        |> ignore<string>
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
            | _ -> GetCurrentHeadBranch cloneDir

        [ headBranch; branchTargetInfo.Name ]

    let allRemoteNames =
        AllRemotes cloneDir |> Map.toSeq |> Seq.map fst |> Seq.toList

    allRemoteNames
    |> Seq.iter(fun remoteName ->
        for branchToFetch in branchesToFetch do
            if CheckRemoteBranchExists cloneDir branchToFetch remoteName then
                let setBranchesCmd =
                    sprintf
                        "git -C %s remote set-branches --add %s %s"
                        cloneDir.FullPath
                        remoteName
                        branchToFetch

                Process
                    .ExecDefault(setBranchesCmd)
                    .UnwrapDefault(throwWhenWarnings = false)
                |> ignore<string>
    )

// Fetch all remotes
Process
    .ExecDefault(sprintf "git -C %s fetch --all" cloneDir.FullPath)
    .UnwrapDefault(throwWhenWarnings = false)
|> ignore<string>

let absWorktreeDir =
    cloneDir.CombineDir(branchTargetInfo.SubFolderName, checkExistence = false)

let gitWorktreeAddArgs =
    if branchTargetInfo.ExistsAlready then
        // Use remote tracking ref to avoid ambiguity when multiple remotes have the same branch
        let allRemoteNames =
            AllRemotes cloneDir |> Map.toSeq |> Seq.map fst |> Seq.toList

        let remotesWithBranch =
            allRemoteNames
            |> List.filter(fun remoteName ->
                CheckRemoteBranchExists
                    cloneDir
                    branchTargetInfo.Name
                    remoteName
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
                        AllRemotes cloneDir
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
                absWorktreeDir.FullPath
                remote
                branchTargetInfo.Name
        | None -> sprintf "%s %s" absWorktreeDir.FullPath branchTargetInfo.Name
    else
        sprintf "-b %s %s" branchTargetInfo.Name absWorktreeDir.FullPath

Process
    .ExecDefault(
        sprintf "git -C %s worktree add %s" cloneDir.FullPath gitWorktreeAddArgs
    )
    .UnwrapDefault(throwWhenWarnings = false)
|> ignore<string>

// If branch already existed, worktree was created from a remote tracking ref
// and is in detached HEAD state; create or reset the local branch to HEAD
// (which is the latest remote tracking ref), then switch to it
if branchTargetInfo.ExistsAlready then
    let worktreeDir = cloneDir.CombineDir(branchTargetInfo.SubFolderName)

    let fullCmd =
        sprintf
            "git -C %s checkout -B %s"
            worktreeDir.FullPath
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
