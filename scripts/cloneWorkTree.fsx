#!/usr/bin/env -S dotnet fsi

open System
open System.IO

#r "System.Configuration"
open System.Configuration

#r "nuget: Fsdk, Version=0.9.99--date20260525-0605.git-a5cfc39"

open Fsdk
open Fsdk.Process

let args = Misc.FsxOnlyArguments()

if args.Length <> 2 then
    Console.Error.WriteLine
        $"Usage: dotnet fsi {__SOURCE_FILE__} <repoUrl> <branchName>"

    Environment.Exit 1

let repoUrl = args.[0]
let branchName = args.[1]

// 1) Extract repo name from URL
let repoName =
    let pathPart =
        if repoUrl.StartsWith("git@", StringComparison.OrdinalIgnoreCase) then
            // SCP-style SSH URL: git@host:path/to/repo.git
            let colonIndex = repoUrl.IndexOf(':')

            if colonIndex < 0 then
                failwith "Invalid SCP-style git URL: missing ':' separator"

            repoUrl.Substring(colonIndex + 1)
        else
            // Standard URI (https, ssh://, file://, etc.)
            let uri = Uri repoUrl
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

let ghOwner =
    let pathPart =
        if repoUrl.StartsWith("git@", StringComparison.OrdinalIgnoreCase) then
            let colonIndex = repoUrl.IndexOf ':'
            repoUrl.Substring(colonIndex + 1)
        else
            (Uri repoUrl).AbsolutePath.TrimStart '/'

    pathPart.TrimEnd('/').Split('/').[0]

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

let branchExists =
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
let isExistingClone =
    if Directory.Exists repoName then
        let gitFile = Path.Combine(repoName, ".git")
        let bareDir = Path.Combine(repoName, ".bare")

        if not(File.Exists gitFile) || not(Directory.Exists bareDir) then
            Console.Error.WriteLine(
                sprintf
                    "Directory '%s' already exists and is not a clone."
                    repoName
            )

            Environment.Exit 2

        true
    else
        false

if not isExistingClone then
    Directory.CreateDirectory repoName |> ignore<DirectoryInfo>

// 3) cd into that folder
Directory.SetCurrentDirectory repoName

if isExistingClone then
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
            repoUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase)

        let remoteName =
            if not isGitHubUrl then
                failwithf
                    "Directory '%s' already exists; URL is not GitHub so API cannot be queried to find best name for new remote"
                    repoName
            else
                match CheckIsGitHubFork ghOwner repoName with
                | Some false ->
                    if not(List.contains "upstream" existingRemotes) then
                        "upstream"
                    elif not(List.contains "origin" existingRemotes) then
                        "origin"
                    else
                        Console.Error.WriteLine(
                            "Both 'upstream' and 'origin' remotes already exist and the new remote is not a fork. Cannot determine a name for the new remote."
                        )

                        Environment.Exit 3
                        String.Empty
                | Some true -> sprintf "%sFork" ghOwner
                | None ->
                    Console.Error.WriteLine(
                        "Could not check whether the repo is a GitHub fork. Cannot determine a name for the new remote."
                    )

                    Environment.Exit 3
                    String.Empty

        let addRemoteProc =
            Process.Execute(
                {
                    Command = "git"
                    Arguments = sprintf "remote add %s %s" remoteName repoUrl
                },
                Echo.All
            )

        match addRemoteProc.Result with
        | Error _ ->
            Console.Error.WriteLine(
                sprintf "Failed to add remote '%s'." repoUrl
            )

            Environment.Exit 3
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
        Console.Error.WriteLine "Git fetch --all failed."
        Environment.Exit 3
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
        Console.Error.WriteLine "Git clone failed."
        Environment.Exit 3
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

// 7) git worktree add <defaultBranch> <branchName>
let gitWorktreeAdd =
    {
        Command = "git"
        Arguments = sprintf "worktree add %s %s" branchName baseBranch
    }

let worktreeProc = Process.Execute(gitWorktreeAdd, Echo.All)

match worktreeProc.Result with
| Error _ ->
    Console.Error.WriteLine "Git worktree add failed."
    Environment.Exit 4
| WarningsOrAmbiguous _
| Success _ -> ()

// 8) cd into branchName and create branch
Directory.SetCurrentDirectory branchName

if not branchExists then
    let checkoutArgs = sprintf "checkout -b %s" branchName

    let gitCheckout =
        {
            Command = "git"
            Arguments = checkoutArgs
        }

    let checkoutProc = Process.Execute(gitCheckout, Echo.All)

    match checkoutProc.Result with
    | Error _ ->
        if branchExists then
            Console.Error.WriteLine("Git switch failed.")
        else
            Console.Error.WriteLine("Git checkout -b failed.")

        Environment.Exit 5
    | WarningsOrAmbiguous _
    | Success _ -> ()

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
                    Console.Error.WriteLine(
                        sprintf
                            "Failed to rename remote 'origin' to '%s'."
                            newRemoteName
                    )

                    Environment.Exit 6
                | WarningsOrAmbiguous _
                | Success _ -> ()
