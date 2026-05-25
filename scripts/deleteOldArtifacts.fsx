#!/usr/bin/env -S dotnet fsi

open System
open System.Net
open System.Net.Http
open System.Net.Http.Headers

#r "nuget: FSharp.Data, Version=5.0.2"
open FSharp.Data

#r "nuget: Fsdk, Version=0.9.99--date20260525-0605.git-a5cfc39"

open Fsdk
open Fsdk.Process
open Fsdk.FSharpUtil

let githubRepository = Environment.GetEnvironmentVariable "GITHUB_REPOSITORY"

if String.IsNullOrEmpty githubRepository then
    Console.Error.WriteLine
        "This script is meant to be used only within a GitHubCI pipeline."

    Environment.Exit 1

let githubToken = Environment.GetEnvironmentVariable "GITHUB_TOKEN"

if String.IsNullOrEmpty githubToken then
    Console.Error.WriteLine
        "Please set the GITHUB_TOKEN environment variable in your GitHubCI pipeline:

    env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
    "

    Environment.Exit 2

let GetPreviousCommitsHashes() =
    Fsdk
        .Process
        .Execute(
            {
                Command = "git"
                Arguments = "log --pretty=format:%H"
            },
            Echo.Off
        )
        .UnwrapDefault()
        .Trim()
        .Split(
        Environment.NewLine
    ).[1..]

type GithubArtifactsType =
    JsonProvider<"""
{
  "total_count": 1,
  "artifacts": [
    {
      "id": 4823510038,
      "node_id": "MDg6QXJ0aWZhY3Q2Njc1MDU5MjI=",
      "name": "publishedPackages",
      "size_in_bytes": 151303891,
      "url": "https://api.github.com/repos/aaarani/RunIntoMe/actions/artifacts/667505922",
      "archive_download_url": "https://api.github.com/repos/aaarani/RunIntoMe/actions/artifacts/667505922/zip",
      "expired": false,
      "created_at": "2023-04-26T22:57:37Z",
      "updated_at": "2023-04-26T22:57:38Z",
      "expires_at": "2023-07-25T22:47:52Z",
      "workflow_run": {
        "id": 4813959132,
        "repository_id": 632336429,
        "head_repository_id": 632336429,
        "head_branch": "wip/WelcomePage",
        "head_sha": "1357c79f43c12189c7aa4c46b5661400b704c211"
      }
    }
  ]
}
""">

let githubApiCallForbiddenErrorMsg =
    """GITHUB_TOKEN passed doesn't seem to have enough permissions.

To modify the permissions of your token, navigate to the Settings section of your 
repository or organization and click on Actions button, then select General. From 
'Workflow permissions' section on that page, choose 'Read and write permissions' 
(which grants access to content and the ability to make changes).
"""

type ApiAction =
    | Get
    | Delete

let GitHubApiQuery
    (url: string)
    (action: ApiAction)
    (acceptMediaTypeOpt: Option<string>)
    =
    let userAgent = ".NET App"
    let xGitHubApiVersion = "2022-11-28"

    use client = new HttpClient()
    client.DefaultRequestHeaders.Accept.Clear()

    match acceptMediaTypeOpt with
    | Some acceptMediaType ->
        client.DefaultRequestHeaders.Accept.Add(
            MediaTypeWithQualityHeaderValue acceptMediaType
        )
    | None -> ()

    client.DefaultRequestHeaders.Add("User-Agent", userAgent)
    client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", xGitHubApiVersion)

    if not(String.IsNullOrEmpty githubToken) then
        client.DefaultRequestHeaders.Add(
            "Authorization",
            $"Bearer {githubToken}"
        )

    Console.WriteLine(sprintf "Calling %s ..." url)

    try
        match action with
        | Get ->
            client.GetStringAsync url
            |> Async.AwaitTask
            |> Async.RunSynchronously
        | Delete ->
            (client.DeleteAsync url |> Async.AwaitTask |> Async.RunSynchronously)
                .ToString()
    with
    | ex ->
        match FindException<HttpRequestException> ex with
        | Some httpRequestException ->
            match Option.ofNullable httpRequestException.StatusCode with
            | Some statusCode when statusCode = HttpStatusCode.Forbidden ->

                failwith githubApiCallForbiddenErrorMsg

            | _ -> reraise()

        | _ -> reraise()

let HasCIStatus(commit: string) : bool =
    let url =
        sprintf
            "https://api.github.com/repos/%s/commits/%s/check-suites"
            githubRepository
            commit

    let mediaTypeWithQuality = Some "application/vnd.github+json"

    let json = GitHubApiQuery url Get mediaTypeWithQuality

    not(json.Contains "\"check_suites\":[]")

let userNameOrOrgName = githubRepository.Split("/").[0]

let artifactsApiQueryResult =
    let mediaTypeWithQuality = Some "application/vnd.github+json"

    let url =
        $"https://api.github.com/repos/{githubRepository}/actions/artifacts"

    GitHubApiQuery url Get mediaTypeWithQuality

let parsedJsonObj = GithubArtifactsType.Parse artifactsApiQueryResult

let artifactIds = parsedJsonObj.Artifacts

type WhatArtifactsToDelete =
    | AllFromBranchlessCommitsAndAllFromPreviousOnesInThisBranch

    // FIXME: there is an edge case, e.g. if previous CI commit is red
    // (such as "Empty commit to test CI") then the next commit that fixes
    // CI will not remove the artifacts from the previous-previous commit
    | OnlyOneFromPreviousCiStatus

let whatArtifactsToDelete =
    AllFromBranchlessCommitsAndAllFromPreviousOnesInThisBranch

let DoesCommitExistInTheRepoAndBranch(commit: string) =
    let branchesTheCommitBelongsTo =
        try
            Fsdk
                .Process
                .Execute(
                    {
                        Command = "git"
                        Arguments = $"branch --contains {commit}"
                    },
                    Echo.Off
                )
                .UnwrapDefault()
                .Trim()
        with
        | :? ProcessFailed -> String.Empty
        | _ -> reraise()

    not(String.IsNullOrEmpty branchesTheCommitBelongsTo)

let DeleteArtifact (githubRepository: string) (artifactId: string) =
    let url =
        $"https://api.github.com/repos/{githubRepository}/actions/artifacts/{artifactId}"

    GitHubApiQuery url Delete None |> ignore<string>

let previousCommitsHashes = GetPreviousCommitsHashes()

match whatArtifactsToDelete with
| OnlyOneFromPreviousCiStatus ->

    Console.WriteLine "About to delete only the artifacts of previous commit..."

    let previousCommitWithCIStatusOpt =
        Seq.tryFind HasCIStatus previousCommitsHashes

    match previousCommitWithCIStatusOpt with
    | None ->
        Console.WriteLine "No commits found with CI status"
        exit 0
    | Some previousCommitWithCIStatus ->
        let artifactIdsToDelete =
            artifactIds
            |> Seq.filter(fun item ->
                item.WorkflowRun.HeadSha = previousCommitWithCIStatus
            )
            |> Seq.map(fun artifact -> artifact.Id.ToString())

        artifactIdsToDelete
        |> Seq.iter(fun artifactId -> DeleteArtifact githubRepository artifactId
        )

| AllFromBranchlessCommitsAndAllFromPreviousOnesInThisBranch ->
    Console.WriteLine "About to delete all previous artifacts of this branch..."

    let artifactIdsToDelete =
        artifactIds
        |> Seq.filter(fun item ->
            let commit = item.WorkflowRun.HeadSha
            Seq.contains commit previousCommitsHashes
        )
        |> Seq.map(fun artifact -> artifact.Id.ToString())

    artifactIdsToDelete
    |> Seq.iter(fun artifactId -> DeleteArtifact githubRepository artifactId)

    Console.WriteLine
        "About to delete all the artifacts of branchless commits..."

    // Make sure git has complete history before checking for branchless commits.
    let gitFetchUnshallowResult =
        Fsdk.Process.Execute(
            {
                Command = "git"
                // The special depth 2147483647 means infinite depth, see https://git-scm.com/docs/shallow
                Arguments = "fetch --depth=2147483647"
            },
            Echo.Off
        )
    // Fsdk may categorize normal output of git fetch as WarningsOrAmbiguous, so have to treat it as succcess.
    match gitFetchUnshallowResult.Result with
    | Success _
    | WarningsOrAmbiguous _ -> ()
    | Error _ -> gitFetchUnshallowResult.UnwrapDefault() |> ignore<string>

    let orphanArtifactIdsToDelete =
        artifactIds
        |> Seq.filter(fun item ->
            let commit = item.WorkflowRun.HeadSha
            not(DoesCommitExistInTheRepoAndBranch commit)
        )
        |> Seq.map(fun artifact -> artifact.Id.ToString())

    orphanArtifactIdsToDelete
    |> Seq.iter(fun artifactId -> DeleteArtifact githubRepository artifactId)
