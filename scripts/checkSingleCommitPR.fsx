#!/usr/bin/env -S dotnet fsi

open System

#r "nuget: Fsdk, Version=0.6.1--date20260403-0728.git-c9a0eae"
#r "nuget: Mono.Unix, Version=7.1.0-final.1.21458.1"
#r "nuget: YamlDotNet, Version=16.1.3"

#load "../src/FileConventions/Library.fs"

open Fsdk
open Fsdk.Process

let prTitle =
    Environment.GetEnvironmentVariable "PR_TITLE"
    |> Option.ofObj
    |> Option.defaultValue String.Empty

let prDescription =
    Environment.GetEnvironmentVariable "PR_DESCRIPTION"
    |> Option.ofObj
    |> Option.defaultValue String.Empty

let numCommitsStr = Environment.GetEnvironmentVariable "PR_COMMITS"
let commitSha = Environment.GetEnvironmentVariable "PR_HEAD_SHA"

if String.IsNullOrEmpty numCommitsStr then
    Console.Error.WriteLine(
        "Error: PR_COMMITS environment variable is not set. This script is meant to be run only by GitHubActions triggers, not locally"
    )

    Environment.Exit 1

if String.IsNullOrEmpty commitSha then
    Console.Error.WriteLine(
        "Error: PR_HEAD_SHA environment variable is not set."
    )

    Environment.Exit 1

let numCommits = int numCommitsStr

if numCommits <> 1 then
    Console.WriteLine(
        sprintf "PR has %i commits. Skipping single-commit check." numCommits
    )

    Environment.Exit 0

let commitMessage =
    Process
        .Execute(
            {
                Command = "git"
                Arguments = sprintf "show --format=%%B --no-patch %s" commitSha
            },
            Echo.Off
        )
        .UnwrapDefault()
        .Trim()

let commitLines = commitMessage.Split([| '\n' |], StringSplitOptions.None)

let commitTitle = commitLines.[0].Trim()

let commitDescription =
    let bodyLineStartsAt = 2
    let hasBody = (commitLines.Length > bodyLineStartsAt)

    if hasBody then
        String
            .Join(Environment.NewLine, commitLines |> Seq.skip bodyLineStartsAt)
            .Trim()
    else
        String.Empty

let normalizedCommitTitle = FileConventions.RemoveAllWhitespace commitTitle
let normalizedPrTitle = FileConventions.RemoveAllWhitespace prTitle
let titleMatches = normalizedCommitTitle = normalizedPrTitle

let normalizedCommitDesc = FileConventions.RemoveAllWhitespace commitDescription
let normalizedPrDesc = FileConventions.RemoveAllWhitespace prDescription
let descMatches = normalizedCommitDesc = normalizedPrDesc

if not titleMatches || not descMatches then
    Console.Error.WriteLine(
        "Error: For single-commit PRs, the commit message must match the PR title and description."
    )

    Console.Error.WriteLine(String.Empty)

    if not titleMatches then
        Console.Error.WriteLine("Title mismatch:")

        Console.Error.WriteLine(
            sprintf "  Commit msg title (normalized): %s" normalizedCommitTitle
        )

        Console.Error.WriteLine(
            sprintf "  PR title         (normalized): %s" normalizedPrTitle
        )

    if not descMatches then
        Console.Error.WriteLine("Description mismatch:")

        Console.Error.WriteLine(
            sprintf "  Commit msg body (normalized): %s" normalizedCommitDesc
        )

        Console.Error.WriteLine(
            sprintf "  PR description  (normalized): %s" normalizedPrDesc
        )

    Console.Error.WriteLine(String.Empty)

    Console.Error.WriteLine(
        "Please sync the PR title and description with the commit message."
    )

    Environment.Exit 1
else
    Console.WriteLine("Commit message matches PR title and description.")
    Environment.Exit 0
