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

// 2) Create subfolder with same name as repo (fail if already exists)
if Directory.Exists repoName then
    Console.Error.WriteLine(sprintf "Directory '%s' already exists." repoName)
    Environment.Exit 2

Directory.CreateDirectory repoName |> ignore<DirectoryInfo>

// 3) cd into that folder
Directory.SetCurrentDirectory repoName

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
let isGitHubUrl =
    repoUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase)

if isGitHubUrl then
    let ghOwner =
        let pathPart =
            if repoUrl.StartsWith("git@", StringComparison.OrdinalIgnoreCase) then
                let colonIndex = repoUrl.IndexOf ':'
                repoUrl.Substring(colonIndex + 1)
            else
                (Uri repoUrl).AbsolutePath.TrimStart '/'

        pathPart.TrimEnd('/').Split('/').[0]

    use httpClient = new System.Net.Http.HttpClient()

    // required or HTTP call will fail
    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd "dotnet-fsi"

    let apiUrl = sprintf "https://api.github.com/repos/%s/%s" ghOwner repoName

    let maybeResponse =
        try
            httpClient.GetStringAsync apiUrl
            |> Async.AwaitTask
            |> Async.RunSynchronously
            |> Some
        with
        | _ ->
            Console.Error.WriteLine(
                "Could not check whether the repo is a GitHub fork. Skipping remote rename."
            )

            None

    match maybeResponse with
    | None -> ()
    | Some response ->
        let isFork = response.Contains "\"fork\":true"

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
