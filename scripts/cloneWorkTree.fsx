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
        $"Usage: dotnet fsi {__SOURCE_FILE__} <repoUrl> <featureBranchName>"

    Environment.Exit 1

let repoUrl = args.[0]
let featureBranchName = args.[1]

// 1) Extract repo name from URL
let repoName =
    let uri = Uri repoUrl
    let segments = uri.AbsolutePath.TrimEnd('/').Split('/')
    let lastSegmentOpt = Array.tryLast segments

    match lastSegmentOpt with
    | None -> failwith "Unreachable"
    | Some lastSegment ->
        if lastSegment.EndsWith(".git", StringComparison.OrdinalIgnoreCase) then
            lastSegment.Substring(0, lastSegment.Length - ".git".Length)
        else
            lastSegment

// 2) Create subfolder with same name as repo (fail if already exists)
if Directory.Exists repoName then
    Console.Error.WriteLine(sprintf "Directory '%s' already exists." repoName)
    Environment.Exit 2

Directory.CreateDirectory repoName |> ignore<DirectoryInfo>

// 3) cd into that folder
Directory.SetCurrentDirectory repoName

// 4) git clone --single-branch --bare <repository-url> .bare
let gitClone =
    {
        Command = "git"
        Arguments = sprintf "clone --single-branch --bare %s .bare" repoUrl
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

let defaultBranch =
    Process
        .Execute(gitSymbolicRef, Echo.Off)
        .UnwrapDefault()
        .Trim()

Console.WriteLine(sprintf "Default branch: %s" defaultBranch)

// 7) git worktree add <defaultBranch> <featureBranchName>
let gitWorktreeAdd =
    {
        Command = "git"
        Arguments = sprintf "worktree add %s %s" featureBranchName defaultBranch
    }

let worktreeProc = Process.Execute(gitWorktreeAdd, Echo.All)

match worktreeProc.Result with
| Error _ ->
    Console.Error.WriteLine "Git worktree add failed."
    Environment.Exit 4
| WarningsOrAmbiguous _
| Success _ -> ()

// 8) cd into featureBranchName and create branch
Directory.SetCurrentDirectory featureBranchName

let branchExists =
    let output =
        Process
            .Execute(
                {
                    Command = "git"
                    Arguments = sprintf "branch --list %s" featureBranchName
                },
                Echo.Off
            )
            .UnwrapDefault()
            .Trim()

    output.Contains featureBranchName

let checkoutArgs =
    if branchExists then
        sprintf "switch %s" featureBranchName
    else
        sprintf "checkout -b %s" featureBranchName

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
        defaultBranch
        featureBranchName
        repoName
)
