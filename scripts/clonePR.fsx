#!/usr/bin/env -S dotnet fsi

open System
open System.IO
open System.Net.Http
open System.Text.RegularExpressions

#r "System.Configuration"
open System.Configuration

#r "nuget: Fsdk, Version=0.9.99--date20260525-0605.git-a5cfc39"
#r "nuget: FSharp.Data, Version=5.0.2"

open FSharp.Data
open Fsdk
open Fsdk.Process

let errUsage = (1, $"Usage: dotnet fsi {__SOURCE_FILE__} <prUrl>")

let errInvalidPrUrl =
    (2,
     "Invalid PR URL. Expected format: https://github.com/<owner>/<repo>/pull/<number>")

let ErrFailedToFetchPrData exMsg =
    (3, sprintf "Failed to fetch PR data from GitHub API: %s" exMsg)

let ErrFailedToExtractHeadRepoUrl exMsg =
    (4, sprintf "Failed to extract head repo URL from PR data: %s" exMsg)

let ErrFailedToExtractHeadBranch exMsg =
    (5, sprintf "Failed to extract head branch from PR data: %s" exMsg)

let errCloneWorkTreeFailed = (6, "cloneWorkTree.fsx failed")

let args = Misc.FsxOnlyArguments()

if args.Length <> 1 then
    let exitCode, errMsg = errUsage
    Console.Error.WriteLine errMsg

    Environment.Exit exitCode

let prUrl = args.[0]

// Validate and parse PR URL
// Expected format: https://github.com/<owner>/<repo>/pull/<number>
let prRegex =
    Regex(
        @"^https?://github\.com/(?<owner>[^/]+)/(?<repo>[^/]+)/pull/(?<number>\d+)/?$",
        RegexOptions.IgnoreCase
    )

let regexMatch = prRegex.Match prUrl

if not regexMatch.Success then
    let exitCode, errMsg = errInvalidPrUrl
    Console.Error.WriteLine errMsg

    Environment.Exit exitCode

let owner = regexMatch.Groups.["owner"].Value
let repo = regexMatch.Groups.["repo"].Value
let prNumber = regexMatch.Groups.["number"].Value

// Fetch PR details via GitHub API
let apiUrl = $"https://api.github.com/repos/{owner}/{repo}/pulls/{prNumber}"

let httpClient = new HttpClient()

httpClient.DefaultRequestHeaders.Add("User-Agent", "clonePR.fsx")

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
            .GetProperty("clone_url")
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

// Call cloneWorkTree.fsx
let cloneWorkTreeScript =
    Path.Combine(__SOURCE_DIRECTORY__, "cloneWorkTree.fsx")

let dotnetFsi =
    {
        Command = "dotnet"
        Arguments =
            $"fsi \"{cloneWorkTreeScript}\" \"{headRepoUrl}\" \"{headBranch}\""
    }

let proc = Process.Execute(dotnetFsi, Echo.All)

match proc.Result with
| Error _ ->
    let exitCode, errMsg = errCloneWorkTreeFailed
    Console.Error.WriteLine errMsg

    Environment.Exit exitCode
| WarningsOrAmbiguous _
| Success _ -> ()
